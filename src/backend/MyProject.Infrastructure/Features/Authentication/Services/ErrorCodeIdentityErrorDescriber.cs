using Microsoft.AspNetCore.Identity;
using MyProject.Application.Errors;

namespace MyProject.Infrastructure.Features.Authentication.Services;

/// <summary>
/// Custom Identity error describer that returns standardized error codes
/// instead of English prose messages. Frontend clients use these codes
/// as i18n keys to display localized error messages.
/// </summary>
internal class ErrorCodeIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = ErrorCodes.Identity.DuplicateEmail };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = ErrorCodes.Identity.DuplicateUserName };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = ErrorCodes.Identity.InvalidEmail };

    public override IdentityError InvalidUserName(string? userName) =>
        new() { Code = nameof(InvalidUserName), Description = ErrorCodes.Identity.InvalidUserName };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = ErrorCodes.Identity.PasswordRequiresDigit };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = ErrorCodes.Identity.PasswordRequiresLower };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = ErrorCodes.Identity.PasswordRequiresUpper };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = ErrorCodes.Identity.PasswordRequiresNonAlphanumeric };

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        new() { Code = nameof(PasswordRequiresUniqueChars), Description = ErrorCodes.Identity.PasswordRequiresUniqueChars };

    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = ErrorCodes.Identity.PasswordTooShort };

    public override IdentityError UserAlreadyInRole(string role) =>
        new() { Code = nameof(UserAlreadyInRole), Description = ErrorCodes.Identity.UserAlreadyInRole };

    public override IdentityError UserNotInRole(string role) =>
        new() { Code = nameof(UserNotInRole), Description = ErrorCodes.Identity.UserNotInRole };

    public override IdentityError UserAlreadyHasPassword() =>
        new() { Code = nameof(UserAlreadyHasPassword), Description = ErrorCodes.Identity.DefaultError };

    public override IdentityError UserLockoutNotEnabled() =>
        new() { Code = nameof(UserLockoutNotEnabled), Description = ErrorCodes.Identity.UserLockout };

    public override IdentityError ConcurrencyFailure() =>
        new() { Code = nameof(ConcurrencyFailure), Description = ErrorCodes.Identity.ConcurrencyFailure };

    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = ErrorCodes.Identity.DefaultError };
}
