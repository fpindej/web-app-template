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
            Roles: roles);

        // NOTE: UserOutput (including roles) is cached to improve performance.
        // Role or permission changes may take up to this duration to be reflected.
        await cacheService.SetAsync(cacheKey, output, TimeSpan.FromMinutes(1));

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
}
