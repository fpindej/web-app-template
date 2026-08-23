using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Features.Authentication.Dtos;
using MyProject.Application.Features.Email;
using MyProject.Application.Features.Email.Models;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IAuthenticationService"/> with JWT token rotation.
/// </summary>
internal class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    TimeProvider timeProvider,
    IUserContext userContext,
    ITemplatedEmailSender templatedEmailSender,
    EmailTokenService emailTokenService,
    IAuditService auditService,
    ITokenSessionService tokenSessionService,
    IOptions<AuthenticationOptions> authenticationOptions,
    IOptions<EmailOptions> emailOptions,
    ILogger<AuthenticationService> logger,
    MyProjectDbContext dbContext) : IAuthenticationService
{
    private readonly AuthenticationOptions.TwoFactorOptions _twoFactorOptions = authenticationOptions.Value.TwoFactor;
    private readonly AuthenticationOptions.EmailTokenOptions _emailTokenOptions = authenticationOptions.Value.EmailToken;
    private readonly EmailOptions _emailOptions = emailOptions.Value;

    /// <summary>
    /// Hash of a random throwaway password, computed lazily on first use with the
    /// DI-configured password hasher so that a customized work factor is reflected.
    /// Verifying candidate passwords against it always fails but costs the same as
    /// a real password check.
    /// </summary>
    private static string? _dummyPasswordHash;

    /// <inheritdoc />
    public async Task<Result<LoginOutput>> Login(string username, string password, bool useCookies = false, bool rememberMe = false, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user is null)
        {
            // Burn a password verification so response timing does not reveal whether the account exists
            BurnDummyPasswordVerification(password);

            await auditService.LogAsync(AuditActions.LoginFailure,
                metadata: JsonSerializer.Serialize(new { attemptedEmail = PiiMasker.MaskEmail(username) }),
                ct: cancellationToken);
            return Result<LoginOutput>.Failure(ErrorMessages.Auth.LoginInvalidCredentials, ErrorType.Unauthorized);
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            // External-only (passwordless) account: CheckPasswordSignInAsync would short-circuit
            // without hashing, so burn a dummy verification to keep timing indistinguishable
            // from a wrong-password attempt, then return the same generic failure.
            BurnDummyPasswordVerification(password);

            await auditService.LogAsync(AuditActions.LoginFailure, userId: user.Id, ct: cancellationToken);
            return Result<LoginOutput>.Failure(ErrorMessages.Auth.LoginInvalidCredentials, ErrorType.Unauthorized);
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            await auditService.LogAsync(AuditActions.LoginFailure, userId: user.Id, ct: cancellationToken);
            return Result<LoginOutput>.Failure(ErrorMessages.Auth.LoginAccountLocked, ErrorType.Unauthorized);
        }

        if (!signInResult.Succeeded)
        {
            await auditService.LogAsync(AuditActions.LoginFailure, userId: user.Id, ct: cancellationToken);
            return Result<LoginOutput>.Failure(ErrorMessages.Auth.LoginInvalidCredentials, ErrorType.Unauthorized);
        }

        if (user.TwoFactorEnabled)
        {
            var challengeToken = GenerateChallengeToken();
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;

            var challenge = new TwoFactorChallenge
            {
                Id = Guid.NewGuid(),
                Token = HashHelper.Sha256(challengeToken),
                UserId = user.Id,
                CreatedAt = utcNow,
                ExpiresAt = utcNow.Add(_twoFactorOptions.ChallengeLifetime),
                IsUsed = false,
                IsRememberMe = rememberMe
            };

            dbContext.TwoFactorChallenges.Add(challenge);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<LoginOutput>.Success(new LoginOutput(
                Tokens: null,
                ChallengeToken: challengeToken,
                RequiresTwoFactor: true));
        }

        var tokens = await tokenSessionService.GenerateTokensAsync(user, useCookies, rememberMe, cancellationToken);
        await auditService.LogAsync(AuditActions.LoginSuccess, userId: user.Id, ct: cancellationToken);

        return Result<LoginOutput>.Success(new LoginOutput(
            Tokens: tokens,
            ChallengeToken: null,
            RequiresTwoFactor: false));
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationOutput>> CompleteTwoFactorLoginAsync(
        string challengeToken, string code, bool useCookies, CancellationToken cancellationToken = default)
    {
        var challengeResult = await ResolveChallengeAsync(challengeToken, cancellationToken);
        if (!challengeResult.IsSuccess)
        {
            return Result<AuthenticationOutput>.Failure(challengeResult.Error ?? ErrorMessages.TwoFactor.ChallengeNotFound, challengeResult.ErrorType ?? ErrorType.Unauthorized);
        }

        var challenge = challengeResult.Value;
        var user = await userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user is null)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.UserNotFound, ErrorType.Unauthorized);
        }

        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user, TokenOptions.DefaultAuthenticatorProvider, code);

        if (!isValid)
        {
            challenge.FailedAttempts++;
            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync(AuditActions.TwoFactorLoginFailure, userId: user.Id, ct: cancellationToken);
            return Result<AuthenticationOutput>.Failure(ErrorMessages.TwoFactor.InvalidCode, ErrorType.Unauthorized);
        }

        challenge.IsUsed = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        var tokens = await tokenSessionService.GenerateTokensAsync(user, useCookies, challenge.IsRememberMe, cancellationToken);

        await auditService.LogAsync(AuditActions.TwoFactorLoginSuccess, userId: user.Id, ct: cancellationToken);

        return Result<AuthenticationOutput>.Success(tokens);
    }

    /// <inheritdoc />
    public async Task<Result<AuthenticationOutput>> CompleteTwoFactorRecoveryLoginAsync(
        string challengeToken, string recoveryCode, bool useCookies, CancellationToken cancellationToken = default)
    {
        var challengeResult = await ResolveChallengeAsync(challengeToken, cancellationToken);
        if (!challengeResult.IsSuccess)
        {
            return Result<AuthenticationOutput>.Failure(challengeResult.Error ?? ErrorMessages.TwoFactor.ChallengeNotFound, challengeResult.ErrorType ?? ErrorType.Unauthorized);
        }

        var challenge = challengeResult.Value;
        var user = await userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user is null)
        {
            return Result<AuthenticationOutput>.Failure(ErrorMessages.Auth.UserNotFound, ErrorType.Unauthorized);
        }

        var redeemResult = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode);
        if (!redeemResult.Succeeded)
        {
            challenge.FailedAttempts++;
            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync(AuditActions.TwoFactorLoginFailure, userId: user.Id, ct: cancellationToken);
            return Result<AuthenticationOutput>.Failure(ErrorMessages.TwoFactor.RecoveryCodeInvalid, ErrorType.Unauthorized);
        }

        challenge.IsUsed = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        var tokens = await tokenSessionService.GenerateTokensAsync(user, useCookies, challenge.IsRememberMe, cancellationToken);

        await auditService.LogAsync(AuditActions.TwoFactorRecoveryCodeUsed, userId: user.Id, ct: cancellationToken);
        await auditService.LogAsync(AuditActions.TwoFactorLoginSuccess, userId: user.Id, ct: cancellationToken);

        return Result<AuthenticationOutput>.Success(tokens);
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
            // Static client message prevents email enumeration; details stay server-side (codes only, no PII)
            logger.LogWarning("Registration rejected by Identity: {ErrorCodes}",
                string.Join(", ", result.Errors.Select(e => e.Code)));
            return Result<Guid>.Failure(ErrorMessages.Auth.RegistrationInvalid);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.User);

        if (!roleResult.Succeeded)
        {
            return Result<Guid>.Failure(ErrorMessages.Auth.RegisterRoleAssignFailed);
        }

        await SendVerificationEmailAsync(user, cancellationToken);

        await auditService.LogAsync(AuditActions.Register, userId: user.Id, ct: cancellationToken);

        return Result<Guid>.Success(user.Id);
    }

    /// <inheritdoc />
    public async Task Logout(CancellationToken cancellationToken = default)
    {
        // Get user ID before clearing cookies
        var userId = userContext.UserId;

        tokenSessionService.DeleteAuthCookies();

        if (userId.HasValue)
        {
            await auditService.LogAsync(AuditActions.Logout, userId: userId.Value, ct: cancellationToken);
            await tokenSessionService.RevokeUserTokensAsync(userId.Value, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task<Result<AuthenticationOutput>> RefreshTokenAsync(
        string refreshToken, bool useCookies = false, CancellationToken cancellationToken = default)
        => tokenSessionService.RefreshTokenAsync(refreshToken, useCookies, cancellationToken);

    /// <inheritdoc />
    public async Task<Result> ChangePasswordAsync(ChangePasswordInput input, CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result.Failure(ErrorMessages.Auth.NotAuthenticated, ErrorType.Unauthorized);
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
            logger.LogWarning("Password change rejected by Identity for user '{UserId}': {ErrorCodes}",
                userId.Value, string.Join(", ", changeResult.Errors.Select(e => e.Code)));
            return Result.Failure(ErrorMessages.Auth.PasswordPolicyViolation);
        }

        await tokenSessionService.RevokeUserTokensAsync(userId.Value, cancellationToken);

        await auditService.LogAsync(AuditActions.PasswordChange, userId: userId.Value, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            // Return success to prevent user enumeration
            logger.LogDebug("Forgot password requested for non-existent email {MaskedEmail}", PiiMasker.MaskEmail(email));
            return Result.Success();
        }

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var opaqueToken = await emailTokenService.CreateAsync(user.Id, identityToken, EmailTokenPurpose.PasswordReset, cancellationToken);
        var resetUrl = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={opaqueToken}";

        var model = new ResetPasswordModel(resetUrl, _emailTokenOptions.Lifetime.ToHumanReadable());
        await templatedEmailSender.SendSafeAsync(EmailTemplateNames.ResetPassword, model, email, cancellationToken);

        await auditService.LogAsync(AuditActions.PasswordResetRequest, userId: user.Id, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(ResetPasswordInput input, CancellationToken cancellationToken = default)
    {
        var emailToken = await emailTokenService.ResolveAsync(input.Token, EmailTokenPurpose.PasswordReset, cancellationToken);

        if (emailToken is null)
        {
            return Result.Failure(ErrorMessages.Auth.ResetPasswordFailed);
        }

        var user = await userManager.FindByIdAsync(emailToken.UserId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.ResetPasswordFailed);
        }

        var resetResult = await userManager.ResetPasswordAsync(user, emailToken.IdentityToken, input.NewPassword);

        if (!resetResult.Succeeded)
        {
            logger.LogWarning("Password reset rejected by Identity for user '{UserId}': {ErrorCodes}",
                user.Id, string.Join(", ", resetResult.Errors.Select(e => e.Code)));

            // Distinguish between invalid token and other Identity errors (e.g., password policy)
            if (resetResult.Errors.Any(e => string.Equals(e.Code, nameof(IdentityErrorDescriber.InvalidToken), StringComparison.Ordinal)))
            {
                return Result.Failure(ErrorMessages.Auth.ResetPasswordTokenInvalid);
            }

            return Result.Failure(ErrorMessages.Auth.PasswordPolicyViolation);
        }

        emailToken.IsUsed = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        await tokenSessionService.RevokeUserTokensAsync(user.Id, cancellationToken);

        await auditService.LogAsync(AuditActions.PasswordReset, userId: user.Id, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> VerifyEmailAsync(VerifyEmailInput input, CancellationToken cancellationToken = default)
    {
        var emailToken = await emailTokenService.ResolveAsync(input.Token, EmailTokenPurpose.EmailVerification, cancellationToken);

        if (emailToken is null)
        {
            return Result.Failure(ErrorMessages.Auth.EmailVerificationFailed);
        }

        var user = await userManager.FindByIdAsync(emailToken.UserId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.EmailVerificationFailed);
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure(ErrorMessages.Auth.EmailAlreadyVerified);
        }

        var confirmResult = await userManager.ConfirmEmailAsync(user, emailToken.IdentityToken);

        if (!confirmResult.Succeeded)
        {
            return Result.Failure(ErrorMessages.Auth.EmailVerificationFailed);
        }

        emailToken.IsUsed = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(AuditActions.EmailVerification, userId: user.Id, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResendVerificationEmailAsync(CancellationToken cancellationToken = default)
    {
        var userId = userContext.UserId;

        if (!userId.HasValue)
        {
            return Result.Failure(ErrorMessages.Auth.NotAuthenticated, ErrorType.Unauthorized);
        }

        var user = await userManager.FindByIdAsync(userId.Value.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Auth.UserNotFound);
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure(ErrorMessages.Auth.EmailAlreadyVerified);
        }

        await SendVerificationEmailAsync(user, cancellationToken);

        await auditService.LogAsync(AuditActions.ResendVerificationEmail, userId: userId.Value, ct: cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Resolves a 2FA challenge token from plaintext, validating it is not expired, used, or locked.
    /// Returns a distinct error for each rejection reason.
    /// </summary>
    private async Task<Result<TwoFactorChallenge>> ResolveChallengeAsync(string plainToken, CancellationToken cancellationToken)
    {
        var hashedToken = HashHelper.Sha256(plainToken);
        var challenge = await dbContext.TwoFactorChallenges
            .FirstOrDefaultAsync(c => c.Token == hashedToken, cancellationToken);

        if (challenge is null || challenge.IsUsed)
        {
            return Result<TwoFactorChallenge>.Failure(ErrorMessages.TwoFactor.ChallengeNotFound, ErrorType.Unauthorized);
        }

        if (challenge.ExpiresAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            return Result<TwoFactorChallenge>.Failure(ErrorMessages.TwoFactor.ChallengeNotFound, ErrorType.Unauthorized);
        }

        if (challenge.FailedAttempts >= _twoFactorOptions.MaxChallengeAttempts)
        {
            await auditService.LogAsync(AuditActions.TwoFactorLoginFailure, userId: challenge.UserId, ct: cancellationToken);
            return Result<TwoFactorChallenge>.Failure(ErrorMessages.TwoFactor.ChallengeLocked, ErrorType.Unauthorized);
        }

        return Result<TwoFactorChallenge>.Success(challenge);
    }

    /// <summary>
    /// Verifies the candidate password against a throwaway hash so that login response timing
    /// does not reveal whether the account exists or has a password set. Uses the password
    /// hasher configured in DI (via <see cref="UserManager{TUser}.PasswordHasher"/>) so the
    /// dummy verification always matches the real work factor.
    /// </summary>
    private void BurnDummyPasswordVerification(string password)
    {
        var dummyUser = new ApplicationUser();

        // Benign race: concurrent first calls may compute the hash twice, all results are equivalent
        _dummyPasswordHash ??= userManager.PasswordHasher.HashPassword(dummyUser, Guid.NewGuid().ToString("N"));

        userManager.PasswordHasher.VerifyHashedPassword(dummyUser, _dummyPasswordHash, password);
    }

    /// <summary>
    /// Generates a cryptographically random challenge token as a base64 string.
    /// </summary>
    private static string GenerateChallengeToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Sends a verification email to the specified user with a confirmation link.
    /// </summary>
    private async Task SendVerificationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            logger.LogWarning("Cannot send verification email: user {UserId} has no email address", user.Id);
            return;
        }

        var identityToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var opaqueToken = await emailTokenService.CreateAsync(user.Id, identityToken, EmailTokenPurpose.EmailVerification, cancellationToken);
        var verifyUrl = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/verify-email?token={opaqueToken}";

        var model = new VerifyEmailModel(verifyUrl);
        await templatedEmailSender.SendSafeAsync(EmailTemplateNames.VerifyEmail, model, user.Email, cancellationToken);
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
