using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Features.Admin;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Identity.Constants;
using MyProject.Domain;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Admin.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IRoleManagementService"/> for role CRUD and permission management.
/// </summary>
internal class RoleManagementService(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    MyProjectDbContext dbContext,
    ICacheService cacheService,
    ILogger<RoleManagementService> logger) : IRoleManagementService
{
    /// <inheritdoc />
    public async Task<Result<RoleDetailOutput>> GetRoleDetailAsync(Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return Result<RoleDetailOutput>.Failure(ErrorMessages.Roles.RoleNotFound);
        }

        var claims = await roleManager.GetClaimsAsync(role);
        var permissions = claims
            .Where(c => c.Type == AppPermissions.ClaimType)
            .Select(c => c.Value)
            .ToList();

        var userCount = await dbContext.UserRoles
            .CountAsync(ur => ur.RoleId == roleId, cancellationToken);

        var isSystem = AppRoles.All.Contains(role.Name ?? string.Empty);

        return Result<RoleDetailOutput>.Success(new RoleDetailOutput(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            isSystem,
            permissions,
            userCount));
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateRoleAsync(CreateRoleInput input,
        CancellationToken cancellationToken = default)
    {
        if (AppRoles.All.Any(r => string.Equals(r, input.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<Guid>.Failure(ErrorMessages.Roles.SystemRoleNameReserved);
        }

        var existing = await roleManager.FindByNameAsync(input.Name);
        if (existing is not null)
        {
            return Result<Guid>.Failure(ErrorMessages.Roles.RoleNameTaken);
        }

        var role = new ApplicationRole
        {
            Name = input.Name,
            Description = input.Description
        };

        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(errors);
        }

        logger.LogInformation("Custom role '{RoleName}' created with ID '{RoleId}'", input.Name, role.Id);

        return Result<Guid>.Success(role.Id);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateRoleAsync(Guid roleId, UpdateRoleInput input,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return Result.Failure(ErrorMessages.Roles.RoleNotFound);
        }

        var isSystem = AppRoles.All.Contains(role.Name ?? string.Empty);

        if (input.Name is not null && input.Name != role.Name)
        {
            if (isSystem)
            {
                return Result.Failure(ErrorMessages.Roles.SystemRoleCannotBeRenamed);
            }

            if (AppRoles.All.Any(r => string.Equals(r, input.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return Result.Failure(ErrorMessages.Roles.SystemRoleNameReserved);
            }

            var existing = await roleManager.FindByNameAsync(input.Name);
            if (existing is not null)
            {
                return Result.Failure(ErrorMessages.Roles.RoleNameTaken);
            }

            role.Name = input.Name;
        }

        if (input.Description is not null)
        {
            role.Description = input.Description;
        }

        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        logger.LogInformation("Role '{RoleId}' updated", roleId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteRoleAsync(Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return Result.Failure(ErrorMessages.Roles.RoleNotFound);
        }

        if (AppRoles.All.Contains(role.Name ?? string.Empty))
        {
            return Result.Failure(ErrorMessages.Roles.SystemRoleCannotBeDeleted);
        }

        var userCount = await dbContext.UserRoles
            .CountAsync(ur => ur.RoleId == roleId, cancellationToken);

        if (userCount > 0)
        {
            return Result.Failure(ErrorMessages.Roles.RoleHasUsers);
        }

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        logger.LogWarning("Custom role '{RoleName}' (ID '{RoleId}') deleted", role.Name, roleId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> SetRolePermissionsAsync(Guid roleId, SetRolePermissionsInput input,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
        {
            return Result.Failure(ErrorMessages.Roles.RoleNotFound);
        }

        if (role.Name == AppRoles.SuperAdmin)
        {
            return Result.Failure(ErrorMessages.Roles.SuperAdminPermissionsFixed);
        }

        var invalidPermissions = input.Permissions
            .Where(p => !AppPermissions.All.Contains(p))
            .ToList();

        if (invalidPermissions.Count > 0)
        {
            return Result.Failure(ErrorMessages.Roles.InvalidPermission);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Bulk-remove existing permission claims
        await dbContext.RoleClaims
            .Where(rc => rc.RoleId == roleId && rc.ClaimType == AppPermissions.ClaimType)
            .ExecuteDeleteAsync(cancellationToken);

        // Bulk-add new permission claims
        var distinctPermissions = input.Permissions.Distinct(StringComparer.Ordinal).ToList();
        dbContext.RoleClaims.AddRange(distinctPermissions.Select(permission =>
            new IdentityRoleClaim<Guid>
            {
                RoleId = roleId,
                ClaimType = AppPermissions.ClaimType,
                ClaimValue = permission
            }));
        await dbContext.SaveChangesAsync(cancellationToken);

        // Rotate security stamps for all users in this role to force re-auth
        await RotateSecurityStampsForRoleAsync(roleId, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Permissions updated for role '{RoleName}' (ID '{RoleId}'): [{Permissions}]",
            role.Name, roleId, string.Join(", ", input.Permissions));

        return Result.Success();
    }

    /// <inheritdoc />
    public IReadOnlyList<PermissionGroupOutput> GetAllPermissions()
    {
        return AppPermissions.ByCategory
            .Select(kvp => new PermissionGroupOutput(
                kvp.Key,
                kvp.Value.Select(p => p.Value).ToList()))
            .ToList();
    }

    /// <summary>
    /// Rotates security stamps for all users in a role, invalidating their current access tokens.
    /// Refresh tokens are intentionally preserved so the frontend can silently re-authenticate
    /// and obtain a new JWT with updated permission claims — avoiding a disruptive sign-out.
    /// </summary>
    private async Task RotateSecurityStampsForRoleAsync(Guid roleId,
        CancellationToken cancellationToken)
    {
        var userIds = await dbContext.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);

        if (userIds.Count == 0) return;

        // Load all affected users in a single query
        var users = await dbContext.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            await userManager.UpdateSecurityStampAsync(user);
            await cacheService.RemoveAsync(CacheKeys.SecurityStamp(user.Id), cancellationToken);
            await cacheService.RemoveAsync(CacheKeys.User(user.Id), cancellationToken);
        }
    }
}
