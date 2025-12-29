using MyProject.Application.Features.Authentication.Dtos;
using MyProject.WebApi.Features.Users.Dtos;

namespace MyProject.WebApi.Features.Users;

public static class UserMapper
{
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
        Roles = user.Roles
    };
}
