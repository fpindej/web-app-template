using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyProject.Domain;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Cookies;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IAuthenticationService"/> with JWT token rotation.
/// </summary>
internal class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenProvider tokenProvider,
    TimeProvider timeProvider,
    ICookieService cookieService,
    IUserContext userContext,
    ICacheService cacheService,
    IOptions<AuthenticationOptions> authenticationOptions,
    MyProjectDbContext dbContext) : IAuthenticationService
{
    private readonly AuthenticationOptions.JwtOptions _jwtOptions = authenticationOptions.Value.Jwt;

    /// <inheritdoc />
    public async Task<Result<AuthenticationOutput>> Login(string username, string password, bool useCookies = false, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user is null)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.LoginInvalidCredentials);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.LoginAccountLocked);
        }

        if (!signInResult.Succeeded)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.LoginInvalidCredentials);
        }

        var accessToken = await tokenProvider.GenerateAccessToken(user);
        var refreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(refreshTokenString),
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = utcNow.UtcDateTime.AddDays(_jwtOptions.RefreshToken.ExpiresInDays),
            Used = false,
            Invalidated = false,
            Persistent = rememberMe
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (useCookies)
        {
            SetAuthCookies(accessToken, refreshTokenString, rememberMe, utcNow,
                utcNow.AddDays(_jwtOptions.RefreshToken.ExpiresInDays));
        }

        var output = new AuthenticationOutput(
            AccessToken: accessToken,
            RefreshToken: refreshTokenString
        );

        return Result<AuthenticationOutput>.Success(output);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> Register(RegisterInput input, CancellationToken cancellationToken = default)
    {
        var normalizedPhone = PhoneNumberHelper.Normalize(input.PhoneNumber);

        if (normalizedPhone is not null && await IsPhoneNumberTakenAsync(normalizedPhone, excludeUserId: null))
        {
            return Result<Guid>.Failure(ErrorMessages.User.PhoneNumberTaken);
        }

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            FirstName = input.FirstName,
            LastName = input.LastName,
            PhoneNumber = normalizedPhone
        };

        var result = await userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.User);

        if (!roleResult.Succeeded)
        {
            return Result<Guid>.Failure(ErrorMessages.Auth.RegisterRoleAssignFailed);
        }

        return Result<Guid>.Success(user.Id);
    }

    /// <inheritdoc />
    public async Task Logout(CancellationToken cancellationToken = default)
    {
        // Get user ID before clearing cookies
        var userId = userContext.UserId;

        cookieService.DeleteCookie(CookieNames.AccessToken);
        cookieService.DeleteCookie(CookieNames.RefreshToken);

        if (userId.HasValue)
        {
            await RevokeUserTokens(userId.Value);
        }
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationOutput>> RefreshTokenAsync(string refreshToken, bool useCookies = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.TokenMissing);
        }

        var hashedToken = HashHelper.Sha256(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken, cancellationToken);

        if (storedToken is null)
        {
            return Fail(ErrorMessages.Auth.TokenNotFound);
        }

        if (storedToken.Invalidated)
        {
            return Fail(ErrorMessages.Auth.TokenInvalidated);
        }

        if (storedToken.Used)
        {
            // Security alert: Token reuse! Revoke all tokens for this user.
            storedToken.Invalidated = true;
            await RevokeUserTokens(storedToken.UserId, cancellationToken);
            return Fail(ErrorMessages.Auth.TokenReused);
        }

        if (storedToken.ExpiredAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            storedToken.Invalidated = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Fail(ErrorMessages.Auth.TokenExpired);
        }

        // Mark current token as used
        storedToken.Used = true;

        var user = storedToken.User;
        if (user is null)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.TokenUserNotFound);
        }

        var newAccessToken = await tokenProvider.GenerateAccessToken(user);
        var newRefreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashHelper.Sha256(newRefreshTokenString),
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = storedToken.ExpiredAt,
            Used = false,
            Invalidated = false,
            Persistent = storedToken.Persistent
        };

        dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (useCookies)
        {
            SetAuthCookies(newAccessToken, newRefreshTokenString, storedToken.Persistent, utcNow,
                new DateTimeOffset(storedToken.ExpiredAt, TimeSpan.Zero));
        }

        var output = new AuthenticationOutput(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenString
        );

        return Result<AuthenticationOutput>.Success(output);

        Result<AuthenticationOutput> Fail(string message)
        {
            if (useCookies)
            {
                cookieService.DeleteCookie(CookieNames.AccessToken);
                cookieService.DeleteCookie(CookieNames.RefreshToken);
            }
            return Result<AuthenticationOutput>.Failure(message);
        }
    }

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(ChangePasswordInput input, CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result.Failure(ErrorMessages.Auth.NotAuthenticated);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.UserNotFound);
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, input.CurrentPassword);

        if (!passwordValid)
        {
            return Result.Failure(ErrorMessages.Auth.PasswordIncorrect);
        }

        var changeResult = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);

        if (!changeResult.Succeeded)
        {
            var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
            return Result.Failure(errors);
        }

        await RevokeUserTokens(userId.Value, cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Sets access and refresh token cookies. When <paramref name="persistent"/> is true,
    /// cookies receive explicit expiry dates so they survive browser restarts.
    /// When false, session cookies are used (no <c>Expires</c> header).
    /// </summary>
    private void SetAuthCookies(string accessToken, string refreshToken, bool persistent,
        DateTimeOffset utcNow, DateTimeOffset refreshTokenExpiry)
    {
        cookieService.SetSecureCookie(
            key: CookieNames.AccessToken,
            value: accessToken,
            expires: persistent ? utcNow.AddMinutes(_jwtOptions.ExpiresInMinutes) : null);

        cookieService.SetSecureCookie(
            key: CookieNames.RefreshToken,
            value: refreshToken,
            expires: persistent ? refreshTokenExpiry : null);
    }

    private async Task RevokeUserTokens(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.Invalidated)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Invalidated = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            await userManager.UpdateSecurityStampAsync(user);
            await cacheService.RemoveAsync(CacheKeys.SecurityStamp(userId), cancellationToken);
        }
    }

    /// <summary>
    /// Checks whether any existing user already has the given normalized phone number.
    /// </summary>
    private async Task<bool> IsPhoneNumberTakenAsync(string normalizedPhone, Guid? excludeUserId)
    {
        return await userManager.Users
            .AnyAsync(u =>
                u.PhoneNumber != null
                && u.PhoneNumber == normalizedPhone
                && (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
    }
}
