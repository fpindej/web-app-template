using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Features.Audit;
using MyProject.Application.Identity;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class TwoFactorServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserContext _userContext;
    private readonly IAuditService _auditService;
    private readonly TwoFactorService _sut;

    private readonly Guid _userId = Guid.NewGuid();

    public TwoFactorServiceTests()
    {
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _userContext = Substitute.For<IUserContext>();
        _auditService = Substitute.For<IAuditService>();

        var authOptions = Options.Create(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
                Issuer = "test-issuer",
                Audience = "test-audience"
            },
            TwoFactor = new AuthenticationOptions.TwoFactorOptions
            {
                Issuer = "TestApp"
            }
        });

        _sut = new TwoFactorService(
            _userManager,
            _userContext,
            _auditService,
            authOptions,
            Substitute.For<ILogger<TwoFactorService>>());
    }

    public void Dispose() => _userManager.Dispose();

    private ApplicationUser CreateTestUser(bool twoFactorEnabled = false) => new()
    {
        Id = _userId,
        UserName = "test@example.com",
        Email = "test@example.com",
        TwoFactorEnabled = twoFactorEnabled
    };

    private void SetupAuthenticatedUser(ApplicationUser user)
    {
        _userContext.UserId.Returns(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
    }

    #region Setup

    [Fact]
    public async Task Setup_AuthenticatedUser_ReturnsSharedKeyAndUri()
    {
        var user = CreateTestUser();
        SetupAuthenticatedUser(user);
        _userManager.GetAuthenticatorKeyAsync(user).Returns("TESTSHAREDKEY");

        var result = await _sut.SetupAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("TESTSHAREDKEY", result.Value.SharedKey);
        Assert.Contains("otpauth://totp/", result.Value.AuthenticatorUri);
    }

    [Fact]
    public async Task Setup_GeneratesUriWithConfiguredIssuer()
    {
        var user = CreateTestUser();
        SetupAuthenticatedUser(user);
        _userManager.GetAuthenticatorKeyAsync(user).Returns("TESTSHAREDKEY");

        var result = await _sut.SetupAsync(CancellationToken.None);

        Assert.Contains("TestApp", result.Value.AuthenticatorUri);
    }

    [Fact]
    public async Task Setup_ResetsAuthenticatorKeyFirst()
    {
        var user = CreateTestUser();
        SetupAuthenticatedUser(user);
        _userManager.GetAuthenticatorKeyAsync(user).Returns("TESTSHAREDKEY");

        await _sut.SetupAsync(CancellationToken.None);

        await _userManager.Received(1).ResetAuthenticatorKeyAsync(user);
    }

    [Fact]
    public async Task Setup_TwoFactorAlreadyEnabled_ReturnsFailure()
    {
        var user = CreateTestUser(twoFactorEnabled: true);
        SetupAuthenticatedUser(user);

        var result = await _sut.SetupAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.AlreadyEnabled, result.Error);
    }

    [Fact]
    public async Task Setup_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.SetupAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Setup_UserNotFound_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.SetupAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Setup_KeyGenerationFails_ReturnsSetupFailed()
    {
        var user = CreateTestUser();
        SetupAuthenticatedUser(user);
        _userManager.GetAuthenticatorKeyAsync(user).Returns((string?)null);

        var result = await _sut.SetupAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.SetupFailed, result.Error);
    }

    #endregion

    #region VerifySetup

    [Fact]
    public async Task VerifySetup_ValidCode_EnablesTwoFactorAndReturnsRecoveryCodes()
    {
        var user = CreateTestUser();
        SetupAuthenticatedUser(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456")
            .Returns(true);
        _userManager.SetTwoFactorEnabledAsync(user, true).Returns(IdentityResult.Success);
        _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(Enumerable.Range(1, 10).Select(i => $"CODE{i:D2}"));

        var result = await _sut.VerifySetupAsync("123456", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.RecoveryCodes.Count);
        await _userManager.Received(1).SetTwoFactorEnabledAsync(user, true);
    }

    [Fact]
    public async Task VerifySetup_ValidCode_LogsAuditEvent()
    {
        var user = CreateTestUser();
        SetupAuthenticatedUser(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456")
            .Returns(true);
        _userManager.SetTwoFactorEnabledAsync(user, true).Returns(IdentityResult.Success);
        _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(new[] { "CODE01" });

        await _sut.VerifySetupAsync("123456", CancellationToken.None);

        await _auditService.Received(1).LogAsync(
            AuditActions.TwoFactorEnabled,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifySetup_InvalidCode_ReturnsVerificationFailed()
    {
        var user = CreateTestUser();
        SetupAuthenticatedUser(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "000000")
            .Returns(false);

        var result = await _sut.VerifySetupAsync("000000", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.VerificationFailed, result.Error);
    }

    [Fact]
    public async Task VerifySetup_TwoFactorAlreadyEnabled_ReturnsFailure()
    {
        var user = CreateTestUser(twoFactorEnabled: true);
        SetupAuthenticatedUser(user);

        var result = await _sut.VerifySetupAsync("123456", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.AlreadyEnabled, result.Error);
    }

    [Fact]
    public async Task VerifySetup_EnableFails_ReturnsSetupFailed()
    {
        var user = CreateTestUser();
        SetupAuthenticatedUser(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456")
            .Returns(true);
        _userManager.SetTwoFactorEnabledAsync(user, true)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Failed" }));

        var result = await _sut.VerifySetupAsync("123456", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.SetupFailed, result.Error);
    }

    [Fact]
    public async Task VerifySetup_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.VerifySetupAsync("123456", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task VerifySetup_UserNotFound_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.VerifySetupAsync("123456", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    #endregion

    #region Disable

    [Fact]
    public async Task Disable_ValidPassword_DisablesTwoFactor()
    {
        var user = CreateTestUser(twoFactorEnabled: true);
        SetupAuthenticatedUser(user);
        _userManager.CheckPasswordAsync(user, "password").Returns(true);
        _userManager.SetTwoFactorEnabledAsync(user, false).Returns(IdentityResult.Success);

        var result = await _sut.DisableAsync("password", CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _userManager.Received(1).SetTwoFactorEnabledAsync(user, false);
        await _userManager.Received(1).ResetAuthenticatorKeyAsync(user);
    }

    [Fact]
    public async Task Disable_ValidPassword_LogsAuditEvent()
    {
        var user = CreateTestUser(twoFactorEnabled: true);
        SetupAuthenticatedUser(user);
        _userManager.CheckPasswordAsync(user, "password").Returns(true);
        _userManager.SetTwoFactorEnabledAsync(user, false).Returns(IdentityResult.Success);

        await _sut.DisableAsync("password", CancellationToken.None);

        await _auditService.Received(1).LogAsync(
            AuditActions.TwoFactorDisabled,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Disable_WrongPassword_ReturnsPasswordIncorrect()
    {
        var user = CreateTestUser(twoFactorEnabled: true);
        SetupAuthenticatedUser(user);
        _userManager.CheckPasswordAsync(user, "wrong").Returns(false);

        var result = await _sut.DisableAsync("wrong", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.PasswordIncorrect, result.Error);
    }

    [Fact]
    public async Task Disable_NotEnabled_ReturnsNotEnabled()
    {
        var user = CreateTestUser(twoFactorEnabled: false);
        SetupAuthenticatedUser(user);

        var result = await _sut.DisableAsync("password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.NotEnabled, result.Error);
    }

    [Fact]
    public async Task Disable_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.DisableAsync("password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Disable_UserNotFound_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.DisableAsync("password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Disable_DisableFails_ReturnsDisableFailed()
    {
        var user = CreateTestUser(twoFactorEnabled: true);
        SetupAuthenticatedUser(user);
        _userManager.CheckPasswordAsync(user, "password").Returns(true);
        _userManager.SetTwoFactorEnabledAsync(user, false)
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Failed" }));

        var result = await _sut.DisableAsync("password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.DisableFailed, result.Error);
    }

    #endregion

    #region RegenerateRecoveryCodes

    [Fact]
    public async Task RegenerateRecoveryCodes_ValidPassword_ReturnsNewCodes()
    {
        var user = CreateTestUser(twoFactorEnabled: true);
        SetupAuthenticatedUser(user);
        _userManager.CheckPasswordAsync(user, "password").Returns(true);
        _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(Enumerable.Range(1, 10).Select(i => $"CODE{i:D2}"));

        var result = await _sut.RegenerateRecoveryCodesAsync("password", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.RecoveryCodes.Count);
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_ValidPassword_LogsAuditEvent()
    {
        var user = CreateTestUser(twoFactorEnabled: true);
        SetupAuthenticatedUser(user);
        _userManager.CheckPasswordAsync(user, "password").Returns(true);
        _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10)
            .Returns(new[] { "CODE01" });

        await _sut.RegenerateRecoveryCodesAsync("password", CancellationToken.None);

        await _auditService.Received(1).LogAsync(
            AuditActions.TwoFactorRecoveryCodesRegenerated,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_WrongPassword_ReturnsPasswordIncorrect()
    {
        var user = CreateTestUser(twoFactorEnabled: true);
        SetupAuthenticatedUser(user);
        _userManager.CheckPasswordAsync(user, "wrong").Returns(false);

        var result = await _sut.RegenerateRecoveryCodesAsync("wrong", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.PasswordIncorrect, result.Error);
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_NotEnabled_ReturnsNotEnabled()
    {
        var user = CreateTestUser(twoFactorEnabled: false);
        SetupAuthenticatedUser(user);

        var result = await _sut.RegenerateRecoveryCodesAsync("password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.NotEnabled, result.Error);
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.RegenerateRecoveryCodesAsync("password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task RegenerateRecoveryCodes_UserNotFound_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns(_userId);
        _userManager.FindByIdAsync(_userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.RegenerateRecoveryCodesAsync("password", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    #endregion
}
