using FluentValidation;

namespace MyProject.WebApi.Features.Admin.Dtos.ListUsers;

/// <summary>
/// Validates <see cref="ListUsersRequest"/> fields at runtime.
/// </summary>
public class ListUsersRequestValidator : AbstractValidator<ListUsersRequest>
{
    /// <summary>
    /// Initializes validation rules for user list requests.
    /// </summary>
    public ListUsersRequestValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => x.Search is not null);
    }
}
