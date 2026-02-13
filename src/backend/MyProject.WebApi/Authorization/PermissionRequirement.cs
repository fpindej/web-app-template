using Microsoft.AspNetCore.Authorization;

namespace MyProject.WebApi.Authorization;

/// <summary>
/// Authorization requirement that demands a specific permission claim.
/// </summary>
/// <param name="Permission">The permission value the user must hold (e.g. <c>"users.view"</c>).</param>
public record PermissionRequirement(string Permission) : IAuthorizationRequirement;
