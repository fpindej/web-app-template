using FluentValidation;

namespace MyProject.WebApi.Features.Authentication.Dtos.Login;

/// <summary>
/// Validates <see cref="LoginRequest"/> fields at runtime.
/// </summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    /// <summary>
    /// Initializes validation rules for login requests.
    /// </summary>
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(255);
    }
}
