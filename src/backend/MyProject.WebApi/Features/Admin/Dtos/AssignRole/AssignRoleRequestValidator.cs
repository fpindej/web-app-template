using FluentValidation;

namespace MyProject.WebApi.Features.Admin.Dtos.AssignRole;

/// <summary>
/// Validates <see cref="AssignRoleRequest"/> fields at runtime.
/// </summary>
public class AssignRoleRequestValidator : AbstractValidator<AssignRoleRequest>
{
    /// <summary>
    /// Initializes validation rules for role assignment requests.
    /// </summary>
    public AssignRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .MaximumLength(50);
    }
}
