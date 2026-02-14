using FluentValidation;

namespace MyProject.WebApi.Features.Admin.Dtos.UpdateRole;

/// <summary>
/// Validates <see cref="UpdateRoleRequest"/> fields at runtime.
/// </summary>
public class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    /// <summary>
    /// Initializes validation rules for role update requests.
    /// </summary>
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Description is not null)
            .WithMessage("At least one field must be provided.");

        RuleFor(x => x.Name)
            .MaximumLength(50)
            .Matches(@"^[A-Za-z][A-Za-z0-9_-]*$")
            .WithMessage("Role name must start with a letter and contain only letters, numbers, hyphens, or underscores.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Description)
            .MaximumLength(200)
            .When(x => x.Description is not null);
    }
}
