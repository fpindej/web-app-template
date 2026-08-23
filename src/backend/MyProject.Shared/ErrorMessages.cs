namespace MyProject.Shared;

/// <summary>
/// User-facing errors organized by domain area. Each entry is an <see cref="Error"/> pairing a stable,
/// machine-readable code with a human-readable message. Entries are used in <c>Result.Failure()</c> calls
/// so that messages remain consistent, greppable, and translatable by clients via the code.
/// <para>
/// Codes follow <c>{nested_class}_{field_name}</c> in snake_case and are verified by
/// <c>ErrorMessagesTests</c>. Renaming a code is a breaking change for API consumers.
/// </para>
/// <para>
/// All client-facing messages must be static - never interpolate runtime values
/// (role names, user IDs, framework error descriptions) into error responses.
/// Log runtime details server-side via <c>ILogger</c> instead. The only exception is
/// an entry explicitly documented as carrying a dynamic message (rate-limit retry hints);
/// it keeps its stable code and overrides <see cref="Error.Message"/> with a <c>with</c> expression.
/// </para>
/// </summary>
public static class ErrorMessages
{
    /// <summary>
    /// Authentication error messages.
    /// </summary>
    public static class Auth
    {
        public static readonly Error LoginInvalidCredentials = new("auth_login_invalid_credentials", "Invalid username or password.");
        public static readonly Error LoginAccountLocked = new("auth_login_account_locked", "Account is temporarily locked. Please try again later or contact an administrator.");
        public static readonly Error RegisterRoleAssignFailed = new("auth_register_role_assign_failed", "Account was created but role assignment failed. Please contact an administrator.");
        public static readonly Error TokenMissing = new("auth_token_missing", "Refresh token is missing.");
        public static readonly Error TokenNotFound = new("auth_token_not_found", "Refresh token not found.");
        public static readonly Error TokenInvalidated = new("auth_token_invalidated", "Refresh token has been invalidated.");
        public static readonly Error TokenReused = new("auth_token_reused", "Invalid refresh token.");
        public static readonly Error TokenExpired = new("auth_token_expired", "Refresh token has expired.");
        public static readonly Error TokenUserNotFound = new("auth_token_user_not_found", "Token owner not found.");
        public static readonly Error NotAuthenticated = new("auth_not_authenticated", "User is not authenticated.");
        public static readonly Error InsufficientPermissions = new("auth_insufficient_permissions", "You do not have the required permissions for this action.");
        public static readonly Error UserNotFound = new("auth_user_not_found", "User not found.");
        public static readonly Error PasswordIncorrect = new("auth_password_incorrect", "Current password is incorrect.");
        public static readonly Error ResetPasswordFailed = new("auth_reset_password_failed", "Password reset failed. The link may have expired or already been used.");
        public static readonly Error ResetPasswordTokenInvalid = new("auth_reset_password_token_invalid", "Invalid or expired password reset token.");
        public static readonly Error EmailVerificationFailed = new("auth_email_verification_failed", "Email verification failed. The link may have expired or already been used.");
        public static readonly Error EmailAlreadyVerified = new("auth_email_already_verified", "Email address is already verified.");
        public static readonly Error CaptchaInvalid = new("auth_captcha_invalid", "CAPTCHA verification failed. Please try again.");

        /// <summary>
        /// Registration rejected by ASP.NET Identity (password policy, duplicate email). The message is
        /// deliberately generic to prevent email enumeration; Identity error codes are logged server-side.
        /// Request validation gives actionable password-policy feedback before this point.
        /// </summary>
        public static readonly Error RegistrationInvalid = new("auth_registration_invalid", "Registration failed. Please check the provided details.");

        /// <summary>
        /// New password rejected by the server-side password policy. The message is static; Identity
        /// error codes are logged server-side. Request validation gives actionable password-policy
        /// feedback before this point.
        /// </summary>
        public static readonly Error PasswordPolicyViolation = new("auth_password_policy_violation", "The new password does not meet the password requirements.");
    }

    /// <summary>
    /// Two-factor authentication error messages.
    /// </summary>
    public static class TwoFactor
    {
        public static readonly Error SetupFailed = new("two_factor_setup_failed", "Failed to set up two-factor authentication.");
        public static readonly Error VerificationFailed = new("two_factor_verification_failed", "The verification code is invalid. Please try again.");
        public static readonly Error AlreadyEnabled = new("two_factor_already_enabled", "Two-factor authentication is already enabled.");
        public static readonly Error NotEnabled = new("two_factor_not_enabled", "Two-factor authentication is not enabled.");
        public static readonly Error DisableFailed = new("two_factor_disable_failed", "Failed to disable two-factor authentication.");
        public static readonly Error ChallengeNotFound = new("two_factor_challenge_not_found", "Two-factor challenge not found or expired.");
        public static readonly Error ChallengeLocked = new("two_factor_challenge_locked", "Too many failed attempts. Please log in again.");
        public static readonly Error RecoveryCodeInvalid = new("two_factor_recovery_code_invalid", "The recovery code is invalid.");
        public static readonly Error InvalidCode = new("two_factor_invalid_code", "The two-factor code is invalid.");
    }

    /// <summary>
    /// User self-service error messages.
    /// </summary>
    public static class User
    {
        public static readonly Error NotAuthenticated = new("user_not_authenticated", "User is not authenticated.");
        public static readonly Error NotFound = new("user_not_found", "User not found.");
        public static readonly Error DeleteInvalidPassword = new("user_delete_invalid_password", "Invalid password.");
        public static readonly Error PhoneNumberTaken = new("user_phone_number_taken", "This phone number is already in use.");
        public static readonly Error UpdateFailed = new("user_update_failed", "Failed to update profile.");
        public static readonly Error DeleteFailed = new("user_delete_failed", "Failed to delete account.");
        public static readonly Error LastSuperuserCannotDelete = new("user_last_superuser_cannot_delete", "Cannot delete your account while you are the last superuser.");
    }

    /// <summary>
    /// Administrative operation error messages.
    /// </summary>
    public static class Admin
    {
        public static readonly Error UserNotFound = new("admin_user_not_found", "User not found.");
        public static readonly Error HierarchyInsufficient = new("admin_hierarchy_insufficient", "You do not have sufficient privileges to manage this user.");
        public static readonly Error RoleAssignAboveRank = new("admin_role_assign_above_rank", "Cannot assign a role at or above your own rank.");
        public static readonly Error RoleRemoveAboveRank = new("admin_role_remove_above_rank", "Cannot remove a role at or above your own rank.");
        public static readonly Error RoleSelfRemove = new("admin_role_self_remove", "Cannot remove a role from your own account.");
        public static readonly Error LockSelfAction = new("admin_lock_self_action", "Cannot lock your own account.");
        public static readonly Error DeleteSelfAction = new("admin_delete_self_action", "Cannot delete your own account.");
        public static readonly Error EmailVerificationRequired = new("admin_email_verification_required", "User must have a verified email address before being assigned this role.");
        public static readonly Error EmailAlreadyRegistered = new("admin_email_already_registered", "A user with this email address already exists.");
        public static readonly Error RoleAssignEscalation = new("admin_role_assign_escalation", "Cannot assign a role that grants permissions you do not hold.");
        public static readonly Error RoleNotFound = new("admin_role_not_found", "Role not found.");
        public static readonly Error RoleAlreadyAssigned = new("admin_role_already_assigned", "User already has this role.");
        public static readonly Error RoleNotAssigned = new("admin_role_not_assigned", "User does not have this role.");
        public static readonly Error LastRoleHolder = new("admin_last_role_holder", "Cannot remove this role - this is the last user holding it.");
        public static readonly Error RoleAssignFailed = new("admin_role_assign_failed", "Failed to assign role.");
        public static readonly Error RoleRemoveFailed = new("admin_role_remove_failed", "Failed to remove role.");
        public static readonly Error LockFailed = new("admin_lock_failed", "Failed to lock user account.");
        public static readonly Error UnlockFailed = new("admin_unlock_failed", "Failed to unlock user account.");
        public static readonly Error DeleteFailed = new("admin_delete_failed", "Failed to delete user account.");
        public static readonly Error EmailVerificationFailed = new("admin_email_verification_failed", "Failed to verify email address.");
        public static readonly Error CreateUserFailed = new("admin_create_user_failed", "Failed to create user account.");
        public static readonly Error LastSuperuserCannotDelete = new("admin_last_superuser_cannot_delete", "Cannot delete this user - they are the last superuser.");
        public static readonly Error TwoFactorNotEnabled = new("admin_two_factor_not_enabled", "Two-factor authentication is not enabled for this user.");
        public static readonly Error DisableTwoFactorSelfAction = new("admin_disable_two_factor_self_action", "You cannot disable your own two-factor authentication from the admin panel.");
        public static readonly Error DisableTwoFactorFailed = new("admin_disable_two_factor_failed", "Failed to disable two-factor authentication.");
    }

    /// <summary>
    /// Role management error messages.
    /// </summary>
    public static class Roles
    {
        public static readonly Error SystemRoleCannotBeDeleted = new("roles_system_role_cannot_be_deleted", "System roles cannot be deleted.");
        public static readonly Error SystemRoleCannotBeRenamed = new("roles_system_role_cannot_be_renamed", "System roles cannot be renamed.");
        public static readonly Error RoleNotFound = new("roles_role_not_found", "Role not found.");
        public static readonly Error RoleNameTaken = new("roles_role_name_taken", "A role with this name already exists.");
        public static readonly Error RoleHasUsers = new("roles_role_has_users", "Cannot delete a role that has users assigned to it.");
        public static readonly Error InvalidPermission = new("roles_invalid_permission", "One or more permission values are invalid.");
        public static readonly Error SystemRoleNameReserved = new("roles_system_role_name_reserved", "This name is reserved for a system role.");
        public static readonly Error SuperuserPermissionsFixed = new("roles_superuser_permissions_fixed", "Permissions of a role that grants all permissions cannot be modified.");
        public static readonly Error CannotGrantUnheldPermission = new("roles_cannot_grant_unheld_permission", "Cannot grant permissions that you do not hold.");
        public static readonly Error CreateFailed = new("roles_create_failed", "Failed to create role.");
        public static readonly Error UpdateFailed = new("roles_update_failed", "Failed to update role.");
        public static readonly Error DeleteFailed = new("roles_delete_failed", "Failed to delete role.");
    }

    /// <summary>
    /// Pagination error messages.
    /// </summary>
    public static class Pagination
    {
        public static readonly Error InvalidPage = new("pagination_invalid_page", "Page number must be positive.");
        public static readonly Error InvalidPageSize = new("pagination_invalid_page_size", "Page size must be positive.");
    }

    /// <summary>
    /// Server-level error messages.
    /// </summary>
    public static class Server
    {
        public static readonly Error InternalError = new("server_internal_error", "An internal error occurred.");

        /// <summary>
        /// Request rejected by the rate limiter. The message is overridden with the retry hint
        /// (seconds until the window resets).
        /// </summary>
        public static readonly Error TooManyRequests = new("server_too_many_requests", "Too many requests. Please try again later.");
    }

    /// <summary>
    /// Job scheduling error messages.
    /// </summary>
    public static class Jobs
    {
        public static readonly Error NotFound = new("jobs_not_found", "Job not found.");
        public static readonly Error TriggerFailed = new("jobs_trigger_failed", "Failed to trigger job.");
        public static readonly Error RestoreFailed = new("jobs_restore_failed", "Failed to restore jobs.");
    }

    /// <summary>
    /// Security infrastructure error messages (CSRF, origin validation).
    /// </summary>
    public static class Security
    {
        public static readonly Error CrossOriginRequestBlocked = new("security_cross_origin_request_blocked", "Cross-origin requests are not allowed.");
    }

    /// <summary>
    /// Avatar upload and processing error messages.
    /// </summary>
    public static class Avatar
    {
        public static readonly Error FileTooLarge = new("avatar_file_too_large", "The file exceeds the maximum allowed size of 5 MB.");
        public static readonly Error UnsupportedFormat = new("avatar_unsupported_format", "Unsupported image format. Allowed formats: JPEG, PNG, WebP, GIF.");
        public static readonly Error ProcessingFailed = new("avatar_processing_failed", "Failed to process the avatar image.");
        public static readonly Error NotFound = new("avatar_not_found", "Avatar not found.");
    }

    /// <summary>
    /// File storage (S3-compatible) error messages.
    /// </summary>
    public static class FileStorage
    {
        public static readonly Error UploadFailed = new("file_storage_upload_failed", "Failed to upload file to storage.");
        public static readonly Error DownloadFailed = new("file_storage_download_failed", "Failed to retrieve file from storage.");
        public static readonly Error DeleteFailed = new("file_storage_delete_failed", "Failed to delete file from storage.");
        public static readonly Error NotFound = new("file_storage_not_found", "File not found.");
    }

    /// <summary>
    /// External authentication (OAuth2) error messages.
    /// </summary>
    public static class ExternalAuth
    {
        public static readonly Error ProviderNotConfigured = new("external_auth_provider_not_configured", "The requested authentication provider is not configured.");
        public static readonly Error InvalidState = new("external_auth_invalid_state", "Invalid or missing OAuth state token.");
        public static readonly Error StateExpired = new("external_auth_state_expired", "OAuth state token has expired. Please try again.");
        public static readonly Error EmailNotVerified = new("external_auth_email_not_verified", "Your email address must be verified before linking an external account. Please verify your email first.");
        public static readonly Error AlreadyLinkedToOtherUser = new("external_auth_already_linked_to_other_user", "This external account is already linked to another user.");
        public static readonly Error ProviderNotLinked = new("external_auth_provider_not_linked", "This provider is not linked to your account.");
        public static readonly Error CannotUnlinkLastMethod = new("external_auth_cannot_unlink_last_method", "Cannot unlink this provider because it is your only sign-in method. Set a password first.");
        public static readonly Error CodeExchangeFailed = new("external_auth_code_exchange_failed", "Failed to exchange the authorization code with the provider.");
        public static readonly Error ProviderError = new("external_auth_provider_error", "The external authentication provider returned an error.");
        public static readonly Error InvalidRedirectUri = new("external_auth_invalid_redirect_uri", "The provided redirect URI is not allowed.");
        public static readonly Error PasswordAlreadySet = new("external_auth_password_already_set", "A password is already set for this account.");
        public static readonly Error PasswordSetFailed = new("external_auth_password_set_failed", "Failed to set the password. Please try again.");
        public static readonly Error UnknownProvider = new("external_auth_unknown_provider", "The specified authentication provider is not recognized.");
        public static readonly Error ClientSecretRequired = new("external_auth_client_secret_required", "A client secret is required when enabling a provider that has no existing secret.");
        public static readonly Error TestConnectionInvalidCredentials = new("external_auth_test_connection_invalid_credentials", "The provider rejected the credentials. Verify the client ID and secret are correct.");
        public static readonly Error TestConnectionProviderUnreachable = new("external_auth_test_connection_provider_unreachable", "Could not reach the authentication provider. Please try again later.");
        public static readonly Error TestConnectionNotConfigured = new("external_auth_test_connection_not_configured", "No credentials are configured for this provider.");
    }

    /// <summary>
    /// Generic entity operation error messages (repository layer).
    /// </summary>
    public static class Entity
    {
        public static readonly Error AddFailed = new("entity_add_failed", "Failed to add entity.");
        public static readonly Error NotFound = new("entity_not_found", "Entity not found.");
        public static readonly Error NotDeleted = new("entity_not_deleted", "Entity could not be deleted.");
    }
}
