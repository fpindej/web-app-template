using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Persistence;

namespace MyProject.Component.Tests.Services;

public class JwtTokenProviderTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MyProjectDbContext _dbContext;
    private readonly JwtTokenProvider _sut;

    public JwtTokenProviderTests()
    {
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _dbContext = TestDbContextFactory.Create();

        var authOptions = Options.Create(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
                Issuer = "test-issuer",
                Audience = "test-audience"
            }
        });

        _sut = new JwtTokenProvider(
            _userManager,
            _dbContext,
            authOptions,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero)));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    [Fact]
    public async Task GenerateAccessToken_AlwaysIncludesSecurityStampClaim()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            SecurityStamp = "stamp-value"
        };
        _userManager.GetRolesAsync(user).Returns(new List<string>());

        var token = await _sut.GenerateAccessToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var stampClaim = Assert.Single(parsed.Claims, c => c.Type == "security_stamp");
        Assert.Equal(HashHelper.Sha256("stamp-value"), stampClaim.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GenerateAccessToken_MissingSecurityStamp_Throws(string? securityStamp)
    {
        // A user without a security stamp is a broken account state; issuing a token without the
        // stamp claim would be rejected at validation, so token issuance must fail loudly instead.
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "test@example.com",
            SecurityStamp = securityStamp
        };
        _userManager.GetRolesAsync(user).Returns(new List<string>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GenerateAccessToken(user));
    }
}
