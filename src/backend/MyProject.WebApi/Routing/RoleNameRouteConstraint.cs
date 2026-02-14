using System.Text.RegularExpressions;

namespace MyProject.WebApi.Routing;

/// <summary>
/// Route constraint that validates a role name: starts with a letter, followed by letters, digits, hyphens, or underscores.
/// Max length 50 characters. Matches the validation in <c>CreateRoleRequestValidator</c> and <c>AssignRoleRequestValidator</c>.
/// </summary>
public partial class RoleNameRouteConstraint : IRouteConstraint
{
    /// <inheritdoc />
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is not string roleName)
        {
            return false;
        }

        return roleName.Length <= 50 && Pattern().IsMatch(roleName);
    }

    [GeneratedRegex(@"^[A-Za-z][A-Za-z0-9_-]*$")]
    private static partial Regex Pattern();
}
