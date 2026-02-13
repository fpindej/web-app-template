using Microsoft.AspNetCore.Authorization;

namespace MyProject.WebApi.Authorization;

/// <summary>
/// Marks a controller action as requiring a specific permission.
/// Generates a policy name of <c>"Permission:{name}"</c> which is resolved
/// dynamically by <see cref="PermissionPolicyProvider"/>.
/// </summary>
/// <param name="permission">The required permission value (e.g. <c>"users.view"</c>).</param>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute(string permission) : AuthorizeAttribute($"Permission:{permission}");
