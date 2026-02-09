namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// Output representing a role with its user count for admin views.
/// </summary>
/// <param name="Id">The role's unique identifier.</param>
/// <param name="Name">The role name.</param>
/// <param name="UserCount">The number of users assigned to this role.</param>
public record AdminRoleOutput(
    Guid Id,
    string Name,
    int UserCount
);
