using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Hybrid;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Features.Admin;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Email;
using MyProject.Application.Features.Email.Models;
using MyProject.Application.Features.FileStorage;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Infrastructure.Persistence.Extensions;
using MyProject.Shared;

namespace MyProject.Infrastructure.Features.Admin.Services;

/// <summary>
/// Identity-backed implementation of <see cref="IAdminService"/> for administrative user and role management.
/// <para>
/// All mutation operations enforce role hierarchy: the caller must have a strictly higher role rank
/// than the target user. Self-action protection and last-admin guards are applied at this layer
/// to ensure consistent enforcement regardless of the consumer (controller, background job, etc.).
/// </para>
/// <para>
/// Destructive actions (lock, role removal, deletion) revoke all active refresh tokens for the
/// affected user and rotate their security stamp to invalidate in-flight access tokens.
/// </para>
/// </summary>
internal class AdminService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    MyProjectDbContext dbContext,
    HybridCache hybridCache,
    TimeProvider timeProvider,
    ITemplatedEmailSender templatedEmailSender,
    EmailTokenService emailTokenService,
    IAuditService auditService,
    PermissionEscalationGuard escalationGuard,
    IFileStorageService fileStorageService,
    IOptions<AuthenticationOptions> authenticationOptions,
    IOptions<EmailOptions> emailOptions,
    ILogger<AdminService> logger) : IAdminService
{
    private readonly EmailOptions _emailOptions = emailOptions.Value;
    private readonly AuthenticationOptions.EmailTokenOptions _emailTokenOptions = authenticationOptions.Value.EmailToken;

    /// <inheritdoc />
    public async Task<AdminUserListOutput> GetUsersAsync(int pageNumber, int pageSize, string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLowerInvariant();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.UserName)
            .Paginate(pageNumber, pageSize)
            .ToListAsync(cancellationToken);

        var userOutputs = await MapUsersToOutputsAsync(users, cancellationToken);

        return new AdminUserListOutput(userOutputs, totalCount, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<Result<AdminUserOutput>> GetUserByIdAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result<AdminUserOutput>.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var output = await MapUserToOutputAsync(user, cancellationToken);
        return Result<AdminUserOutput>.Success(output);
    }

    /// <inheritdoc />
    public async Task<Result> AssignRoleAsync(Guid callerUserId, Guid userId, AssignRoleInput input,
        CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByNameAsync(input.Role);
        if (role is null)
        {
            return Result.Failure(ErrorMessages.Admin.RoleNotFound);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user, cancellationToken);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        if (role.Rank >= hierarchyResult.Value)
        {
            return Result.Failure(ErrorMessages.Admin.RoleAssignAboveRank);
        }

        // Every assignment is escalation-checked: operators can expand any role's permission
        // set at runtime, so system roles are not exempt. Callers holding the target's
        // permissions (or the wildcard) pass; roles granting nothing pass unconditionally.
        var escalationResult = await EnforceRolePermissionEscalationAsync(role, callerUserId, cancellationToken);
        if (!escalationResult.IsSuccess)
        {
            return escalationResult;
        }

        if (await userManager.IsInRoleAsync(user, input.Role))
        {
            return Result.Failure(ErrorMessages.Admin.RoleAlreadyAssigned);
        }

        if (role.Rank > 0 && !user.EmailConfirmed)
        {
            return Result.Failure(ErrorMessages.Admin.EmailVerificationRequired);
        }

        var result = await userManager.AddToRoleAsync(user, input.Role);

        if (!result.Succeeded)
        {
            logger.LogWarning("AddToRoleAsync failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.RoleAssignFailed);
        }

        await RotateSecurityStampAsync(user, userId, cancellationToken);
        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Role '{Role}' assigned to user '{UserId}' by admin '{CallerUserId}'",
            input.Role, userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminAssignRole, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId,
            metadata: JsonSerializer.Serialize(new { role = input.Role }), ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveRoleAsync(Guid callerUserId, Guid userId, string role,
        CancellationToken cancellationToken = default)
    {
        var roleEntity = await roleManager.FindByNameAsync(role);
        if (roleEntity is null)
        {
            return Result.Failure(ErrorMessages.Admin.RoleNotFound);
        }

        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.RoleSelfRemove);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user, cancellationToken);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        if (roleEntity.Rank >= hierarchyResult.Value)
        {
            return Result.Failure(ErrorMessages.Admin.RoleRemoveAboveRank);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            return Result.Failure(ErrorMessages.Admin.RoleNotAssigned);
        }

        // Note: for the top-ranked Superuser role this guard is unreachable in practice -
        // the rank gate above already blocks removing a role at or above the caller's rank.
        // It protects rank-0 custom roles flagged GrantsAllPermissions.
        var lockoutResult = await EnforceLockoutInvariantForRoleRemovalAsync(userId, roleEntity, cancellationToken);
        if (!lockoutResult.IsSuccess)
        {
            return lockoutResult;
        }

        // Mutation and lockout re-verification share one transaction so two concurrent
        // removals against the last two grants-all holders cannot both slip past the
        // pre-check and jointly leave zero holders. Serializable is required: at READ
        // COMMITTED a reader neither blocks on nor sees a concurrent uncommitted delete of
        // a different row, so both re-checks could still pass; under serializable isolation
        // Postgres aborts one transaction as transient and the retrying execution strategy
        // re-runs it, failing the re-check with the stable error code. The InMemory test
        // provider is not relational and ignores transactions, hence the provider guard.
        var removalResult = Result.Success();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var result = await userManager.RemoveFromRoleAsync(user, role);
            if (!result.Succeeded)
            {
                logger.LogWarning("RemoveFromRoleAsync failed for user '{UserId}': {Errors}",
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                removalResult = Result.Failure(ErrorMessages.Admin.RoleRemoveFailed);
                return;
            }

            removalResult = await EnforceLockoutInvariantForRoleRemovalAsync(userId, roleEntity, cancellationToken);
            if (removalResult.IsFailure)
            {
                return;
            }

            await transaction.CommitAsync(cancellationToken);
        });

        if (removalResult.IsFailure)
        {
            return removalResult;
        }

        await RotateSecurityStampAsync(user, userId, cancellationToken);
        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Role '{Role}' removed from user '{UserId}' by admin '{CallerUserId}'",
            role, userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminRemoveRole, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId,
            metadata: JsonSerializer.Serialize(new { role }), ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> LockUserAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.LockSelfAction);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user, cancellationToken);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        // Set lockout end to 100 years in the future (effectively permanent)
        var lockoutEnd = timeProvider.GetUtcNow().AddYears(100);
        var result = await userManager.SetLockoutEndDateAsync(user, lockoutEnd);

        if (!result.Succeeded)
        {
            logger.LogWarning("SetLockoutEndDateAsync (lock) failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.LockFailed);
        }

        await RevokeUserSessionsAsync(user, userId, cancellationToken);
        await InvalidateUserCacheAsync(userId);
        logger.LogWarning("User '{UserId}' has been locked out by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminLockUser, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> UnlockUserAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user, cancellationToken);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var result = await userManager.SetLockoutEndDateAsync(user, null);

        if (!result.Succeeded)
        {
            logger.LogWarning("SetLockoutEndDateAsync (unlock) failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.UnlockFailed);
        }

        // Reset access failed count
        await userManager.ResetAccessFailedCountAsync(user);

        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("User '{UserId}' has been unlocked by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminUnlockUser, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteUserAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.DeleteSelfAction);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user, cancellationToken);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        // Lockout invariant: deleting the target may not leave zero grants-all holders.
        // The flag is captured before the mutation so the in-transaction re-check below
        // still fires after the target's role assignments are gone.
        var targetHeldGrantsAll = await UserHoldsGrantsAllRoleAsync(userId, cancellationToken);
        if (targetHeldGrantsAll && !await OtherGrantsAllHolderExistsAsync(userId, cancellationToken))
        {
            return Result.Failure(ErrorMessages.Admin.LastSuperuserCannotDelete);
        }

        // Captured before the mutation: the entity is detached after a committed delete.
        var hadAvatar = user.HasAvatar;

        // Mutation and lockout re-verification share one transaction so two concurrent
        // deletions of the last two grants-all holders cannot both slip past the pre-check
        // and jointly leave zero holders. Serializable is required: at READ COMMITTED a
        // reader neither blocks on nor sees a concurrent uncommitted delete of a different
        // row, so both re-checks could still pass; under serializable isolation Postgres
        // aborts one transaction as transient and the retrying execution strategy re-runs
        // it, failing the re-check with the stable error code. The InMemory test provider
        // is not relational and ignores transactions, hence the provider guard.
        var deletionResult = Result.Success();
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = dbContext.Database.IsRelational()
                ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken)
                : await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                logger.LogWarning("DeleteAsync failed for user '{UserId}': {Errors}",
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                deletionResult = Result.Failure(ErrorMessages.Admin.DeleteFailed);
                return;
            }

            if (targetHeldGrantsAll && !await OtherGrantsAllHolderExistsAsync(userId, cancellationToken))
            {
                deletionResult = Result.Failure(ErrorMessages.Admin.LastSuperuserCannotDelete);
                return;
            }

            await transaction.CommitAsync(cancellationToken);
        });

        if (deletionResult.IsFailure)
        {
            return deletionResult;
        }

        // Side effects run only after a committed delete: on rollback the surviving user
        // must keep their sessions and avatar. Refresh tokens cascade-delete with the user
        // row, and stamp validation fails closed for a missing user, so evicting the cached
        // security stamp is all that is needed to kill in-flight access tokens. Rotating
        // the stamp via UserManager would issue an update against the deleted row, leaving
        // a poisoned change tracker entry that silently breaks the audit write below.
        await hybridCache.RemoveAsync(CacheKeys.SecurityStamp(userId), cancellationToken);

        // Clean up avatar from storage if present (best-effort, never blocks the response)
        if (hadAvatar)
        {
            var avatarDeleteResult = await fileStorageService.DeleteAsync($"avatars/{userId}.webp", cancellationToken);
            if (!avatarDeleteResult.IsSuccess)
            {
                logger.LogWarning("Failed to delete avatar for user {UserId} during admin deletion: {Error}",
                    userId, avatarDeleteResult.Error);
            }
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogWarning("User '{UserId}' has been deleted by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminDeleteUser, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AdminRoleOutput>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await roleManager.Roles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var roleCounts = await dbContext.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var roleClaims = (await dbContext.RoleClaims
                .Where(rc => rc.ClaimType == AppPermissions.ClaimType)
                .Select(rc => new { rc.RoleId, rc.ClaimValue })
                .ToListAsync(cancellationToken))
            .GroupBy(rc => rc.RoleId)
            .ToDictionary(g => g.Key, g => g.Select(c => c.ClaimValue!).ToList());

        return roles
            .Select(role => new AdminRoleOutput(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                role.IsSystem,
                roleCounts.GetValueOrDefault(role.Id),
                roleClaims.GetValueOrDefault(role.Id, []),
                role.Rank,
                role.GrantsAllPermissions))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<Result> VerifyEmailAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user, cancellationToken);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        if (user.EmailConfirmed)
        {
            return Result.Failure(ErrorMessages.Auth.EmailAlreadyVerified);
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var confirmResult = await userManager.ConfirmEmailAsync(user, token);

        if (!confirmResult.Succeeded)
        {
            logger.LogWarning("ConfirmEmailAsync failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", confirmResult.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.EmailVerificationFailed);
        }

        await InvalidateUserCacheAsync(userId);
        logger.LogInformation("Email for user '{UserId}' manually verified by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminVerifyEmail, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> SendPasswordResetAsync(Guid callerUserId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user, cancellationToken);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var opaqueToken = await emailTokenService.CreateAsync(user.Id, identityToken, EmailTokenPurpose.PasswordReset, cancellationToken);
        var email = user.Email ?? user.UserName ?? string.Empty;
        var resetUrl = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={opaqueToken}";

        var model = new AdminResetPasswordModel(resetUrl, _emailTokenOptions.Lifetime.ToHumanReadable());
        await templatedEmailSender.SendSafeAsync(EmailTemplateNames.AdminResetPassword, model, email, cancellationToken);

        logger.LogInformation("Password reset email sent for user '{UserId}' by admin '{CallerUserId}'",
            userId, callerUserId);

        await auditService.LogAsync(AuditActions.AdminSendPasswordReset, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId, ct: cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DisableTwoFactorAsync(Guid callerUserId, Guid userId, string? reason,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return Result.Failure(ErrorMessages.Admin.UserNotFound, ErrorType.NotFound);
        }

        if (callerUserId == userId)
        {
            return Result.Failure(ErrorMessages.Admin.DisableTwoFactorSelfAction);
        }

        var hierarchyResult = await EnforceHierarchyAsync(callerUserId, user, cancellationToken);
        if (!hierarchyResult.IsSuccess)
        {
            return hierarchyResult;
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            return Result.Failure(ErrorMessages.Admin.TwoFactorNotEnabled);
        }

        var disableResult = await userManager.SetTwoFactorEnabledAsync(user, false);
        if (!disableResult.Succeeded)
        {
            logger.LogWarning("SetTwoFactorEnabledAsync (disable) failed for user '{UserId}': {Errors}",
                userId, string.Join(", ", disableResult.Errors.Select(e => e.Description)));
            return Result.Failure(ErrorMessages.Admin.DisableTwoFactorFailed);
        }

        await userManager.ResetAuthenticatorKeyAsync(user);
        await RevokeUserSessionsAsync(user, userId, cancellationToken);
        await InvalidateUserCacheAsync(userId);
        logger.LogWarning("Two-factor authentication disabled for user '{UserId}' by admin '{CallerUserId}'",
            userId, callerUserId);

        var metadata = reason is not null
            ? JsonSerializer.Serialize(new { reason })
            : null;

        await auditService.LogAsync(AuditActions.AdminDisableTwoFactor, userId: callerUserId,
            targetEntityType: "User", targetEntityId: userId,
            metadata: metadata, ct: cancellationToken);

        var email = user.Email ?? user.UserName ?? string.Empty;
        var model = new AdminDisableTwoFactorModel(user.UserName ?? email, reason);
        await templatedEmailSender.SendSafeAsync(EmailTemplateNames.AdminDisableTwoFactor, model, email, cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CreateUserAsync(Guid callerUserId, CreateUserInput input,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await userManager.FindByEmailAsync(input.Email);
        if (existingUser is not null)
        {
            return Result<Guid>.Failure(ErrorMessages.Admin.EmailAlreadyRegistered);
        }

        var tempPassword = GenerateTemporaryPassword();

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email,
            EmailConfirmed = true,
            FirstName = input.FirstName,
            LastName = input.LastName,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, tempPassword);
        if (!createResult.Succeeded)
        {
            logger.LogWarning("CreateAsync failed for admin-created user '{MaskedEmail}': {ErrorCodes}",
                PiiMasker.MaskEmail(input.Email), string.Join(", ", createResult.Errors.Select(e => e.Code)));
            return Result<Guid>.Failure(ErrorMessages.Admin.CreateUserFailed);
        }

        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.User);
        if (!roleResult.Succeeded)
        {
            logger.LogWarning("User '{UserId}' created but default role assignment failed", user.Id);
        }

        // Send invitation email with password reset link
        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var opaqueToken = await emailTokenService.CreateAsync(user.Id, identityToken, EmailTokenPurpose.PasswordReset, cancellationToken);
        var setPasswordUrl = $"{_emailOptions.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={opaqueToken}&invited=1";

        var invitationModel = new InvitationModel(setPasswordUrl, _emailTokenOptions.Lifetime.ToHumanReadable());
        await templatedEmailSender.SendSafeAsync(EmailTemplateNames.Invitation, invitationModel, input.Email, cancellationToken);

        logger.LogInformation("User '{UserId}' created via admin invitation by admin '{CallerUserId}'",
            user.Id, callerUserId);

        await auditService.LogAsync(AuditActions.AdminCreateUser, userId: callerUserId,
            targetEntityType: "User", targetEntityId: user.Id, ct: cancellationToken);

        return Result<Guid>.Success(user.Id);
    }

    /// <summary>
    /// Verifies that the caller has a strictly higher role rank than the target user.
    /// Both effective ranks are resolved from role metadata in a single query.
    /// On success the result carries the caller's effective rank so rank gates at the
    /// call sites do not have to query it again.
    /// </summary>
    private async Task<Result<int>> EnforceHierarchyAsync(Guid callerUserId, ApplicationUser targetUser,
        CancellationToken cancellationToken)
    {
        var callerRoles = await GetUserRolesAsync(callerUserId);
        var targetRoles = await userManager.GetRolesAsync(targetUser);

        var ranksByName = await GetRoleMetadataAsync(callerRoles.Concat(targetRoles), cancellationToken);

        var callerRank = GetHighestRank(callerRoles, ranksByName);
        var targetRank = GetHighestRank(targetRoles, ranksByName);

        if (callerRank <= targetRank)
        {
            return Result<int>.Failure(ErrorMessages.Admin.HierarchyInsufficient);
        }

        return Result<int>.Success(callerRank);
    }

    /// <summary>
    /// Loads role metadata for the given role names in a single query, keyed by normalized name.
    /// Names without a matching role row are simply absent from the result (effective rank 0).
    /// </summary>
    private async Task<Dictionary<string, int>> GetRoleMetadataAsync(
        IEnumerable<string> roleNames, CancellationToken cancellationToken)
    {
        var normalizedNames = roleNames
            .Select(r => r.ToUpperInvariant())
            .Distinct()
            .ToList();

        return await dbContext.Roles
            .AsNoTracking()
            .Where(r => normalizedNames.Contains(r.NormalizedName!))
            .ToDictionaryAsync(r => r.NormalizedName!, r => r.Rank, cancellationToken);
    }

    /// <summary>
    /// Returns the highest rank among the given role names based on stored role metadata.
    /// Unknown roles contribute rank 0.
    /// </summary>
    private static int GetHighestRank(IEnumerable<string> roleNames, Dictionary<string, int> ranksByName) =>
        roleNames
            .Select(r => ranksByName.GetValueOrDefault(r.ToUpperInvariant()))
            .DefaultIfEmpty(0)
            .Max();

    /// <summary>
    /// Enforces the lockout invariant for a role removal: the operation may not leave zero
    /// users holding any role that grants all permissions. Only checked when the role being
    /// removed grants all permissions; the affected assignment itself is excluded from the count.
    /// <para>
    /// For the Superuser role itself this guard is effectively unreachable via
    /// <see cref="RemoveRoleAsync"/>: the rank gate (role rank at or above the caller's rank
    /// is blocked) fires first for a max-rank role, so the <c>LastRoleHolder</c> failure is
    /// only reachable for rank-0 roles flagged <c>GrantsAllPermissions</c>.
    /// </para>
    /// </summary>
    private async Task<Result> EnforceLockoutInvariantForRoleRemovalAsync(Guid userId,
        ApplicationRole role, CancellationToken cancellationToken)
    {
        if (!role.GrantsAllPermissions)
        {
            return Result.Success();
        }

        var otherHolderExists = await dbContext.UserRoles
            .Join(dbContext.Roles.Where(r => r.GrantsAllPermissions),
                ur => ur.RoleId,
                r => r.Id,
                (ur, _) => new { ur.UserId, ur.RoleId })
            .Where(x => x.UserId != userId || x.RoleId != role.Id)
            .AnyAsync(cancellationToken);

        return otherHolderExists
            ? Result.Success()
            : Result.Failure(ErrorMessages.Admin.LastRoleHolder);
    }

    /// <summary>
    /// Determines whether the given user currently holds any role that grants all permissions.
    /// </summary>
    private async Task<bool> UserHoldsGrantsAllRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(dbContext.Roles.Where(r => r.GrantsAllPermissions),
                ur => ur.RoleId,
                r => r.Id,
                (ur, _) => ur.UserId)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Determines whether any user other than <paramref name="userId"/> holds a role that
    /// grants all permissions.
    /// </summary>
    private async Task<bool> OtherGrantsAllHolderExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles
            .Where(ur => ur.UserId != userId)
            .Join(dbContext.Roles.Where(r => r.GrantsAllPermissions),
                ur => ur.RoleId,
                r => r.Id,
                (ur, _) => ur.UserId)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// Verifies that the caller holds everything the target role grants. A grants-all role
    /// carries no stored permission claims, so assigning it hands out wildcard access; the
    /// caller must therefore hold the wildcard themselves. For other roles the stored
    /// permission claims are required; roles granting nothing are allowed unconditionally.
    /// </summary>
    private async Task<Result> EnforceRolePermissionEscalationAsync(ApplicationRole targetRole,
        Guid callerUserId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> requiredPermissions;

        if (targetRole.GrantsAllPermissions)
        {
            requiredPermissions = [AppPermissions.Wildcard];
        }
        else
        {
            var targetClaims = await roleManager.GetClaimsAsync(targetRole);
            requiredPermissions = targetClaims
                .Where(c => c.Type == AppPermissions.ClaimType)
                .Select(c => c.Value)
                .ToList();
        }

        return await escalationGuard.EnsureCallerHoldsAllAsync(callerUserId, requiredPermissions,
            ErrorMessages.Admin.RoleAssignEscalation, cancellationToken);
    }

    /// <summary>
    /// Rotates a user's security stamp, invalidating their current access token.
    /// Refresh tokens are preserved so the frontend can silently re-authenticate
    /// and obtain a new JWT with updated claims.
    /// </summary>
    private async Task RotateSecurityStampAsync(ApplicationUser user, Guid userId,
        CancellationToken cancellationToken)
    {
        await userManager.UpdateSecurityStampAsync(user);
        await hybridCache.RemoveAsync(CacheKeys.SecurityStamp(userId), cancellationToken);
    }

    /// <summary>
    /// Revokes all active refresh tokens for a user and rotates their security stamp,
    /// forcing re-authentication on all devices.
    /// </summary>
    private async Task RevokeUserSessionsAsync(ApplicationUser user, Guid userId,
        CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsInvalidated)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsInvalidated = true;
        }

        if (tokens.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await userManager.UpdateSecurityStampAsync(user);
        await hybridCache.RemoveAsync(CacheKeys.SecurityStamp(userId), cancellationToken);
    }

    private async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return [];
        }

        return await userManager.GetRolesAsync(user);
    }

    private async Task<IReadOnlyList<AdminUserOutput>> MapUsersToOutputsAsync(
        IReadOnlyList<ApplicationUser> users, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var userIds = users.Select(u => u.Id).ToList();

        var userRolesMap = await dbContext.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name ?? string.Empty })
            .GroupBy(x => x.UserId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Select(x => x.RoleName).ToList(),
                cancellationToken);

        return users.Select(user =>
        {
            var roles = userRolesMap.GetValueOrDefault(user.Id, []);
            var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > now;

            return new AdminUserOutput(
                Id: user.Id,
                UserName: user.UserName ?? string.Empty,
                FirstName: user.FirstName,
                LastName: user.LastName,
                PhoneNumber: user.PhoneNumber,
                Bio: user.Bio,
                HasAvatar: user.HasAvatar,
                Roles: roles,
                EmailConfirmed: user.EmailConfirmed,
                LockoutEnabled: user.LockoutEnabled,
                LockoutEnd: user.LockoutEnd,
                AccessFailedCount: user.AccessFailedCount,
                IsLockedOut: isLockedOut,
                IsTwoFactorEnabled: user.TwoFactorEnabled);
        }).ToList();
    }

    private async Task<AdminUserOutput> MapUserToOutputAsync(ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roleNames = await dbContext.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id,
                (_, r) => r.Name ?? string.Empty)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd > now;

        return new AdminUserOutput(
            Id: user.Id,
            UserName: user.UserName ?? string.Empty,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            Bio: user.Bio,
            HasAvatar: user.HasAvatar,
            Roles: roleNames,
            EmailConfirmed: user.EmailConfirmed,
            LockoutEnabled: user.LockoutEnabled,
            LockoutEnd: user.LockoutEnd,
            AccessFailedCount: user.AccessFailedCount,
            IsLockedOut: isLockedOut,
            IsTwoFactorEnabled: user.TwoFactorEnabled);
    }

    private async Task InvalidateUserCacheAsync(Guid userId)
    {
        await hybridCache.RemoveAsync(CacheKeys.User(userId));
    }

    /// <summary>
    /// Generates a cryptographically random temporary password that satisfies default ASP.NET Identity complexity rules.
    /// </summary>
    private static string GenerateTemporaryPassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*";
        const string all = upper + lower + digits + special;

        Span<byte> randomBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(randomBytes);

        var password = new char[32];
        // Ensure at least one of each required category
        password[0] = upper[randomBytes[0] % upper.Length];
        password[1] = lower[randomBytes[1] % lower.Length];
        password[2] = digits[randomBytes[2] % digits.Length];
        password[3] = special[randomBytes[3] % special.Length];

        for (var i = 4; i < 32; i++)
        {
            password[i] = all[randomBytes[i] % all.Length];
        }

        // Shuffle to avoid predictable prefix
        Span<byte> shuffleBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(shuffleBytes);
        for (var i = password.Length - 1; i > 0; i--)
        {
            var j = shuffleBytes[i] % (i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }
}
