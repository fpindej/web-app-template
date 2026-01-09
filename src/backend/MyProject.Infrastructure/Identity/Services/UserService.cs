using Microsoft.AspNetCore.Identity;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity;
using MyProject.Domain;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Identity.Services;

internal class UserService(
    UserManager<ApplicationUser> userManager,
    IUserContext userContext,
    ICacheService cacheService) : IUserService
{
    private static readonly CacheEntryOptions UserCacheOptions =
        CacheEntryOptions.AbsoluteExpireIn(TimeSpan.FromMinutes(1));

    public async Task<Result<UserOutput>> GetCurrentUserAsync()
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure("User is not authenticated.");
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
            return Result<UserOutput>.Failure("User not found.");
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

    public async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return new List<string>();
        }
        return await userManager.GetRolesAsync(user);
    }

    public async Task<Result<UserOutput>> UpdateProfileAsync(UpdateProfileInput input)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure("User is not authenticated.");
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result<UserOutput>.Failure("User not found.");
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
}
