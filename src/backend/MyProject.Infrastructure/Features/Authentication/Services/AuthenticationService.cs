using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyProject.Domain;
using MyProject.Infrastructure.Features.Authentication.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Cookies;
using MyProject.Application.Identity;

namespace MyProject.Infrastructure.Features.Authentication.Services;

internal class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenProvider tokenProvider,
    TimeProvider timeProvider,
    ICookieService cookieService,
    IUserContext userContext,
    IOptions<JwtOptions> jwtOptions,
    MyProjectDbContext dbContext) : IAuthenticationService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result<AuthenticationOutput>> Login(string username, string password, bool useCookies = false, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user is null)
        {
            return Result<AuthenticationOutput>.Failure("Invalid username or password.");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return Result<AuthenticationOutput>.Failure("Invalid username or password.");
        }

        var accessToken = await tokenProvider.GenerateAccessToken(user);
        var refreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashToken(refreshTokenString),
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = utcNow.UtcDateTime.AddDays(_jwtOptions.RefreshToken.ExpiresInDays),
            Used = false,
            Invalidated = false
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (useCookies)
        {
            cookieService.SetSecureCookie(
                key: CookieNames.AccessToken,
                value: accessToken,
                expires: utcNow.AddMinutes(_jwtOptions.ExpiresInMinutes));

            cookieService.SetSecureCookie(
                key: CookieNames.RefreshToken,
                value: refreshTokenString,
                expires: utcNow.AddDays(_jwtOptions.RefreshToken.ExpiresInDays));
        }

        var output = new AuthenticationOutput(
            AccessToken: accessToken,
            RefreshToken: refreshTokenString
        );

        return Result<AuthenticationOutput>.Success(output);
    }

    public async Task<Result<Guid>> Register(RegisterInput input, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            FirstName = input.FirstName,
            LastName = input.LastName,
            PhoneNumber = input.PhoneNumber
        };

        var result = await userManager.CreateAsync(user, input.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(errors);
        }

        var roleResult = await userManager.AddToRoleAsync(user, "User");

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(errors);
        }

        return Result<Guid>.Success(user.Id);
    }

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

    public async Task<Result<AuthenticationOutput>> RefreshTokenAsync(string refreshToken, bool useCookies = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result<AuthenticationOutput>.Failure("Refresh token is missing.");
        }

        var hashedToken = HashToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == hashedToken, cancellationToken);

        if (storedToken is null)
        {
            return Fail("Refresh token not found.");
        }

        if (storedToken.Invalidated)
        {
            return Fail("Refresh token has been invalidated.");
        }

        if (storedToken.Used)
        {
            // Security alert: Token reuse! Revoke all tokens for this user.
            storedToken.Invalidated = true;
            await RevokeUserTokens(storedToken.UserId, cancellationToken);
            return Fail("Invalid refresh token.");
        }

        if (storedToken.ExpiredAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            storedToken.Invalidated = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Fail("Refresh token has expired.");
        }

        // Mark current token as used
        storedToken.Used = true;

        var user = storedToken.User;
        if (user is null)
        {
            return Result<AuthenticationOutput>.Failure("User not found.");
        }

        var newAccessToken = await tokenProvider.GenerateAccessToken(user);
        var newRefreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = HashToken(newRefreshTokenString),
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = utcNow.UtcDateTime.AddDays(_jwtOptions.RefreshToken.ExpiresInDays),
            Used = false,
            Invalidated = false
        };

        dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (useCookies)
        {
            cookieService.SetSecureCookie(
                key: CookieNames.AccessToken,
                value: newAccessToken,
                expires: utcNow.AddMinutes(_jwtOptions.ExpiresInMinutes));

            cookieService.SetSecureCookie(
                key: CookieNames.RefreshToken,
                value: newRefreshTokenString,
                expires: utcNow.AddDays(_jwtOptions.RefreshToken.ExpiresInDays));
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
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }
}

