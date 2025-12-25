using MyProject.Application.Features.Authentication.Dtos;
using MyProject.WebApi.Features.Authentication.Dtos.Register;

namespace MyProject.WebApi.Features.Authentication;

internal static class AuthMapper
{
    public static RegisterInput ToRegisterInput(this RegisterRequest request) =>
        new(
            Email: request.Email,
            Password: request.Password,
            FirstName: request.FirstName,
            LastName: request.LastName,
            PhoneNumber: request.PhoneNumber
        );
}
