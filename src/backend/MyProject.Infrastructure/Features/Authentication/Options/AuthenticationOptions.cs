using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.Infrastructure.Features.Authentication.Options;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    [Required]
    public JwtOptions Jwt { get; init; } = new();

    public sealed class JwtOptions
    {
        [Required]
        public string Key { get; init; } = string.Empty;

        [Required]
        public string Issuer { get; init; } = string.Empty;

        [Required]
        public string Audience { get; init; } = string.Empty;

        [Range(1, 120)]
        public int ExpiresInMinutes { get; init; } = 10;

        public RefreshTokenOptions RefreshToken { get; init; } = new();

        public string SecurityStampClaimType { get; init; } = "security_stamp";

        public sealed class RefreshTokenOptions
        {
            [Range(1, 365)]
            public int ExpiresInDays { get; [UsedImplicitly] init; } = 7;
        }
    }
}
