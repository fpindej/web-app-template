using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// HMAC-SHA256 JWT implementation of <see cref="ITokenProvider"/>.
/// </summary>
internal class JwtTokenProvider(
    UserManager<ApplicationUser> userManager,
    MyProjectDbContext dbContext,
    IOptions<AuthenticationOptions> authenticationOptions,
    TimeProvider timeProvider) : ITokenProvider
{
    private readonly AuthenticationOptions.JwtOptions _jwtOptions = authenticationOptions.Value.Jwt;

    /// <inheritdoc />
    public async Task<string> GenerateAccessToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.Add(_jwtOptions.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (string.IsNullOrEmpty(user.SecurityStamp))
        {
            // Identity always assigns a security stamp on user creation; a missing stamp is a broken
            // account state. Tokens without the stamp claim are rejected at validation, so refuse to
            // issue one rather than silently omitting the claim.
            throw new InvalidOperationException($"User '{user.Id}' has no security stamp; cannot issue an access token.");
        }

        claims.Add(new Claim(_jwtOptions.SecurityStampClaimType, HashHelper.Sha256(user.SecurityStamp)));

        var userRoles = await userManager.GetRolesAsync(user);
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var permissions = await GetPermissionsForRolesAsync(userRoles);
        claims.AddRange(permissions.Select(p => new Claim(AppPermissions.ClaimType, p)));

        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(GenerateSecureToken(32));
    }

    private static byte[] GenerateSecureToken(int length)
    {
        var randomBytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return randomBytes;
    }

    /// <summary>
    /// Collects deduplicated permission claim values from all of the user's roles.
    /// When any role grants all permissions, a single <see cref="AppPermissions.Wildcard"/>
    /// claim is emitted instead of per-permission claims, keeping the token constant-size
    /// and never stale when new permissions are added.
    /// </summary>
    private async Task<HashSet<string>> GetPermissionsForRolesAsync(IList<string> roleNames)
    {
        var normalizedNames = roleNames
            .Select(r => r.ToUpperInvariant())
            .ToList();

        var grantsAll = await dbContext.Roles
            .AnyAsync(r => normalizedNames.Contains(r.NormalizedName!) && r.GrantsAllPermissions);

        if (grantsAll)
        {
            return [AppPermissions.Wildcard];
        }

        var permissions = await dbContext.RoleClaims
            .Join(dbContext.Roles,
                rc => rc.RoleId,
                r => r.Id,
                (rc, r) => new { r.NormalizedName, rc.ClaimType, rc.ClaimValue })
            .Where(x => normalizedNames.Contains(x.NormalizedName!)
                        && x.ClaimType == AppPermissions.ClaimType)
            .Select(x => x.ClaimValue!)
            .Distinct()
            .ToListAsync();

        return permissions.ToHashSet(StringComparer.Ordinal);
    }
}
