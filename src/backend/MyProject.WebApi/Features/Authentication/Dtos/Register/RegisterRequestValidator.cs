using FluentValidation;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Authentication.Dtos.Register;

/// <summary>
/// Validates <see cref="RegisterRequest"/> fields at runtime.
/// </summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    /// <summary>
    /// Initializes validation rules for registration requests.
    /// </summary>
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6)
            .MaximumLength(255)
            .Matches("[a-z]")
            .WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]")
            .WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .Matches(ValidationConstants.PhoneNumberPattern)
            .WithMessage("Phone number must be a valid format (e.g. +420123456789)")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.FirstName)
            .MaximumLength(255);

        RuleFor(x => x.LastName)
            .MaximumLength(255);
    }
}
