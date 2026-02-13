using MyProject.Application.Features.Authentication.Dtos;
using MyProject.WebApi.Features.Users.Dtos;

namespace MyProject.WebApi.Features.Users;

/// <summary>
/// Maps between user Application layer DTOs and WebApi response DTOs.
/// </summary>
internal static class UserMapper
{
    /// <summary>
    /// Maps a <see cref="UserOutput"/> to a <see cref="UserResponse"/>.
    /// </summary>
    public static UserResponse ToResponse(this UserOutput user) => new()
    {
        Id = user.Id,
        Username = user.UserName,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        Bio = user.Bio,
        AvatarUrl = user.AvatarUrl,
        Roles = user.Roles,
        Permissions = user.Permissions
    };
}
