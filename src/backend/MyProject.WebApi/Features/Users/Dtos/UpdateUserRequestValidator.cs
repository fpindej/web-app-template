using FluentValidation;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Users.Dtos;

/// <summary>
/// Validates <see cref="UpdateUserRequest"/> fields at runtime.
/// </summary>
public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    /// <summary>
    /// Initializes validation rules for profile update requests.
    /// </summary>
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(255);

        RuleFor(x => x.LastName)
            .MaximumLength(255);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20)
            .Matches(ValidationConstants.PhoneNumberPattern)
            .WithMessage("Phone number must be a valid format (e.g. +420123456789)")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        RuleFor(x => x.Bio)
            .MaximumLength(1000);

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Avatar URL must be a valid absolute HTTP or HTTPS URL.")
            .When(x => !string.IsNullOrEmpty(x.AvatarUrl));
    }
}
