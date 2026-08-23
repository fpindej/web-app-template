using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Caching.Hybrid;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Email;
using MyProject.Application.Features.Email.Models;
using MyProject.Application.Features.FileStorage;
using MyProject.Application.Identity.Constants;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Admin.Services;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class AdminServiceDisableTwoFactorTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly HybridCache _hybridCache;
    private readonly ITemplatedEmailSender _templatedEmailSender;
    private readonly IAuditService _auditService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly MyProjectDbContext _dbContext;
    private readonly AdminService _sut;

    private readonly Guid _callerId = Guid.NewGuid();
    private readonly Guid _targetId = Guid.NewGuid();

    public AdminServiceDisableTwoFactorTests()
    {
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _roleManager = IdentityMockHelpers.CreateMockRoleManager();
        _hybridCache = Substitute.For<HybridCache>();
        _templatedEmailSender = Substitute.For<ITemplatedEmailSender>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
        _dbContext = TestDbContextFactory.Create();
        var logger = Substitute.For<ILogger<AdminService>>();
        var authOptions = Options.Create(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
                Issuer = "test-issuer",
                Audience = "test-audience"
            }
        });
        var emailOptions = Options.Create(new EmailOptions { FrontendBaseUrl = "https://example.com" });
        var emailTokenService = new EmailTokenService(_dbContext, _timeProvider, authOptions);
        _auditService = Substitute.For<IAuditService>();

        var fileStorageService = Substitute.For<IFileStorageService>();

        _sut = new AdminService(
            _userManager, _roleManager, _dbContext, _hybridCache, _timeProvider,
            _templatedEmailSender, emailTokenService, _auditService,
            new PermissionEscalationGuard(_dbContext),
            fileStorageService, authOptions, emailOptions, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    private ApplicationUser SetupCallerAsAdmin()
    {
        var caller = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Admin });
        TestRoles.SeedAssigned(_dbContext, _callerId, AppRoles.Admin);
        return caller;
    }

    private ApplicationUser SetupTargetAsUser(bool twoFactorEnabled = true)
    {
        var target = new ApplicationUser
        {
            Id = _targetId,
            UserName = "user@test.com",
            Email = "user@test.com",
            EmailConfirmed = true,
            TwoFactorEnabled = twoFactorEnabled
        };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.User });
        _userManager.GetTwoFactorEnabledAsync(target).Returns(twoFactorEnabled);
        return target;
    }

    #region DisableTwoFactor

    [Fact]
    public async Task DisableTwoFactor_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetTwoFactorEnabledAsync(target, false).Returns(IdentityResult.Success);
        _userManager.ResetAuthenticatorKeyAsync(target).Returns(IdentityResult.Success);

        var result = await _sut.DisableTwoFactorAsync(_callerId, _targetId, null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DisableTwoFactor_Valid_AuditsAction()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetTwoFactorEnabledAsync(target, false).Returns(IdentityResult.Success);
        _userManager.ResetAuthenticatorKeyAsync(target).Returns(IdentityResult.Success);

        await _sut.DisableTwoFactorAsync(_callerId, _targetId, "Lost device");

        await _auditService.Received(1).LogAsync(
            AuditActions.AdminDisableTwoFactor,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: _targetId,
            metadata: Arg.Is<string?>(m => m != null && m.Contains("Lost device")),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableTwoFactor_Valid_SendsNotificationEmail()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetTwoFactorEnabledAsync(target, false).Returns(IdentityResult.Success);
        _userManager.ResetAuthenticatorKeyAsync(target).Returns(IdentityResult.Success);

        await _sut.DisableTwoFactorAsync(_callerId, _targetId, "Lost device");

        await _templatedEmailSender.Received(1).SendSafeAsync(
            EmailTemplateNames.AdminDisableTwoFactor,
            Arg.Is<AdminDisableTwoFactorModel>(m =>
                m.UserName == "user@test.com" && m.Reason == "Lost device"),
            "user@test.com",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableTwoFactor_Valid_RevokesRefreshTokens()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetTwoFactorEnabledAsync(target, false).Returns(IdentityResult.Success);
        _userManager.ResetAuthenticatorKeyAsync(target).Returns(IdentityResult.Success);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "hashed",
            UserId = _targetId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        await _sut.DisableTwoFactorAsync(_callerId, _targetId, null);

        var token = Assert.Single(_dbContext.RefreshTokens);
        Assert.True(token.IsInvalidated);
    }

    [Fact]
    public async Task DisableTwoFactor_Valid_ResetsAuthenticatorKey()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetTwoFactorEnabledAsync(target, false).Returns(IdentityResult.Success);
        _userManager.ResetAuthenticatorKeyAsync(target).Returns(IdentityResult.Success);

        await _sut.DisableTwoFactorAsync(_callerId, _targetId, null);

        await _userManager.Received(1).ResetAuthenticatorKeyAsync(target);
    }

    [Fact]
    public async Task DisableTwoFactor_Valid_RotatesSecurityStamp()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetTwoFactorEnabledAsync(target, false).Returns(IdentityResult.Success);
        _userManager.ResetAuthenticatorKeyAsync(target).Returns(IdentityResult.Success);

        await _sut.DisableTwoFactorAsync(_callerId, _targetId, null);

        await _userManager.Received(1).UpdateSecurityStampAsync(target);
    }

    [Fact]
    public async Task DisableTwoFactor_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.DisableTwoFactorAsync(_callerId, _targetId, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DisableTwoFactor_SelfAction_ReturnsFailure()
    {
        var user = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(user);

        var result = await _sut.DisableTwoFactorAsync(_callerId, _callerId, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.DisableTwoFactorSelfAction, result.Error);
    }

    [Fact]
    public async Task DisableTwoFactor_TwoFactorNotEnabled_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        SetupTargetAsUser(twoFactorEnabled: false);

        var result = await _sut.DisableTwoFactorAsync(_callerId, _targetId, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.TwoFactorNotEnabled, result.Error);
    }

    [Fact]
    public async Task DisableTwoFactor_InsufficientHierarchy_ReturnsFailure()
    {
        // Caller is User (rank 1), target is Admin (rank 2)
        var caller = new ApplicationUser { Id = _callerId, UserName = "user@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.User });
        TestRoles.Seed(_dbContext, AppRoles.User);

        var target = new ApplicationUser { Id = _targetId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.Admin });
        TestRoles.Seed(_dbContext, AppRoles.Admin);

        var result = await _sut.DisableTwoFactorAsync(_callerId, _targetId, null);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.HierarchyInsufficient, result.Error);
    }

    [Fact]
    public async Task DisableTwoFactor_NullReason_AuditsWithNullMetadata()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetTwoFactorEnabledAsync(target, false).Returns(IdentityResult.Success);
        _userManager.ResetAuthenticatorKeyAsync(target).Returns(IdentityResult.Success);

        await _sut.DisableTwoFactorAsync(_callerId, _targetId, null);

        await _auditService.Received(1).LogAsync(
            AuditActions.AdminDisableTwoFactor,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: _targetId,
            metadata: null,
            ct: Arg.Any<CancellationToken>());
    }

    #endregion
}
