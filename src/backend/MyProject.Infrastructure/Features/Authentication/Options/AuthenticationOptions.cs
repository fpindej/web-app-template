using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using MyProject.Application.Identity.Constants;

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
    /// Gets or sets the email token configuration for opaque password-reset and email-verification links.
    /// </summary>
    [ValidateObjectMembers]
    public EmailTokenOptions EmailToken { get; init; } = new();

    /// <summary>
    /// Gets or sets the two-factor authentication configuration.
    /// </summary>
    [ValidateObjectMembers]
    public TwoFactorOptions TwoFactor { get; init; } = new();

    /// <summary>
    /// Configuration options for JWT token generation and validation.
    /// </summary>
    public sealed class JwtOptions : IValidatableObject
    {
        /// <summary>
        /// Gets or sets the symmetric signing key for JWT tokens.
        /// Must be at least 64 characters to provide a full-entropy HMAC-SHA256 key.
        /// </summary>
        [Required]
        [MinLength(64)]
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
        /// Gets or sets the JWT access token lifetime.
        /// Defaults to 10 minutes. Valid range: 1 minute – 2 hours.
        /// </summary>
        public TimeSpan AccessTokenLifetime { get; [UsedImplicitly] init; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the refresh token configuration.
        /// </summary>
        [ValidateObjectMembers]
        public RefreshTokenOptions RefreshToken { get; init; } = new();

        /// <summary>
        /// Gets or sets the claim type used to store the ASP.NET Identity security stamp in JWT tokens.
        /// Used to invalidate tokens when security-sensitive user data changes (password, email, etc.).
        /// Must not collide with registered JWT claim names or other claim types used in the token.
        /// </summary>
        [Required]
        public string SecurityStampClaimType { get; init; } = "security_stamp";

        /// <summary>
        /// Registered JWT claim names and other claim types used in the access token.
        /// <see cref="SecurityStampClaimType"/> must not collide with any of these.
        /// </summary>
        private static readonly HashSet<string> ReservedClaimTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            JwtRegisteredClaimNames.Sub,
            JwtRegisteredClaimNames.Email,
            JwtRegisteredClaimNames.Jti,
            JwtRegisteredClaimNames.UniqueName,
            JwtRegisteredClaimNames.Iss,
            JwtRegisteredClaimNames.Aud,
            JwtRegisteredClaimNames.Exp,
            JwtRegisteredClaimNames.Nbf,
            JwtRegisteredClaimNames.Iat,
            "role",
            ClaimTypes.Role,
            AppPermissions.ClaimType
        };

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (AccessTokenLifetime < TimeSpan.FromMinutes(1) || AccessTokenLifetime > TimeSpan.FromHours(2))
            {
                yield return new ValidationResult(
                    $"AccessTokenLifetime must be between 1 minute and 2 hours, but was {AccessTokenLifetime}.",
                    [nameof(AccessTokenLifetime)]);
            }

            if (ReservedClaimTypes.Contains(SecurityStampClaimType))
            {
                yield return new ValidationResult(
                    $"SecurityStampClaimType '{SecurityStampClaimType}' collides with a registered JWT claim name or built-in claim type.",
                    [nameof(SecurityStampClaimType)]);
            }
        }

        /// <summary>
        /// Configuration options for refresh token generation and lifetime.
        /// </summary>
        public sealed class RefreshTokenOptions : IValidatableObject
        {
            /// <summary>
            /// Gets or sets the refresh token lifetime for persistent (remember-me) sessions.
            /// Defaults to 7 days. Valid range: 1 day – 365 days.
            /// </summary>
            public TimeSpan PersistentLifetime { get; [UsedImplicitly] init; } = TimeSpan.FromDays(7);

            /// <summary>
            /// Gets or sets the refresh token lifetime for non-persistent (session) logins.
            /// Defaults to 24 hours. Valid range: 10 minutes – 30 days. Must be ≤ PersistentLifetime.
            /// </summary>
            public TimeSpan SessionLifetime { get; [UsedImplicitly] init; } = TimeSpan.FromHours(24);

            /// <inheritdoc />
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (PersistentLifetime < TimeSpan.FromDays(1) || PersistentLifetime > TimeSpan.FromDays(365))
                {
                    yield return new ValidationResult(
                        $"PersistentLifetime must be between 1 day and 365 days, but was {PersistentLifetime}.",
                        [nameof(PersistentLifetime)]);
                }

                if (SessionLifetime < TimeSpan.FromMinutes(10) || SessionLifetime > TimeSpan.FromDays(30))
                {
                    yield return new ValidationResult(
                        $"SessionLifetime must be between 10 minutes and 30 days, but was {SessionLifetime}.",
                        [nameof(SessionLifetime)]);
                }

                if (SessionLifetime > PersistentLifetime)
                {
                    yield return new ValidationResult(
                        $"SessionLifetime ({SessionLifetime}) must not exceed PersistentLifetime ({PersistentLifetime}).",
                        [nameof(SessionLifetime)]);
                }
            }
        }
    }

    /// <summary>
    /// Configuration options for two-factor authentication.
    /// </summary>
    public sealed class TwoFactorOptions : IValidatableObject
    {
        /// <summary>
        /// Gets or sets the lifetime of a two-factor challenge token.
        /// Defaults to 5 minutes. Valid range: 1 minute – 15 minutes.
        /// </summary>
        public TimeSpan ChallengeLifetime { get; [UsedImplicitly] init; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the issuer name displayed in authenticator apps.
        /// Appears as the account label prefix in TOTP applications.
        /// </summary>
        [Required]
        public string Issuer { get; init; } = "MyProject";

        /// <summary>
        /// Gets or sets the maximum number of failed verification attempts per challenge.
        /// After this limit, the challenge is locked and the user must log in again.
        /// Defaults to 5.
        /// </summary>
        [Range(1, 20)]
        public int MaxChallengeAttempts { get; [UsedImplicitly] init; } = 5;

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ChallengeLifetime < TimeSpan.FromMinutes(1) || ChallengeLifetime > TimeSpan.FromMinutes(15))
            {
                yield return new ValidationResult(
                    $"ChallengeLifetime must be between 1 minute and 15 minutes, but was {ChallengeLifetime}.",
                    [nameof(ChallengeLifetime)]);
            }
        }
    }

    /// <summary>
    /// Configuration options for opaque email tokens used in password-reset and email-verification links.
    /// </summary>
    public sealed class EmailTokenOptions : IValidatableObject
    {
        /// <summary>
        /// Gets or sets the length of the random token in bytes.
        /// Defaults to 32 (256-bit). The resulting URL token is a hex string of twice this length.
        /// </summary>
        [Range(16, 128)]
        public int TokenLengthInBytes { get; [UsedImplicitly] init; } = 32;

        /// <summary>
        /// Gets or sets the email token lifetime.
        /// Defaults to 24 hours. Valid range: 1 hour – 7 days.
        /// </summary>
        public TimeSpan Lifetime { get; [UsedImplicitly] init; } = TimeSpan.FromHours(24);

        /// <inheritdoc />
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Lifetime < TimeSpan.FromHours(1) || Lifetime > TimeSpan.FromDays(7))
            {
                yield return new ValidationResult(
                    $"Lifetime must be between 1 hour and 7 days, but was {Lifetime}.",
                    [nameof(Lifetime)]);
            }
        }
    }
}
