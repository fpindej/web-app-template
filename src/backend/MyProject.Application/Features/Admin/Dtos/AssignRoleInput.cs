namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// Input for assigning a role to a user.
/// </summary>
/// <param name="Role">The role name to assign.</param>
public record AssignRoleInput(string Role);
