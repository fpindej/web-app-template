using Microsoft.AspNetCore.Identity;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Identity;
using MyProject.Domain;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Identity.Services;

internal class UserService(
    UserManager<ApplicationUser> userManager,
    IUserContext userContext) : IUserService
{
    public async Task<Result<UserOutput>> GetCurrentUserAsync()
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

        return Result<UserOutput>.Success(new UserOutput(
            Id: user.Id,
            UserName: user.UserName!));
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
