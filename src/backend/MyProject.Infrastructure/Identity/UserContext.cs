using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyProject.Application.Identity;

namespace MyProject.Infrastructure.Identity;

internal class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid? UserId => GetClaimValue(ClaimTypes.NameIdentifier, Guid.Parse);
    public string? Email => GetClaimValue(ClaimTypes.Email, x => x);
    public string? UserName => GetClaimValue(ClaimTypes.Name, x => x);
    public bool IsAuthenticated => UserId.HasValue;

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
