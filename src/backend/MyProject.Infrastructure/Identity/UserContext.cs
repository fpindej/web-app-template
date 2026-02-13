using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyProject.Application.Identity;

namespace MyProject.Infrastructure.Identity;

/// <summary>
/// Extracts the current user's identity from <see cref="IHttpContextAccessor"/> JWT claims.
/// </summary>
internal class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(value, out var guid))
            {
                return guid;
            }

            return null;
        }
    }

    /// <inheritdoc />
    public string? Email => GetClaimValue(ClaimTypes.Email, x => x);

    /// <inheritdoc />
    public string? UserName => GetClaimValue(ClaimTypes.Name, x => x);

    /// <inheritdoc />
    public Guid AuthenticatedUserId =>
        UserId ?? throw new InvalidOperationException("No authenticated user. This property should only be accessed on authenticated endpoints.");

    /// <inheritdoc />
    public bool IsAuthenticated => UserId.HasValue;

    /// <inheritdoc />
    public bool IsInRole(string role)
    {
        return httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
    }

    private T? GetClaimValue<T>(string claimType, Func<string, T> converter)
    {
        var value = httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;

        if (value is not null)
        {
            return converter(value);
        }

        return default;
    }
}
