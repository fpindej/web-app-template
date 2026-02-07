namespace MyProject.Application.Errors;

/// <summary>
/// Standardized error codes returned by the API.
/// Frontend clients use these codes as i18n message keys to display localized error messages.
/// Convention: {domain}.{subject}.{rule} — all lowercase, dot-separated.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Validation error codes for Data Annotation and FluentValidation failures.
    /// These appear in <c>ValidationProblemDetails.errors</c> as the error message array values.
    /// </summary>
    public static class Validation
    {
        // General field rules
        public const string Required = "validation.required";
        public const string InvalidEmail = "validation.invalidEmail";
        public const string MaxLength = "validation.maxLength";
        public const string MinLength = "validation.minLength";
        public const string InvalidUrl = "validation.invalidUrl";
        public const string InvalidPhoneNumber = "validation.invalidPhoneNumber";
    }

    /// <summary>
    /// Error codes returned by ASP.NET Identity via the custom <c>ErrorCodeIdentityErrorDescriber</c>.
    /// These appear in <c>ErrorResponse.message</c> from <c>Result.Failure()</c> calls.
    /// </summary>
    public static class Identity
    {
        public const string DuplicateEmail = "identity.duplicateEmail";
        public const string DuplicateUserName = "identity.duplicateUserName";
        public const string InvalidEmail = "identity.invalidEmail";
        public const string InvalidUserName = "identity.invalidUserName";
        public const string PasswordRequiresDigit = "identity.passwordRequiresDigit";
        public const string PasswordRequiresLower = "identity.passwordRequiresLower";
        public const string PasswordRequiresUpper = "identity.passwordRequiresUpper";
        public const string PasswordRequiresNonAlphanumeric = "identity.passwordRequiresNonAlphanumeric";
        public const string PasswordRequiresUniqueChars = "identity.passwordRequiresUniqueChars";
        public const string PasswordTooShort = "identity.passwordTooShort";
        public const string UserAlreadyInRole = "identity.userAlreadyInRole";
        public const string UserNotInRole = "identity.userNotInRole";
        public const string UserLockout = "identity.userLockout";
        public const string ConcurrencyFailure = "identity.concurrencyFailure";
        public const string DefaultError = "identity.defaultError";
    }

    /// <summary>
    /// Authentication-related business logic error codes.
    /// </summary>
    public static class Auth
    {
        public const string InvalidCredentials = "auth.invalidCredentials";
        public const string RefreshTokenMissing = "auth.refreshTokenMissing";
        public const string RefreshTokenNotFound = "auth.refreshTokenNotFound";
        public const string RefreshTokenInvalidated = "auth.refreshTokenInvalidated";
        public const string RefreshTokenReused = "auth.refreshTokenReused";
        public const string RefreshTokenExpired = "auth.refreshTokenExpired";
        public const string UserNotFound = "auth.userNotFound";
    }

    /// <summary>
    /// User/profile-related business logic error codes.
    /// </summary>
    public static class User
    {
        public const string NotAuthenticated = "user.notAuthenticated";
        public const string NotFound = "user.notFound";
    }
}
