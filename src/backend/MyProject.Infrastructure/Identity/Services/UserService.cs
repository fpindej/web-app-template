using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Cookies;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;
using MyProject.Application.Identity.Dtos;
using MyProject.Domain;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Identity.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IUserService"/> with Redis caching.
/// </summary>
internal class UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IUserContext userContext,
    ICacheService cacheService,
    MyProjectDbContext dbContext,
    ICookieService cookieService) : IUserService
{
    private static readonly CacheEntryOptions UserCacheOptions =
        CacheEntryOptions.AbsoluteExpireIn(TimeSpan.FromMinutes(1));

    /// <inheritdoc />
    public async Task<Result<UserOutput>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotAuthenticated);
        }

        var cacheKey = CacheKeys.User(userId.Value);
        var cachedUser = await cacheService.GetAsync<UserOutput>(cacheKey);

        if (cachedUser is not null)
        {
            return Result<UserOutput>.Success(cachedUser);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotFound);
        }

        var roles = await userManager.GetRolesAsync(user);

        var output = new UserOutput(
            Id: user.Id,
            UserName: user.UserName!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Bio: user.Bio,
            AvatarUrl: user.AvatarUrl,
            Roles: roles);

        // NOTE: UserOutput (including roles) is cached to improve performance.
        // Role or permission changes may take up to this duration to be reflected.
        await cacheService.SetAsync(cacheKey, output, UserCacheOptions);

        return Result<UserOutput>.Success(output);
    }

    /// <inheritdoc />
    public async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new List<string>();
        }
        return await userManager.GetRolesAsync(user);
    }

    /// <inheritdoc />
    public async Task<Result<UserOutput>> UpdateProfileAsync(UpdateProfileInput input, CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotAuthenticated);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result<UserOutput>.Failure(ErrorMessages.User.NotFound);
        }

        user.FirstName = input.FirstName;
        user.LastName = input.LastName;
        user.PhoneNumber = input.PhoneNumber;
        user.Bio = input.Bio;
        user.AvatarUrl = input.AvatarUrl;

        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<UserOutput>.Failure(errors);
        }

        // Invalidate cache after update
        var cacheKey = CacheKeys.User(userId.Value);
        await cacheService.RemoveAsync(cacheKey);

        var roles = await userManager.GetRolesAsync(user);

        var output = new UserOutput(
            Id: user.Id,
            UserName: user.UserName!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Bio: user.Bio,
            AvatarUrl: user.AvatarUrl,
            Roles: roles);

        return Result<UserOutput>.Success(output);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAccountAsync(DeleteAccountInput input, CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result.Failure(ErrorMessages.User.NotAuthenticated);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.User.NotFound);
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, input.Password);

        if (!passwordValid)
        {
            return Result.Failure(ErrorMessages.User.DeleteInvalidPassword);
        }

        var lastAdminResult = await EnforceLastAdminProtectionForDeletionAsync(user, cancellationToken);
        if (!lastAdminResult.IsSuccess)
        {
            return lastAdminResult;
        }

        await RevokeUserTokens(user, userId.Value, cancellationToken);
        await DeleteUser(user);
        ClearAuthCookies();
        await InvalidateUserCache(userId.Value);

        return Result.Success();
    }

    /// <summary>
    /// Prevents self-deletion if the user is the last holder of any administrative role.
    /// </summary>
    private async Task<Result> EnforceLastAdminProtectionForDeletionAsync(
        ApplicationUser user, CancellationToken cancellationToken)
    {
        var userRoles = await userManager.GetRolesAsync(user);

        foreach (var role in userRoles.Where(r => r is AppRoles.Admin or AppRoles.SuperAdmin))
        {
            var roleEntity = await roleManager.FindByNameAsync(role);
            if (roleEntity is null) continue;

            var usersInRoleCount = await dbContext.UserRoles
                .CountAsync(ur => ur.RoleId == roleEntity.Id, cancellationToken);

            if (usersInRoleCount <= 1)
            {
                return Result.Failure(
                    $"Cannot delete your account — you are the last user with the '{role}' role.");
            }
        }

        return Result.Success();
    }

    private async Task RevokeUserTokens(ApplicationUser user, Guid userId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.Invalidated)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Invalidated = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await userManager.UpdateSecurityStampAsync(user);
    }

    private void ClearAuthCookies()
    {
        cookieService.DeleteCookie(CookieNames.AccessToken);
        cookieService.DeleteCookie(CookieNames.RefreshToken);
    }

    private async Task InvalidateUserCache(Guid userId)
    {
        var cacheKey = CacheKeys.User(userId);
        await cacheService.RemoveAsync(cacheKey);
    }

    private async Task DeleteUser(ApplicationUser user)
    {
        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to delete user account: {errors}");
        }
    }
}
