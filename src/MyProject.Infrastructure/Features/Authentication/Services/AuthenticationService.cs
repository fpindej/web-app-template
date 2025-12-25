using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
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

namespace MyProject.Infrastructure.Features.Authentication.Services;

internal class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ITokenProvider tokenProvider,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor,
    IOptions<JwtOptions> jwtOptions,
    MyProjectDbContext dbContext) : IAuthenticationService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<Result> Login(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user is null)
        {
            return Result.Failure("Invalid username or password.");
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            return Result.Failure("Invalid username or password.");
        }

        var accessToken = await tokenProvider.GenerateAccessToken(user);
        var refreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenString,
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = utcNow.UtcDateTime.AddDays(_jwtOptions.RefreshToken.ExpiresInDays),
            Used = false,
            Invalidated = false
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        SetCookie(
            cookieName: CookieNames.AccessToken,
            content: accessToken,
            options: CreateCookieOptions(expiresAt: utcNow.AddMinutes(_jwtOptions.ExpiresInMinutes)));

        SetCookie(
            cookieName: CookieNames.RefreshToken,
            content: refreshTokenString,
            options: CreateCookieOptions(expiresAt: utcNow.AddDays(_jwtOptions.RefreshToken.ExpiresInDays)));

        return Result.Success();
    }

    public async Task<Result<Guid>> Register(RegisterInput input)
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

    public async Task Logout()
    {
        // Get user ID before clearing cookies
        var userId = TryGetUserIdFromAccessToken();

        DeleteCookie(CookieNames.AccessToken);
        DeleteCookie(CookieNames.RefreshToken);

        if (userId.HasValue)
        {
            await RevokeUserTokens(userId.Value);
        }
    }

    public async Task<Result> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Result.Failure("Refresh token is missing.");
        }

        var storedToken = await dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken is null)
        {
            DeleteCookie(CookieNames.AccessToken);
            DeleteCookie(CookieNames.RefreshToken);
            return Result.Failure("Invalid refresh token.");
        }

        if (storedToken.Invalidated)
        {
            DeleteCookie(CookieNames.AccessToken);
            DeleteCookie(CookieNames.RefreshToken);
            return Result.Failure("Invalid refresh token.");
        }

        if (storedToken.Used)
        {
            // Security alert: Token reuse! Revoke all tokens for this user.
            storedToken.Invalidated = true;
            await RevokeUserTokens(storedToken.UserId);
            DeleteCookie(CookieNames.AccessToken);
            DeleteCookie(CookieNames.RefreshToken);
            return Result.Failure("Invalid refresh token.");
        }

        if (storedToken.ExpiredAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            storedToken.Invalidated = true;
            await dbContext.SaveChangesAsync(cancellationToken);
            DeleteCookie(CookieNames.AccessToken);
            DeleteCookie(CookieNames.RefreshToken);
            return Result.Failure("Refresh token has expired.");
        }

        // Mark current token as used
        storedToken.Used = true;

        var user = storedToken.User;
        if (user is null)
        {
            return Result.Failure("User not found.");
        }

        var newAccessToken = await tokenProvider.GenerateAccessToken(user);
        var newRefreshTokenString = tokenProvider.GenerateRefreshToken();
        var utcNow = timeProvider.GetUtcNow();

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshTokenString,
            UserId = user.Id,
            CreatedAt = utcNow.UtcDateTime,
            ExpiredAt = utcNow.UtcDateTime.AddDays(_jwtOptions.RefreshToken.ExpiresInDays),
            Used = false,
            Invalidated = false
        };

        dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        SetCookie(
            cookieName: CookieNames.AccessToken,
            content: newAccessToken,
            options: CreateCookieOptions(expiresAt: utcNow.AddMinutes(_jwtOptions.ExpiresInMinutes)));

        SetCookie(
            cookieName: CookieNames.RefreshToken,
            content: newRefreshTokenString,
            options: CreateCookieOptions(expiresAt: utcNow.AddDays(_jwtOptions.RefreshToken.ExpiresInDays)));

        return Result.Success();
    }

    private Guid? TryGetUserIdFromAccessToken()
    {
        if (httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue(
                key: CookieNames.AccessToken,
                value: out var accessToken) is not true)
        {
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return null;
            }

            var jwtToken = handler.ReadJwtToken(accessToken);

            var userIdString = jwtToken.Claims.FirstOrDefault(c => c.Type is JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(userIdString, out var userId) ? userId : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task RevokeUserTokens(Guid userId)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.Invalidated)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.Invalidated = true;
        }

        await dbContext.SaveChangesAsync();

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            await userManager.UpdateSecurityStampAsync(user);
        }
    }

    public async Task<Result<UserOutput>> GetCurrentUserAsync()
    {
        var userId = TryGetUserIdFromAccessToken();

        if (!userId.HasValue)
        {
            return Result<UserOutput>.Failure("User is not authenticated.");
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result<UserOutput>.Failure("User not found.");
        }

        return Result<UserOutput>.Success(new UserOutput(
            Id: user.Id,
            UserName: user.UserName!));
    }

    public async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return new List<string>();
        }
        return await userManager.GetRolesAsync(user);
    }

    /// <summary>
    /// Sets the authentication cookie in the HTTP response
    /// </summary>
    /// <param name="cookieName">Name of the cookie</param>
    /// <param name="content">Content to be stored in the cookie</param>
    /// <param name="options">Cookie options</param>
    private void SetCookie(string cookieName, string content, CookieOptions options)
        => httpContextAccessor.HttpContext?.Response.Cookies.Append(key: cookieName, value: content, options: options);

    /// <summary>
    /// Deletes a specific cookie from the HTTP response
    /// </summary>
    /// <param name="cookieName">Name of the cookie to delete</param>
    private void DeleteCookie(string cookieName)
        => httpContextAccessor.HttpContext?.Response.Cookies.Delete(
            cookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None
            });

    private static CookieOptions CreateCookieOptions(DateTimeOffset expiresAt)
        => new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = expiresAt
        };
}
