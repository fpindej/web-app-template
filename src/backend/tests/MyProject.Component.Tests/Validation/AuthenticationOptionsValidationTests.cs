using System.ComponentModel.DataAnnotations;
using MyProject.Infrastructure.Features.Authentication.Options;

namespace MyProject.Component.Tests.Validation;

public class AuthenticationOptionsValidationTests
{
    private static List<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    #region JwtOptions.Key

    [Fact]
    public void JwtOptions_Key_ValidLength_NoErrors()
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = "test",
            Audience = "test"
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.Key)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("this-key-is-exactly-31-chars!!!")]
    [InlineData("this-key-is-63-chars-one-below-the-hmac-sha256-minimum-of-64!!!")]
    public void JwtOptions_Key_EmptyOrTooShort_ReturnsError(string key)
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = key,
            Issuer = "test",
            Audience = "test"
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.Key)));
    }

    #endregion

    #region JwtOptions.Issuer

    [Fact]
    public void JwtOptions_Issuer_Valid_NoErrors()
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = "MyProjectIssuer",
            Audience = "test"
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.Issuer)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void JwtOptions_Issuer_EmptyOrNull_ReturnsError(string? issuer)
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = issuer!,
            Audience = "test"
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.Issuer)));
    }

    #endregion

    #region JwtOptions.Audience

    [Fact]
    public void JwtOptions_Audience_Valid_NoErrors()
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = "test",
            Audience = "MyProjectAudience"
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.Audience)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void JwtOptions_Audience_EmptyOrNull_ReturnsError(string? audience)
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = "test",
            Audience = audience!
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.Audience)));
    }

    #endregion

    #region JwtOptions.AccessTokenLifetime

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(120)]
    public void JwtOptions_AccessTokenLifetime_ValidMinutes_NoErrors(int minutes)
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = "test",
            Audience = "test",
            AccessTokenLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.AccessTokenLifetime)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    [InlineData(1440)]
    public void JwtOptions_AccessTokenLifetime_OutOfRange_ReturnsError(int minutes)
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = "test",
            Audience = "test",
            AccessTokenLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.AccessTokenLifetime)));
    }

    #endregion

    #region RefreshTokenOptions.PersistentLifetime

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(365)]
    public void RefreshTokenOptions_PersistentLifetime_ValidDays_NoErrors(int days)
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(days),
            SessionLifetime = TimeSpan.FromHours(12)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.PersistentLifetime)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void RefreshTokenOptions_PersistentLifetime_OutOfRange_ReturnsError(int days)
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(days),
            SessionLifetime = TimeSpan.FromHours(1)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.PersistentLifetime)));
    }

    #endregion

    #region RefreshTokenOptions.SessionLifetime

    [Theory]
    [InlineData(10)]
    [InlineData(60)]
    [InlineData(43200)] // 30 days in minutes
    public void RefreshTokenOptions_SessionLifetime_ValidMinutes_NoErrors(int minutes)
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(365),
            SessionLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.SessionLifetime)));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(43201)] // 30 days + 1 minute
    public void RefreshTokenOptions_SessionLifetime_OutOfRange_ReturnsError(int minutes)
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(365),
            SessionLifetime = TimeSpan.FromMinutes(minutes)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.SessionLifetime)));
    }

    [Fact]
    public void RefreshTokenOptions_SessionLifetimeExceedsPersistent_ReturnsError()
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(1),
            SessionLifetime = TimeSpan.FromDays(2)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.SessionLifetime)));
    }

    [Fact]
    public void RefreshTokenOptions_SessionLifetimeEqualsPersistent_NoErrors()
    {
        var options = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
        {
            PersistentLifetime = TimeSpan.FromDays(7),
            SessionLifetime = TimeSpan.FromDays(7)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r =>
            r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.RefreshTokenOptions.SessionLifetime))
            && r.ErrorMessage!.Contains("must not exceed"));
    }

    #endregion

    #region EmailTokenOptions.TokenLengthInBytes

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(128)]
    public void EmailTokenOptions_TokenLengthInBytes_ValidRange_NoErrors(int length)
    {
        var options = new AuthenticationOptions.EmailTokenOptions
        {
            TokenLengthInBytes = length
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.EmailTokenOptions.TokenLengthInBytes)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(129)]
    public void EmailTokenOptions_TokenLengthInBytes_OutOfRange_ReturnsError(int length)
    {
        var options = new AuthenticationOptions.EmailTokenOptions
        {
            TokenLengthInBytes = length
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.EmailTokenOptions.TokenLengthInBytes)));
    }

    #endregion

    #region EmailTokenOptions.Lifetime

    [Theory]
    [InlineData(1)]
    [InlineData(24)]
    [InlineData(168)] // 7 days
    public void EmailTokenOptions_Lifetime_ValidHours_NoErrors(int hours)
    {
        var options = new AuthenticationOptions.EmailTokenOptions
        {
            Lifetime = TimeSpan.FromHours(hours)
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.EmailTokenOptions.Lifetime)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(169)] // 7 days + 1 hour
    public void EmailTokenOptions_Lifetime_OutOfRange_ReturnsError(int hours)
    {
        var options = new AuthenticationOptions.EmailTokenOptions
        {
            Lifetime = TimeSpan.FromHours(hours)
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.EmailTokenOptions.Lifetime)));
    }

    #endregion

    #region JwtOptions.SecurityStampClaimType

    [Theory]
    [InlineData("security_stamp")]
    [InlineData("sec_stamp")]
    [InlineData("custom_claim")]
    public void JwtOptions_SecurityStampClaimType_ValidValue_NoErrors(string claimType)
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = "test",
            Audience = "test",
            SecurityStampClaimType = claimType
        };

        var results = Validate(options);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.SecurityStampClaimType)));
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("email")]
    [InlineData("jti")]
    [InlineData("unique_name")]
    [InlineData("iss")]
    [InlineData("aud")]
    [InlineData("exp")]
    [InlineData("nbf")]
    [InlineData("iat")]
    [InlineData("role")]
    [InlineData("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")]
    [InlineData("permission")]
    [InlineData("SUB")]
    [InlineData("Email")]
    public void JwtOptions_SecurityStampClaimType_ReservedClaimName_ReturnsError(string claimType)
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = "test",
            Audience = "test",
            SecurityStampClaimType = claimType
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.SecurityStampClaimType)));
    }

    [Fact]
    public void JwtOptions_SecurityStampClaimType_Empty_ReturnsError()
    {
        var options = new AuthenticationOptions.JwtOptions
        {
            Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
            Issuer = "test",
            Audience = "test",
            SecurityStampClaimType = string.Empty
        };

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(AuthenticationOptions.JwtOptions.SecurityStampClaimType)));
    }

    #endregion
}
