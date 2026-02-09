using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace MyProject.Infrastructure.Features.Authentication.Options;

/// <summary>
/// Root authentication configuration options.
/// Maps to the "Authentication" section in appsettings.json.
/// </summary>
public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// Gets or sets the JWT token configuration.
    /// Contains signing key, issuer, audience, and token lifetime settings.
    /// </summary>
    [Required]
    [ValidateObjectMembers]
    public JwtOptions Jwt { get; init; } = new();

    /// <summary>
    /// Configuration options for JWT token generation and validation.
    /// </summary>
    public sealed class JwtOptions
    {
        /// <summary>
        /// Gets or sets the symmetric signing key for JWT tokens.
        /// Must be at least 32 characters for HMAC-SHA256.
        /// </summary>
        [Required]
        [MinLength(32)]
        public string Key { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the issuer claim for generated JWT tokens.
        /// Must match the expected issuer during token validation.
        /// </summary>
        [Required]
        public string Issuer { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the audience claim for generated JWT tokens.
        /// Must match the expected audience during token validation.
        /// </summary>
        [Required]
        public string Audience { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the JWT access token lifetime in minutes.
        /// Defaults to 10 minutes. Must be between 1 and 120.
        /// </summary>
        [Range(1, 120)]
        public int ExpiresInMinutes { get; init; } = 10;

        /// <summary>
        /// Gets or sets the refresh token configuration.
        /// </summary>
        [ValidateObjectMembers]
        public RefreshTokenOptions RefreshToken { get; init; } = new();

        /// <summary>
        /// Gets or sets the claim type used to store the ASP.NET Identity security stamp in JWT tokens.
        /// Used to invalidate tokens when security-sensitive user data changes (password, email, etc.).
        /// </summary>
        public string SecurityStampClaimType { get; init; } = "security_stamp";

        /// <summary>
        /// Configuration options for refresh token generation and lifetime.
        /// </summary>
        public sealed class RefreshTokenOptions
        {
            /// <summary>
            /// Gets or sets the refresh token lifetime in days.
            /// Defaults to 7 days. Must be between 1 and 365.
            /// </summary>
            [Range(1, 365)]
            public int ExpiresInDays { get; [UsedImplicitly] init; } = 7;
        }
    }
}
