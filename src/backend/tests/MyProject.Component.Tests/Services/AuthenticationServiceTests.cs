using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Caching.Hybrid;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Cookies;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Features.Email;
using MyProject.Application.Features.Email.Models;
using MyProject.Application.Identity;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class AuthenticationServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenProvider _tokenProvider;
    private readonly FakeTimeProvider _timeProvider;
    private readonly ICookieService _cookieService;
    private readonly IUserContext _userContext;
    private readonly HybridCache _hybridCache;
    private readonly ITemplatedEmailSender _templatedEmailSender;
    private readonly IAuditService _auditService;
    private readonly MyProjectDbContext _dbContext;
    private readonly ITokenSessionService _tokenSessionService;
    private readonly AuthenticationService _sut;

    public AuthenticationServiceTests()
    {
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _signInManager = IdentityMockHelpers.CreateMockSignInManager(_userManager);
        _tokenProvider = Substitute.For<ITokenProvider>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
        _cookieService = Substitute.For<ICookieService>();
        _userContext = Substitute.For<IUserContext>();
        _hybridCache = Substitute.For<HybridCache>();
        _templatedEmailSender = Substitute.For<ITemplatedEmailSender>();
        _dbContext = TestDbContextFactory.Create();

        var authOptions = Options.Create(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
                Issuer = "test-issuer",
                Audience = "test-audience",
                AccessTokenLifetime = TimeSpan.FromMinutes(10),
                RefreshToken = new AuthenticationOptions.JwtOptions.RefreshTokenOptions
                {
                    PersistentLifetime = TimeSpan.FromDays(7),
                    SessionLifetime = TimeSpan.FromHours(24)
                }
            }
        });

        var emailOptions = Options.Create(new EmailOptions
        {
            FromAddress = "noreply@test.com",
            FromName = "Test",
            FrontendBaseUrl = "https://test.example.com"
        });

        var emailTokenService = new EmailTokenService(_dbContext, _timeProvider, authOptions);
        _auditService = Substitute.For<IAuditService>();

        _tokenSessionService = new TokenSessionService(
            _tokenProvider, _timeProvider, _cookieService, _userManager, _hybridCache,
            authOptions, _dbContext);

        _sut = new AuthenticationService(
            _userManager,
            _signInManager,
            _timeProvider,
            _userContext,
            _templatedEmailSender,
            emailTokenService,
            _auditService,
            _tokenSessionService,
            authOptions,
            emailOptions,
            Substitute.For<ILogger<AuthenticationService>>(),
            _dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    private ApplicationUser CreateTestUser(Guid? id = null, string? userName = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        UserName = userName ?? "test@example.com",
        // Password-based accounts carry a hash; passwordless (external-only) users are created explicitly
        PasswordHash = "hashed-password"
    };

    private void SetupSuccessfulLogin(ApplicationUser user, string password = "password123")
    {
        _userManager.FindByNameAsync(user.UserName!).Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, password, true)
            .Returns(SignInResult.Success);
        _tokenProvider.GenerateAccessToken(user).Returns("access-token");
        _tokenProvider.GenerateRefreshToken().Returns("refresh-token");
    }

    private async Task<string> SeedTwoFactorChallengeAsync(
        Guid userId,
        bool isUsed = false,
        bool isExpired = false,
        bool isRememberMe = false,
        int failedAttempts = 0)
    {
        var plainToken = "test-challenge-" + Guid.NewGuid().ToString("N");
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        _dbContext.TwoFactorChallenges.Add(new TwoFactorChallenge
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(plainToken),
            UserId = userId,
            CreatedAt = utcNow.AddMinutes(-1),
            ExpiresAt = isExpired ? utcNow.AddMinutes(-1) : utcNow.AddMinutes(5),
            IsUsed = isUsed,
            IsRememberMe = isRememberMe,
            FailedAttempts = failedAttempts
        });
        await _dbContext.SaveChangesAsync();
        return plainToken;
    }

    #region Login

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccessWithTokens()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        var result = await _sut.Login("test@example.com", "password123");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.RequiresTwoFactor);
        Assert.NotNull(result.Value.Tokens);
        Assert.Equal("access-token", result.Value.Tokens.AccessToken);
        Assert.Equal("refresh-token", result.Value.Tokens.RefreshToken);
        await _auditService.Received(1).LogAsync(
            AuditActions.LoginSuccess,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_ValidCredentials_StoresRefreshTokenInDatabase()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123");

        var storedToken = Assert.Single(_dbContext.RefreshTokens);
        Assert.Equal(HashHelper.Sha256("refresh-token"), storedToken.Token);
        Assert.Equal(user.Id, storedToken.UserId);
        Assert.False(storedToken.IsUsed);
        Assert.False(storedToken.IsInvalidated);
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsCreatedAtToCurrentTime()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123");

        var storedToken = Assert.Single(_dbContext.RefreshTokens);
        Assert.Equal(_timeProvider.GetUtcNow().UtcDateTime, storedToken.CreatedAt);
    }

    [Fact]
    public async Task Login_WithRememberMe_UsesPersistentLifetime()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123", rememberMe: true);

        var storedToken = Assert.Single(_dbContext.RefreshTokens);
        var expectedExpiry = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7);
        Assert.Equal(expectedExpiry, storedToken.ExpiredAt);
    }

    [Fact]
    public async Task Login_WithoutRememberMe_UsesSessionLifetime()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123", rememberMe: false);

        var storedToken = Assert.Single(_dbContext.RefreshTokens);
        var expectedExpiry = _timeProvider.GetUtcNow().UtcDateTime.AddHours(24);
        Assert.Equal(expectedExpiry, storedToken.ExpiredAt);
    }

    [Fact]
    public async Task Login_InvalidUser_ReturnsUnauthorized()
    {
        _userManager.FindByNameAsync("unknown@example.com").Returns((ApplicationUser?)null);

        var result = await _sut.Login("unknown@example.com", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.LoginInvalidCredentials, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Login_InvalidUser_MasksAttemptedEmailInAuditMetadata()
    {
        _userManager.FindByNameAsync("unknown@example.com").Returns((ApplicationUser?)null);

        await _sut.Login("unknown@example.com", "password123");

        await _auditService.Received(1).LogAsync(
            AuditActions.LoginFailure,
            userId: Arg.Any<Guid?>(),
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Is<string?>(m =>
                m != null && m.Contains("u***@e***.com") && !m.Contains("unknown@example.com")),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_ExternalOnlyAccount_ReturnsSameGenericFailure()
    {
        // Passwordless (OAuth-only) accounts must be indistinguishable from unknown accounts
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "external@example.com",
            PasswordHash = null
        };
        _userManager.FindByNameAsync("external@example.com").Returns(user);

        var result = await _sut.Login("external@example.com", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.LoginInvalidCredentials, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        // The password sign-in pipeline is never entered for passwordless accounts
        await _signInManager.DidNotReceive().CheckPasswordSignInAsync(
            Arg.Any<ApplicationUser>(), Arg.Any<string>(), Arg.Any<bool>());
        await _auditService.Received(1).LogAsync(
            AuditActions.LoginFailure,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var user = CreateTestUser();
        _userManager.FindByNameAsync("test@example.com").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "wrong", true)
            .Returns(SignInResult.Failed);

        var result = await _sut.Login("test@example.com", "wrong");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.LoginInvalidCredentials, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
        await _auditService.Received(1).LogAsync(
            AuditActions.LoginFailure,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_LockedOut_ReturnsLockedMessage()
    {
        var user = CreateTestUser();
        _userManager.FindByNameAsync("test@example.com").Returns(user);
        _signInManager.CheckPasswordSignInAsync(user, "password123", true)
            .Returns(SignInResult.LockedOut);

        var result = await _sut.Login("test@example.com", "password123");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.LoginAccountLocked, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task Login_WithCookies_SetsCookiesWithCorrectValues()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123", useCookies: true);

        _cookieService.Received(1).SetSecureCookie(
            CookieNames.AccessToken, "access-token", Arg.Any<DateTimeOffset?>());
        _cookieService.Received(1).SetSecureCookie(
            CookieNames.RefreshToken, "refresh-token", Arg.Any<DateTimeOffset?>());
    }

    [Fact]
    public async Task Login_WithoutCookies_DoesNotSetCookies()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123", useCookies: false);

        _cookieService.DidNotReceive().SetSecureCookie(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTimeOffset?>());
    }

    [Fact]
    public async Task Login_WithRememberMe_SetsIsPersistentOnToken()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123", useCookies: true, rememberMe: true);

        var storedToken = Assert.Single(_dbContext.RefreshTokens);
        Assert.True(storedToken.IsPersistent);
    }

    [Fact]
    public async Task Login_WithRememberMe_SetsPersistentCookiesWithExpiry()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123", useCookies: true, rememberMe: true);

        // Access token cookie should have expiry (persistent)
        _cookieService.Received(1).SetSecureCookie(
            CookieNames.AccessToken, "access-token",
            Arg.Is<DateTimeOffset?>(d => d.HasValue));
        // Refresh token cookie should have expiry (persistent)
        _cookieService.Received(1).SetSecureCookie(
            CookieNames.RefreshToken, "refresh-token",
            Arg.Is<DateTimeOffset?>(d => d.HasValue));
    }

    [Fact]
    public async Task Login_WithoutRememberMe_SetsSessionCookiesWithoutExpiry()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123", useCookies: true, rememberMe: false);

        var storedToken = Assert.Single(_dbContext.RefreshTokens);
        Assert.False(storedToken.IsPersistent);

        // Session cookies: expires should be null
        _cookieService.Received(1).SetSecureCookie(
            CookieNames.AccessToken, "access-token", null);
        _cookieService.Received(1).SetSecureCookie(
            CookieNames.RefreshToken, "refresh-token", null);
    }

    #endregion

    #region Register

    [Fact]
    public async Task Register_ValidInput_ReturnsSuccess()
    {
        var input = new RegisterInput("test@example.com", "Password1!", null, null, null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password1!")
            .Returns(callInfo =>
            {
                callInfo.Arg<ApplicationUser>().Id = Guid.NewGuid();
                return IdentityResult.Success;
            });
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "User")
            .Returns(IdentityResult.Success);

        var result = await _sut.Register(input);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _auditService.Received(1).LogAsync(
            AuditActions.Register,
            userId: result.Value,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsGenericFailureWithoutIdentityDetails()
    {
        var input = new RegisterInput("test@example.com", "Password1!", null, null, null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password1!")
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateEmail",
                Description = "Email 'test@example.com' is already taken."
            }));

        var result = await _sut.Register(input);

        Assert.True(result.IsFailure);
        // Static error prevents email enumeration - Identity descriptions must never reach the client
        Assert.Equal(ErrorMessages.Auth.RegistrationInvalid, result.Error);
        Assert.DoesNotContain("test@example.com", result.Error?.Message);
        Assert.DoesNotContain("already taken", result.Error?.Message);
    }

    [Fact]
    public async Task Register_DuplicatePhone_ReturnsFailure()
    {
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "existing@example.com",
            PhoneNumber = "+420123456789"
        });
        await _dbContext.SaveChangesAsync();

        _userManager.Users.Returns(_dbContext.Users);

        var input = new RegisterInput("test@example.com", "Password1!", null, null, "+420123456789");

        var result = await _sut.Register(input);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.User.PhoneNumberTaken, result.Error);
    }

    [Fact]
    public async Task Register_NormalizesPhoneNumber()
    {
        // Users queryable needed for phone uniqueness check
        _userManager.Users.Returns(_dbContext.Users);

        var input = new RegisterInput("test@example.com", "Password1!", null, null, "+420 123 456 789");
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password1!")
            .Returns(callInfo =>
            {
                callInfo.Arg<ApplicationUser>().Id = Guid.NewGuid();
                return IdentityResult.Success;
            });
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "User")
            .Returns(IdentityResult.Success);

        await _sut.Register(input);

        await _userManager.Received(1).CreateAsync(
            Arg.Is<ApplicationUser>(u => u.PhoneNumber == "+420123456789"),
            Arg.Any<string>());
    }

    [Fact]
    public async Task Register_RoleAssignFails_ReturnsFailure()
    {
        var input = new RegisterInput("test@example.com", "Password1!", null, null, null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password1!")
            .Returns(callInfo =>
            {
                callInfo.Arg<ApplicationUser>().Id = Guid.NewGuid();
                return IdentityResult.Success;
            });
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "User")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Role error." }));

        var result = await _sut.Register(input);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.RegisterRoleAssignFailed, result.Error);
    }

    #endregion

    #region RefreshToken

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        var hashedToken = HashHelper.Sha256("valid-refresh-token");

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = hashedToken,
            UserId = userId,
            User = user,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        _tokenProvider.GenerateAccessToken(user).Returns("new-access-token");
        _tokenProvider.GenerateRefreshToken().Returns("new-refresh-token");

        var result = await _sut.RefreshTokenAsync("valid-refresh-token");

        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.Value.AccessToken);
        Assert.Equal("new-refresh-token", result.Value.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_MarksOldTokenAsUsed()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        var tokenId = Guid.NewGuid();

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = tokenId,
            Token = HashHelper.Sha256("valid-refresh-token"),
            UserId = userId,
            User = user,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false,
            IsPersistent = true
        });
        await _dbContext.SaveChangesAsync();

        _tokenProvider.GenerateAccessToken(user).Returns("new-access");
        _tokenProvider.GenerateRefreshToken().Returns("new-refresh");

        await _sut.RefreshTokenAsync("valid-refresh-token");

        var oldToken = await _dbContext.RefreshTokens.FindAsync(tokenId);
        Assert.True(oldToken!.IsUsed);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_NewTokenInheritsExpiryAndPersistence()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        var originalExpiry = _timeProvider.GetUtcNow().UtcDateTime.AddDays(5);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256("valid-refresh-token"),
            UserId = userId,
            User = user,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-2),
            ExpiredAt = originalExpiry,
            IsUsed = false,
            IsInvalidated = false,
            IsPersistent = true
        });
        await _dbContext.SaveChangesAsync();

        _tokenProvider.GenerateAccessToken(user).Returns("new-access");
        _tokenProvider.GenerateRefreshToken().Returns("new-refresh");

        await _sut.RefreshTokenAsync("valid-refresh-token");

        var newToken = await _dbContext.RefreshTokens
            .FirstAsync(rt => rt.Token == HashHelper.Sha256("new-refresh"));
        Assert.Equal(originalExpiry, newToken.ExpiredAt);
        Assert.True(newToken.IsPersistent);
        Assert.False(newToken.IsUsed);
        Assert.False(newToken.IsInvalidated);
    }

    [Fact]
    public async Task RefreshToken_WithCookies_SetsCookies()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256("valid-refresh-token"),
            UserId = userId,
            User = user,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false,
            IsPersistent = true
        });
        await _dbContext.SaveChangesAsync();

        _tokenProvider.GenerateAccessToken(user).Returns("new-access");
        _tokenProvider.GenerateRefreshToken().Returns("new-refresh");

        await _sut.RefreshTokenAsync("valid-refresh-token", useCookies: true);

        _cookieService.Received(1).SetSecureCookie(
            CookieNames.AccessToken, "new-access", Arg.Any<DateTimeOffset?>());
        _cookieService.Received(1).SetSecureCookie(
            CookieNames.RefreshToken, "new-refresh", Arg.Any<DateTimeOffset?>());
    }

    [Fact]
    public async Task RefreshToken_ExpiredToken_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "expired@test.com");
        var hashedToken = HashHelper.Sha256("expired-token");

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = hashedToken,
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-10),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-3),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.RefreshTokenAsync("expired-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenExpired, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task RefreshToken_ExpiredToken_MarksAsInvalidatedInDb()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "expired@test.com");
        var tokenId = Guid.NewGuid();

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = tokenId,
            Token = HashHelper.Sha256("expired-token"),
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-10),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-3),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        await _sut.RefreshTokenAsync("expired-token");

        var token = await _dbContext.RefreshTokens.FindAsync(tokenId);
        Assert.True(token!.IsInvalidated);
    }

    [Fact]
    public async Task RefreshToken_InvalidatedToken_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "invalidated@test.com");
        var hashedToken = HashHelper.Sha256("invalidated-token");

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = hashedToken,
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = true
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.RefreshTokenAsync("invalidated-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenInvalidated, result.Error);
    }

    [Fact]
    public async Task RefreshToken_ReusedToken_RevokesAllUserTokensInDb()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "reused@test.com");

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256("reused-token"),
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = true,
            IsInvalidated = false
        });
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256("other-valid-token"),
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        _userManager.FindByIdAsync(userId.ToString())
            .Returns(user);

        var result = await _sut.RefreshTokenAsync("reused-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenReused, result.Error);

        // All tokens for this user should be invalidated
        var allTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
        Assert.All(allTokens, t => Assert.True(t.IsInvalidated));
    }

    [Fact]
    public async Task RefreshToken_ReusedToken_RotatesSecurityStamp()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId, "reused@test.com");

        _dbContext.Users.Add(user);
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256("reused-token"),
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-1),
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = true,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        _userManager.FindByIdAsync(userId.ToString()).Returns(user);

        await _sut.RefreshTokenAsync("reused-token");

        await _userManager.Received(1).UpdateSecurityStampAsync(user);
        await _hybridCache.Received(1).RemoveAsync(
            CacheKeys.SecurityStamp(userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshToken_FailureWithCookies_ClearsCookies()
    {
        var result = await _sut.RefreshTokenAsync("nonexistent-token", useCookies: true);

        Assert.True(result.IsFailure);
        _cookieService.Received(1).DeleteCookie(CookieNames.AccessToken);
        _cookieService.Received(1).DeleteCookie(CookieNames.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_FailureWithoutCookies_DoesNotClearCookies()
    {
        var result = await _sut.RefreshTokenAsync("nonexistent-token", useCookies: false);

        Assert.True(result.IsFailure);
        _cookieService.DidNotReceive().DeleteCookie(Arg.Any<string>());
    }

    [Fact]
    public async Task RefreshToken_EmptyString_ReturnsTokenMissing()
    {
        var result = await _sut.RefreshTokenAsync("");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenMissing, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task RefreshToken_NotFound_ReturnsFailure()
    {
        var result = await _sut.RefreshTokenAsync("nonexistent-token");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.TokenNotFound, result.Error);
    }

    #endregion

    #region ChangePassword

    [Fact]
    public async Task ChangePassword_Valid_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "current").Returns(true);
        _userManager.ChangePasswordAsync(user, "current", "newPass1!")
            .Returns(IdentityResult.Success);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordInput("current", "newPass1!"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_Valid_RevokesExistingTokens()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "current").Returns(true);
        _userManager.ChangePasswordAsync(user, "current", "newPass1!")
            .Returns(IdentityResult.Success);

        // Seed a refresh token
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256("existing-token"),
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        await _sut.ChangePasswordAsync(new ChangePasswordInput("current", "newPass1!"));

        var token = Assert.Single(_dbContext.RefreshTokens);
        Assert.True(token.IsInvalidated);
        await _userManager.Received(1).UpdateSecurityStampAsync(user);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "wrong").Returns(false);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordInput("wrong", "newPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.PasswordIncorrect, result.Error);
    }

    [Fact]
    public async Task ChangePassword_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordInput("current", "newPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.NotAuthenticated, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task ChangePassword_UserNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordInput("current", "newPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.UserNotFound, result.Error);
    }

    [Fact]
    public async Task ChangePassword_IdentityFails_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.CheckPasswordAsync(user, "current").Returns(true);
        _userManager.ChangePasswordAsync(user, "current", "newPass1!")
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Password too common."
            }));

        var result = await _sut.ChangePasswordAsync(new ChangePasswordInput("current", "newPass1!"));

        Assert.True(result.IsFailure);
        // Static error - Identity descriptions must never reach the client
        Assert.Equal(ErrorMessages.Auth.PasswordPolicyViolation, result.Error);
        Assert.DoesNotContain("Password too common", result.Error?.Message);
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_WithAuthenticatedUser_ClearsCookiesAndRevokesTokens()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString())
            .Returns(CreateTestUser(userId));

        // Seed a token to verify revocation
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256("active-token"),
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        await _sut.Logout();

        _cookieService.Received(1).DeleteCookie(CookieNames.AccessToken);
        _cookieService.Received(1).DeleteCookie(CookieNames.RefreshToken);

        var token = Assert.Single(_dbContext.RefreshTokens);
        Assert.True(token.IsInvalidated);
    }

    [Fact]
    public async Task Logout_WithAuthenticatedUser_RotatesSecurityStampAndClearsCache()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);

        await _sut.Logout();

        await _userManager.Received(1).UpdateSecurityStampAsync(user);
        await _hybridCache.Received(1).RemoveAsync(
            CacheKeys.SecurityStamp(userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Logout_WithoutAuthenticatedUser_ClearsCookiesOnly()
    {
        _userContext.UserId.Returns((Guid?)null);

        await _sut.Logout();

        _cookieService.Received(1).DeleteCookie(CookieNames.AccessToken);
        _cookieService.Received(1).DeleteCookie(CookieNames.RefreshToken);
        await _userManager.DidNotReceive().UpdateSecurityStampAsync(Arg.Any<ApplicationUser>());
    }

    #endregion

    #region ForgotPassword

    [Fact]
    public async Task ForgotPassword_ExistingUser_RendersTemplateAndSendsEmail()
    {
        var user = CreateTestUser();
        _userManager.FindByEmailAsync("test@example.com").Returns(user);
        _userManager.GeneratePasswordResetTokenAsync(user).Returns("reset-token-123");

        var result = await _sut.ForgotPasswordAsync("test@example.com");

        Assert.True(result.IsSuccess);
        await _templatedEmailSender.Received(1).SendSafeAsync(
            "reset-password",
            Arg.Is<ResetPasswordModel>(m => m.ResetUrl.Contains("https://test.example.com/reset-password?token=")),
            "test@example.com",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForgotPassword_NonExistentUser_ReturnsSuccessWithoutEmail()
    {
        _userManager.FindByEmailAsync("unknown@example.com").Returns((ApplicationUser?)null);

        var result = await _sut.ForgotPasswordAsync("unknown@example.com");

        Assert.True(result.IsSuccess);
        await _templatedEmailSender.DidNotReceiveWithAnyArgs().SendSafeAsync<object>(
            default!, default!, default!, default);
    }

    [Fact]
    public async Task ForgotPassword_ResetUrlDoesNotContainEmail()
    {
        var user = CreateTestUser();
        _userManager.FindByEmailAsync("test@example.com").Returns(user);
        _userManager.GeneratePasswordResetTokenAsync(user).Returns("reset-token");

        await _sut.ForgotPasswordAsync("test@example.com");

        await _templatedEmailSender.Received(1).SendSafeAsync(
            "reset-password",
            Arg.Is<ResetPasswordModel>(m =>
                m.ResetUrl.Contains("https://test.example.com/reset-password?token=") &&
                !m.ResetUrl.Contains("email=")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForgotPassword_StoresEmailTokenInDatabase()
    {
        var user = CreateTestUser();
        _userManager.FindByEmailAsync("test@example.com").Returns(user);
        _userManager.GeneratePasswordResetTokenAsync(user).Returns("reset-token");

        await _sut.ForgotPasswordAsync("test@example.com");

        var storedToken = Assert.Single(_dbContext.EmailTokens);
        Assert.Equal(user.Id, storedToken.UserId);
        Assert.Equal("reset-token", storedToken.IdentityToken);
        Assert.Equal(EmailTokenPurpose.PasswordReset, storedToken.Purpose);
        Assert.False(storedToken.IsUsed);
    }

    #endregion

    #region ResetPassword

    private async Task<string> SeedEmailTokenAsync(Guid userId, string identityToken, EmailTokenPurpose purpose, bool isUsed = false, int expiresInHours = 24)
    {
        var rawToken = "opaque-test-token-" + Guid.NewGuid().ToString("N");
        _dbContext.EmailTokens.Add(new EmailToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(rawToken),
            IdentityToken = identityToken,
            Purpose = purpose,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddHours(expiresInHours),
            IsUsed = isUsed,
            UserId = userId
        });
        await _dbContext.SaveChangesAsync();
        return rawToken;
    }

    [Fact]
    public async Task ResetPassword_ValidInput_ReturnsSuccess()
    {
        var user = CreateTestUser();
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-reset-token", EmailTokenPurpose.PasswordReset);
        _userManager.ResetPasswordAsync(user, "identity-reset-token", "NewPass1!")
            .Returns(IdentityResult.Success);

        var result = await _sut.ResetPasswordAsync(
            new ResetPasswordInput(rawToken, "NewPass1!"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ResetPassword_ValidInput_MarksEmailTokenAsUsed()
    {
        var user = CreateTestUser();
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-reset-token", EmailTokenPurpose.PasswordReset);
        _userManager.ResetPasswordAsync(user, "identity-reset-token", "NewPass1!")
            .Returns(IdentityResult.Success);

        await _sut.ResetPasswordAsync(new ResetPasswordInput(rawToken, "NewPass1!"));

        var emailToken = Assert.Single(_dbContext.EmailTokens);
        Assert.True(emailToken.IsUsed);
    }

    [Fact]
    public async Task ResetPassword_ValidInput_RevokesAllRefreshTokens()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(userId, "identity-reset-token", EmailTokenPurpose.PasswordReset);
        _userManager.ResetPasswordAsync(user, "identity-reset-token", "NewPass1!")
            .Returns(IdentityResult.Success);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256("existing-token"),
            UserId = userId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        await _sut.ResetPasswordAsync(new ResetPasswordInput(rawToken, "NewPass1!"));

        var token = Assert.Single(_dbContext.RefreshTokens);
        Assert.True(token.IsInvalidated);
    }

    [Fact]
    public async Task ResetPassword_InvalidOpaqueToken_ReturnsFailure()
    {
        var result = await _sut.ResetPasswordAsync(
            new ResetPasswordInput("nonexistent-opaque-token", "NewPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.ResetPasswordFailed, result.Error);
    }

    [Fact]
    public async Task ResetPassword_UsedToken_ReturnsFailure()
    {
        var user = CreateTestUser();
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-token", EmailTokenPurpose.PasswordReset, isUsed: true);

        var result = await _sut.ResetPasswordAsync(
            new ResetPasswordInput(rawToken, "NewPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.ResetPasswordFailed, result.Error);
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_ReturnsFailure()
    {
        var user = CreateTestUser();
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-token", EmailTokenPurpose.PasswordReset, expiresInHours: -1);

        var result = await _sut.ResetPasswordAsync(
            new ResetPasswordInput(rawToken, "NewPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.ResetPasswordFailed, result.Error);
    }

    [Fact]
    public async Task ResetPassword_WrongPurpose_ReturnsFailure()
    {
        var user = CreateTestUser();
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-token", EmailTokenPurpose.EmailVerification);

        var result = await _sut.ResetPasswordAsync(
            new ResetPasswordInput(rawToken, "NewPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.ResetPasswordFailed, result.Error);
    }

    [Fact]
    public async Task ResetPassword_InvalidIdentityToken_ReturnsTokenInvalidError()
    {
        var user = CreateTestUser();
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(user.Id, "bad-identity-token", EmailTokenPurpose.PasswordReset);
        _userManager.ResetPasswordAsync(user, "bad-identity-token", "NewPass1!")
            .Returns(IdentityResult.Failed(new IdentityError { Code = "InvalidToken", Description = "Invalid token." }));

        var result = await _sut.ResetPasswordAsync(
            new ResetPasswordInput(rawToken, "NewPass1!"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.ResetPasswordTokenInvalid, result.Error);
    }

    [Fact]
    public async Task ResetPassword_PasswordPolicyFailure_ReturnsGenericPolicyError()
    {
        var user = CreateTestUser();
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-token", EmailTokenPurpose.PasswordReset);
        _userManager.ResetPasswordAsync(user, "identity-token", "weak")
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = "Password too short."
            }));

        var result = await _sut.ResetPasswordAsync(
            new ResetPasswordInput(rawToken, "weak"));

        Assert.True(result.IsFailure);
        // Static error - Identity descriptions must never reach the client
        Assert.Equal(ErrorMessages.Auth.PasswordPolicyViolation, result.Error);
        Assert.DoesNotContain("Password too short", result.Error?.Message);
    }

    [Fact]
    public async Task ResetPassword_DoesNotVerifyCandidatePasswordAgainstCurrentOne()
    {
        // A reset-token holder must not be able to use the endpoint as a password oracle:
        // the service never calls CheckPasswordAsync, and resetting to the current password succeeds.
        var user = CreateTestUser();
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-reset-token", EmailTokenPurpose.PasswordReset);
        _userManager.ResetPasswordAsync(user, "identity-reset-token", "CurrentPass1!")
            .Returns(IdentityResult.Success);

        var result = await _sut.ResetPasswordAsync(new ResetPasswordInput(rawToken, "CurrentPass1!"));

        Assert.True(result.IsSuccess);
        await _userManager.DidNotReceive().CheckPasswordAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>());
    }

    #endregion

    #region VerifyEmail

    [Fact]
    public async Task VerifyEmail_ValidInput_ReturnsSuccess()
    {
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-confirm-token", EmailTokenPurpose.EmailVerification);
        _userManager.ConfirmEmailAsync(user, "identity-confirm-token").Returns(IdentityResult.Success);

        var result = await _sut.VerifyEmailAsync(new VerifyEmailInput(rawToken));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task VerifyEmail_ValidInput_MarksEmailTokenAsUsed()
    {
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-confirm-token", EmailTokenPurpose.EmailVerification);
        _userManager.ConfirmEmailAsync(user, "identity-confirm-token").Returns(IdentityResult.Success);

        await _sut.VerifyEmailAsync(new VerifyEmailInput(rawToken));

        var emailToken = Assert.Single(_dbContext.EmailTokens);
        Assert.True(emailToken.IsUsed);
    }

    [Fact]
    public async Task VerifyEmail_InvalidOpaqueToken_ReturnsFailure()
    {
        var result = await _sut.VerifyEmailAsync(
            new VerifyEmailInput("nonexistent-opaque-token"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.EmailVerificationFailed, result.Error);
    }

    [Fact]
    public async Task VerifyEmail_AlreadyVerified_ReturnsFailure()
    {
        var user = CreateTestUser();
        user.EmailConfirmed = true;
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-confirm-token", EmailTokenPurpose.EmailVerification);

        var result = await _sut.VerifyEmailAsync(new VerifyEmailInput(rawToken));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.EmailAlreadyVerified, result.Error);
    }

    [Fact]
    public async Task VerifyEmail_UsedToken_ReturnsFailure()
    {
        var user = CreateTestUser();
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-token", EmailTokenPurpose.EmailVerification, isUsed: true);

        var result = await _sut.VerifyEmailAsync(new VerifyEmailInput(rawToken));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.EmailVerificationFailed, result.Error);
    }

    [Fact]
    public async Task VerifyEmail_ExpiredToken_ReturnsFailure()
    {
        var user = CreateTestUser();
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-token", EmailTokenPurpose.EmailVerification, expiresInHours: -1);

        var result = await _sut.VerifyEmailAsync(new VerifyEmailInput(rawToken));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.EmailVerificationFailed, result.Error);
    }

    [Fact]
    public async Task VerifyEmail_WrongPurpose_ReturnsFailure()
    {
        var user = CreateTestUser();
        var rawToken = await SeedEmailTokenAsync(user.Id, "identity-token", EmailTokenPurpose.PasswordReset);

        var result = await _sut.VerifyEmailAsync(new VerifyEmailInput(rawToken));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.EmailVerificationFailed, result.Error);
    }

    [Fact]
    public async Task VerifyEmail_InvalidIdentityToken_ReturnsFailure()
    {
        var user = CreateTestUser();
        user.EmailConfirmed = false;
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        var rawToken = await SeedEmailTokenAsync(user.Id, "bad-identity-token", EmailTokenPurpose.EmailVerification);
        _userManager.ConfirmEmailAsync(user, "bad-identity-token")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }));

        var result = await _sut.VerifyEmailAsync(new VerifyEmailInput(rawToken));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.EmailVerificationFailed, result.Error);
    }

    #endregion

    #region ResendVerification

    [Fact]
    public async Task ResendVerification_UnverifiedUser_RendersTemplateAndSendsEmail()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.Email = "test@example.com";
        user.EmailConfirmed = false;
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);
        _userManager.GenerateEmailConfirmationTokenAsync(user).Returns("confirm-token");

        var result = await _sut.ResendVerificationEmailAsync();

        Assert.True(result.IsSuccess);
        await _templatedEmailSender.Received(1).SendSafeAsync(
            "verify-email",
            Arg.Is<VerifyEmailModel>(m => m.VerifyUrl.Contains("https://test.example.com/verify-email?token=")),
            "test@example.com",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResendVerification_AlreadyVerified_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        user.EmailConfirmed = true;
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);

        var result = await _sut.ResendVerificationEmailAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.EmailAlreadyVerified, result.Error);
    }

    [Fact]
    public async Task ResendVerification_NotAuthenticated_ReturnsUnauthorized()
    {
        _userContext.UserId.Returns((Guid?)null);

        var result = await _sut.ResendVerificationEmailAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.NotAuthenticated, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task ResendVerification_UserNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _userContext.UserId.Returns(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.ResendVerificationEmailAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.UserNotFound, result.Error);
    }

    #endregion

    #region Login_TwoFactor

    [Fact]
    public async Task Login_TwoFactorEnabled_ReturnsChallengeToken()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        SetupSuccessfulLogin(user);

        var result = await _sut.Login("test@example.com", "password123");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.RequiresTwoFactor);
        Assert.NotNull(result.Value.ChallengeToken);
        Assert.Null(result.Value.Tokens);
    }

    [Fact]
    public async Task Login_TwoFactorEnabled_StoresChallengeInDatabase()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        SetupSuccessfulLogin(user);

        var result = await _sut.Login("test@example.com", "password123");

        var challenge = Assert.Single(_dbContext.TwoFactorChallenges);
        Assert.Equal(user.Id, challenge.UserId);
        Assert.False(challenge.IsUsed);
        Assert.Equal(0, challenge.FailedAttempts);
        Assert.Equal(HashHelper.Sha256(result.Value.ChallengeToken!), challenge.Token);
    }

    [Fact]
    public async Task Login_TwoFactorEnabled_ChallengeExpiresAfterConfiguredLifetime()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123");

        var challenge = Assert.Single(_dbContext.TwoFactorChallenges);
        var expectedExpiry = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(5);
        Assert.Equal(expectedExpiry, challenge.ExpiresAt);
    }

    [Fact]
    public async Task Login_TwoFactorEnabled_RememberMeCarriedForward()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123", rememberMe: true);

        var challenge = Assert.Single(_dbContext.TwoFactorChallenges);
        Assert.True(challenge.IsRememberMe);
    }

    [Fact]
    public async Task Login_TwoFactorEnabled_DoesNotLogLoginSuccess()
    {
        var user = CreateTestUser();
        user.TwoFactorEnabled = true;
        SetupSuccessfulLogin(user);

        await _sut.Login("test@example.com", "password123");

        await _auditService.DidNotReceive().LogAsync(
            AuditActions.LoginSuccess,
            userId: Arg.Any<Guid?>(),
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    #endregion

    #region CompleteTwoFactorLogin

    [Fact]
    public async Task CompleteTwoFactorLogin_ValidCode_ReturnsTokens()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456")
            .Returns(true);

        var result = await _sut.CompleteTwoFactorLoginAsync(challengeToken, "123456", false);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
        Assert.Equal("refresh-token", result.Value.RefreshToken);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_ValidCode_MarksChallengeAsUsed()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456")
            .Returns(true);

        await _sut.CompleteTwoFactorLoginAsync(challengeToken, "123456", false);

        var challenge = Assert.Single(_dbContext.TwoFactorChallenges);
        Assert.True(challenge.IsUsed);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_ValidCode_LogsAuditSuccess()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456")
            .Returns(true);

        await _sut.CompleteTwoFactorLoginAsync(challengeToken, "123456", false);

        await _auditService.Received(1).LogAsync(
            AuditActions.TwoFactorLoginSuccess,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_InvalidCode_ReturnsUnauthorized()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "000000")
            .Returns(false);

        var result = await _sut.CompleteTwoFactorLoginAsync(challengeToken, "000000", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.InvalidCode, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_InvalidCode_IncrementsFailedAttempts()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "000000")
            .Returns(false);

        await _sut.CompleteTwoFactorLoginAsync(challengeToken, "000000", false);

        var challenge = Assert.Single(_dbContext.TwoFactorChallenges);
        Assert.Equal(1, challenge.FailedAttempts);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_ChallengeNotFound_ReturnsUnauthorized()
    {
        var result = await _sut.CompleteTwoFactorLoginAsync("invalid-token", "123456", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.ChallengeNotFound, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_ExpiredChallenge_ReturnsUnauthorized()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id, isExpired: true);

        var result = await _sut.CompleteTwoFactorLoginAsync(challengeToken, "123456", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.ChallengeNotFound, result.Error);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_UsedChallenge_ReturnsUnauthorized()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id, isUsed: true);

        var result = await _sut.CompleteTwoFactorLoginAsync(challengeToken, "123456", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.ChallengeNotFound, result.Error);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_LockedChallenge_ReturnsChallengeLocked()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id, failedAttempts: 5);

        var result = await _sut.CompleteTwoFactorLoginAsync(challengeToken, "123456", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.ChallengeLocked, result.Error);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_UserNotFound_ReturnsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var challengeToken = await SeedTwoFactorChallengeAsync(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.CompleteTwoFactorLoginAsync(challengeToken, "123456", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.UserNotFound, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_InvalidCode_LogsAuditFailure()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "000000")
            .Returns(false);

        await _sut.CompleteTwoFactorLoginAsync(challengeToken, "000000", false);

        await _auditService.Received(1).LogAsync(
            AuditActions.TwoFactorLoginFailure,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteTwoFactorLogin_RememberMe_UsesCorrectLifetime()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id, isRememberMe: true);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, "123456")
            .Returns(true);

        await _sut.CompleteTwoFactorLoginAsync(challengeToken, "123456", false);

        var refreshToken = _dbContext.RefreshTokens.Single();
        Assert.True(refreshToken.IsPersistent);
        var expectedExpiry = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7);
        Assert.Equal(expectedExpiry, refreshToken.ExpiredAt);
    }

    #endregion

    #region CompleteTwoFactorRecoveryLogin

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_ValidCode_ReturnsTokens()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.RedeemTwoFactorRecoveryCodeAsync(user, "RECOVERY-01")
            .Returns(IdentityResult.Success);

        var result = await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "RECOVERY-01", false);

        Assert.True(result.IsSuccess);
        Assert.Equal("access-token", result.Value.AccessToken);
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_ValidCode_LogsBothAuditEvents()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.RedeemTwoFactorRecoveryCodeAsync(user, "RECOVERY-01")
            .Returns(IdentityResult.Success);

        await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "RECOVERY-01", false);

        await _auditService.Received(1).LogAsync(
            AuditActions.TwoFactorRecoveryCodeUsed,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogAsync(
            AuditActions.TwoFactorLoginSuccess,
            userId: user.Id,
            targetEntityType: Arg.Any<string?>(),
            targetEntityId: Arg.Any<Guid?>(),
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_InvalidCode_ReturnsUnauthorized()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.RedeemTwoFactorRecoveryCodeAsync(user, "INVALID")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Invalid" }));

        var result = await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "INVALID", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.RecoveryCodeInvalid, result.Error);
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_InvalidCode_IncrementsFailedAttempts()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.RedeemTwoFactorRecoveryCodeAsync(user, "INVALID")
            .Returns(IdentityResult.Failed(new IdentityError { Description = "Invalid" }));

        await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "INVALID", false);

        var challenge = Assert.Single(_dbContext.TwoFactorChallenges);
        Assert.Equal(1, challenge.FailedAttempts);
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_ChallengeNotFound_ReturnsUnauthorized()
    {
        var result = await _sut.CompleteTwoFactorRecoveryLoginAsync("invalid-token", "RECOVERY-01", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.ChallengeNotFound, result.Error);
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_ValidCode_MarksChallengeAsUsed()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.RedeemTwoFactorRecoveryCodeAsync(user, "RECOVERY-01")
            .Returns(IdentityResult.Success);

        await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "RECOVERY-01", false);

        var challenge = Assert.Single(_dbContext.TwoFactorChallenges);
        Assert.True(challenge.IsUsed);
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_RememberMe_UsesCorrectLifetime()
    {
        var user = CreateTestUser();
        SetupSuccessfulLogin(user);
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id, isRememberMe: true);
        _userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        _userManager.RedeemTwoFactorRecoveryCodeAsync(user, "RECOVERY-01")
            .Returns(IdentityResult.Success);

        await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "RECOVERY-01", false);

        var refreshToken = _dbContext.RefreshTokens.Single();
        Assert.True(refreshToken.IsPersistent);
        var expectedExpiry = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7);
        Assert.Equal(expectedExpiry, refreshToken.ExpiredAt);
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_ExpiredChallenge_ReturnsUnauthorized()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id, isExpired: true);

        var result = await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "RECOVERY-01", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.ChallengeNotFound, result.Error);
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_UsedChallenge_ReturnsUnauthorized()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id, isUsed: true);

        var result = await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "RECOVERY-01", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.ChallengeNotFound, result.Error);
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_LockedChallenge_ReturnsChallengeLocked()
    {
        var user = CreateTestUser();
        var challengeToken = await SeedTwoFactorChallengeAsync(user.Id, failedAttempts: 5);

        var result = await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "RECOVERY-01", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.TwoFactor.ChallengeLocked, result.Error);
    }

    [Fact]
    public async Task CompleteTwoFactorRecoveryLogin_UserNotFound_ReturnsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var challengeToken = await SeedTwoFactorChallengeAsync(userId);
        _userManager.FindByIdAsync(userId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.CompleteTwoFactorRecoveryLoginAsync(challengeToken, "RECOVERY-01", false);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.UserNotFound, result.Error);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    #endregion

    #region Register_SendsVerificationEmail

    [Fact]
    public async Task Register_Success_SendsVerificationEmail()
    {
        var input = new RegisterInput("test@example.com", "Password1!", null, null, null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), "Password1!")
            .Returns(callInfo =>
            {
                callInfo.Arg<ApplicationUser>().Id = Guid.NewGuid();
                return IdentityResult.Success;
            });
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), "User")
            .Returns(IdentityResult.Success);
        _userManager.GenerateEmailConfirmationTokenAsync(Arg.Any<ApplicationUser>())
            .Returns("confirm-token");

        await _sut.Register(input);

        await _templatedEmailSender.Received(1).SendSafeAsync(
            "verify-email",
            Arg.Any<VerifyEmailModel>(),
            "test@example.com",
            Arg.Any<CancellationToken>());
    }

    #endregion
}
