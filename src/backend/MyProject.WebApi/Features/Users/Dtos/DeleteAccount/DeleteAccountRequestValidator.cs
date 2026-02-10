using FluentValidation;

namespace MyProject.WebApi.Features.Users.Dtos.DeleteAccount;

/// <summary>
/// Validates <see cref="DeleteAccountRequest"/> fields at runtime.
/// </summary>
public class DeleteAccountRequestValidator : AbstractValidator<DeleteAccountRequest>
{
    /// <summary>
    /// Initializes validation rules for account deletion requests.
    /// </summary>
    public DeleteAccountRequestValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty()
            .MaximumLength(255);
    }
}
