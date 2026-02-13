using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MyProject.Application.Identity.Constants;

namespace MyProject.WebApi.Authorization;

/// <summary>
/// Handles <see cref="PermissionRequirement"/> by checking:
/// <list type="number">
///   <item>SuperAdmin role → always allowed (implicit all permissions).</item>
///   <item>Matching <c>"permission"</c> claim → allowed.</item>
///   <item>Otherwise → denied.</item>
/// </list>
/// </summary>
internal class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.IsInRole(AppRoles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(AppPermissions.ClaimType, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
