using System.Text.RegularExpressions;

namespace MyProject.WebApi.Routing;

/// <summary>
/// Route constraint that validates a Hangfire job identifier: letters, digits, dots, hyphens, or underscores.
/// Max length 100 characters.
/// </summary>
public partial class JobIdRouteConstraint : IRouteConstraint
{
    /// <inheritdoc />
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey,
        RouteValueDictionary values, RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is not string jobId)
        {
            return false;
        }

        return jobId.Length <= 100 && Pattern().IsMatch(jobId);
    }

    [GeneratedRegex(@"^[A-Za-z0-9._-]+$")]
    private static partial Regex Pattern();
}
