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
using MyProject.Infrastructure.Persistence.Extensions;

namespace MyProject.Infrastructure.Features.Admin.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IAdminService"/> for administrative user and role management.
/// <para>
/// All mutation operations enforce role hierarchy: the caller must have a strictly higher role rank
/// than the target user. Self-action protection and last-admin guards are applied at this layer
/// to ensure consistent enforcement regardless of the consumer (controller, background job, etc.).
/// </para>
/// <para>
/// Destructive actions (lock, role removal, deletion) revoke all active refresh tokens for the
/// affected user and rotate their security stamp to invalidate in-flight access tokens.
/// </para>
/// </summary>
internal class AdminService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    MyProjectDbContext dbContext,
    ICacheService cacheService,
    TimeProvider timeProvider,
    ILogger<AdminService> logger) : IAdminService
{
    /// <inheritdoc />
    public async Task<AdminUserListOutput> GetUsersAsync(int pageNumber, int pageSize, string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Paginate(pageNumber, pageSize)
            .ToListAsync(cancellationToken);

        var userOutputs = await MapUsersToOutputsAsync(users, cancellationToken);

        return new AdminUserListOutput(userOutputs, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<AdminUserOutput> GetUserByIdAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        return await MapUserToOutputAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result> AssignRoleAsync(Guid callerUserId, Guid userId, AssignRoleInput input,
        CancellationToken cancellationToken = default)
    {
        if (!AppRoles.All.Contains(input.Role))
        {
            return Result.Failure($"Role '{input.Role}' does not exist.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var callerRoles = await GetUserRolesAsync(callerUserId);
        var callerRank = AppRoles.GetHighestRank(callerRoles);

        if (AppRoles.GetRoleRank(input.Role) >= callerRank)
        {
            return Result.Failure(ErrorMessages.Admin.RoleAssignAboveRank);
        }

        if (await userManager.IsInRoleAsync(user, input.Role))
        {
            return Result.Failure($"User already has the '{input.Role}' role.");
        }

        var result = await userManager.AddToRoleAsync(user, input.Role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Role '{Role}' assigned to user '{UserId}' by admin '{CallerUserId}'",
            input.Role, userId, callerUserId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveRoleAsync(Guid callerUserId, Guid userId, string role,
        CancellationToken cancellationToken = default)
    {
        if (!AppRoles.All.Contains(role))
        {
            return Result.Failure($"Role '{role}' does not exist.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.RoleSelfRemove);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var callerRoles = await GetUserRolesAsync(callerUserId);
        var callerRank = AppRoles.GetHighestRank(callerRoles);

        if (AppRoles.GetRoleRank(role) >= callerRank)
        {
            return Result.Failure(ErrorMessages.Admin.RoleRemoveAboveRank);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            return Result.Failure($"User does not have the '{role}' role.");
        }

        var lastAdminResult = await EnforceLastAdminProtectionAsync(userId, role, cancellationToken);
        if (!lastAdminResult.IsSuccess)
        {
            return lastAdminResult;
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await RevokeUserSessionsAsync(user, userId, cancellationToken);
        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Role '{Role}' removed from user '{UserId}' by admin '{CallerUserId}'",
            role, userId, callerUserId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> LockUserAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.LockSelfAction);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        // Set lockout end to 100 years in the future (effectively permanent)
        var lockoutEnd = timeProvider.GetUtcNow().AddYears(100);
        var result = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await RevokeUserSessionsAsync(user, userId, cancellationToken);
        await InvalidateUserCacheAsync(userId);
        logger.LogWarning("User '{UserId}' has been locked out by admin '{CallerUserId}'",
            userId, callerUserId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UnlockUserAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var result = await userManager.SetLockoutEndDateAsync(user, null);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        // Reset access failed count
        await userManager.ResetAccessFailedCountAsync(user);

        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("User '{UserId}' has been unlocked by admin '{CallerUserId}'",
            userId, callerUserId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteUserAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{userId}' was not found.");
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.DeleteSelfAction);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var targetRoles = await userManager.GetRolesAsync(user);
        var lastAdminCheckResult = await EnforceLastAdminProtectionForDeletionAsync(
            targetRoles, cancellationToken);
        if (!lastAdminCheckResult.IsSuccess)
        {
            return lastAdminCheckResult;
        }

        await RevokeUserSessionsAsync(user, userId, cancellationToken);

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogWarning("User '{UserId}' has been deleted by admin '{CallerUserId}'",
            userId, callerUserId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRoleOutput>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var roleCounts = await dbContext.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return roles
            .Select(role => new AdminRoleOutput(
                role.Id,
                role.Name ?? string.Empty,
                roleCounts.GetValueOrDefault(role.Id)))
            .ToList();
    }

    /// <summary>
    /// Verifies that the caller has a strictly higher role rank than the target user.
    /// Returns <see cref="Result.Failure(string)"/> if the hierarchy check fails.
    /// </summary>
    private async Task<Result> EnforceHierarchyAsync(Guid callerUserId, ApplicationUser targetUser)
    {
        var callerRoles = await GetUserRolesAsync(callerUserId);
        var targetRoles = await userManager.GetRolesAsync(targetUser);

        var callerRank = AppRoles.GetHighestRank(callerRoles);
        var targetRank = AppRoles.GetHighestRank(targetRoles);

        if (callerRank <= targetRank)
        {
            return Result.Failure(ErrorMessages.Admin.HierarchyInsufficient);
        }

        return Result.Success();
    }

    /// <summary>
    /// Prevents removal of an administrative role if the target user is the last user with that role.
    /// Only applies to Admin and SuperAdmin roles.
    /// </summary>
    private async Task<Result> EnforceLastAdminProtectionAsync(Guid userId, string role,
        CancellationToken cancellationToken)
    {
        if (role is not (AppRoles.Admin or AppRoles.SuperAdmin))
        {
            return Result.Success();
        }

        var roleEntity = await roleManager.FindByNameAsync(role);
        if (roleEntity is null)
        {
            return Result.Success();
        }

        var usersInRoleCount = await dbContext.UserRoles
            .CountAsync(ur => ur.RoleId == roleEntity.Id, cancellationToken);

        if (usersInRoleCount <= 1)
        {
            return Result.Failure($"Cannot remove the '{role}' role — this is the last user with this role.");
        }

        return Result.Success();
    }

    /// <summary>
    /// Prevents deletion of a user if they are the last user holding any administrative role
    /// (Admin or SuperAdmin).
    /// </summary>
    private async Task<Result> EnforceLastAdminProtectionForDeletionAsync(
        IList<string> targetRoles, CancellationToken cancellationToken)
    {
        foreach (var role in targetRoles.Where(r => r is AppRoles.Admin or AppRoles.SuperAdmin))
        {
            var roleEntity = await roleManager.FindByNameAsync(role);
            if (roleEntity is null) continue;

            var usersInRoleCount = await dbContext.UserRoles
                .CountAsync(ur => ur.RoleId == roleEntity.Id, cancellationToken);

            if (usersInRoleCount <= 1)
            {
                return Result.Failure(
                    $"Cannot delete this user — they are the last user with the '{role}' role.");
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// Revokes all active refresh tokens for a user and rotates their security stamp,
    /// forcing re-authentication on all devices.
    /// </summary>
    private async Task RevokeUserSessionsAsync(ApplicationUser user, Guid userId,
        CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.Invalidated)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Invalidated = true;
        }

        if (tokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await userManager.UpdateSecurityStampAsync(user);
    }

    private async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        return await userManager.GetRolesAsync(user);
    }

    private async Task<IReadOnlyList<AdminUserOutput>> MapUsersToOutputsAsync(
        IReadOnlyList<ApplicationUser> users, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var userIds = users.Select(u => u.Id).ToList();

        var userRolesMap = await dbContext.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name ?? string.Empty })
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.RoleName).ToList(),
                cancellationToken);

        return users.Select(user =>
        {
            var roles = userRolesMap.GetValueOrDefault(user.Id, []);
            var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > now;

            return new AdminUserOutput(
                Id: user.Id,
                UserName: user.UserName ?? string.Empty,
                FirstName: user.FirstName,
                LastName: user.LastName,
                PhoneNumber: user.PhoneNumber,
                Bio: user.Bio,
                AvatarUrl: user.AvatarUrl,
                Roles: roles,
                EmailConfirmed: user.EmailConfirmed,
                LockoutEnabled: user.LockoutEnabled,
                LockoutEnd: user.LockoutEnd,
                AccessFailedCount: user.AccessFailedCount,
                IsLockedOut: isLockedOut);
        }).ToList();
    }

    private async Task<AdminUserOutput> MapUserToOutputAsync(ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roleNames = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id,
                (_, r) => r.Name ?? string.Empty)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > now;

        return new AdminUserOutput(
            Id: user.Id,
            UserName: user.UserName ?? string.Empty,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Bio: user.Bio,
            AvatarUrl: user.AvatarUrl,
            Roles: roleNames,
            EmailConfirmed: user.EmailConfirmed,
            LockoutEnabled: user.LockoutEnabled,
            LockoutEnd: user.LockoutEnd,
            AccessFailedCount: user.AccessFailedCount,
            IsLockedOut: isLockedOut);
    }

    private async Task InvalidateUserCacheAsync(Guid userId)
    {
        await cacheService.RemoveAsync(CacheKeys.User(userId));
    }
}
