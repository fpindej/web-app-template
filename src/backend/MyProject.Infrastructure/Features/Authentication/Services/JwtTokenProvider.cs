using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// HMAC-SHA256 JWT implementation of <see cref="ITokenProvider"/>.
/// </summary>
internal class JwtTokenProvider(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<AuthenticationOptions> authenticationOptions,
    TimeProvider timeProvider) : ITokenProvider
{
    private readonly AuthenticationOptions.JwtOptions _jwtOptions = authenticationOptions.Value.Jwt;

    /// <inheritdoc />
    public async Task<string> GenerateAccessToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_jwtOptions.ExpiresInMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.SecurityStamp))
        {
            claims.Add(new Claim(_jwtOptions.SecurityStampClaimType, HashHelper.Sha256(user.SecurityStamp)));
        }

        var userRoles = await userManager.GetRolesAsync(user);
        claims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

        var permissions = await GetPermissionsForRolesAsync(userRoles);
        claims.AddRange(permissions.Select(p => new Claim(AppPermissions.ClaimType, p)));

        var token = new JwtSecurityToken(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
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
    /// </summary>
    private async Task<HashSet<string>> GetPermissionsForRolesAsync(IList<string> roleNames)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleName in roleNames)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null) continue;

            var roleClaims = await roleManager.GetClaimsAsync(role);
            foreach (var claim in roleClaims.Where(c => c.Type == AppPermissions.ClaimType))
            {
                permissions.Add(claim.Value);
            }
        }

        return permissions;
    }
}
