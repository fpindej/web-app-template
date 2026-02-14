using FluentValidation;

namespace MyProject.WebApi.Features.Admin.Dtos.CreateRole;

/// <summary>
/// Validates <see cref="CreateRoleRequest"/> fields at runtime.
/// </summary>
public class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    /// <summary>
    /// Initializes validation rules for role creation requests.
    /// </summary>
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^[A-Za-z][A-Za-z0-9_-]*$")
            .WithMessage("Role name must start with a letter and contain only letters, numbers, hyphens, or underscores.");

        RuleFor(x => x.Description)
            .MaximumLength(200);
    }
}
