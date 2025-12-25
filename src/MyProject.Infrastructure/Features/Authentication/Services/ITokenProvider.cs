using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Provider interface for generating authentication tokens.
/// </summary>
public interface ITokenProvider
{
    /// <summary>
    /// Generates an access token for the specified user.
    /// </summary>
    /// <param name="user">The user for whom to generate the access token.</param>
    /// <returns>A string representing the generated access token.</returns>
    Task<string> GenerateAccessToken(ApplicationUser user);

    /// <summary>
    /// Generates a refresh token.
    /// </summary>
    /// <returns>A string representing the generated refresh token.</returns>
    string GenerateRefreshToken();
}

