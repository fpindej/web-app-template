using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using MyProject.Application.Errors;

namespace MyProject.WebApi.Features.Authentication.Dtos.Register;

/// <summary>
/// Represents a request to register a new user account.
/// </summary>
[UsedImplicitly]
public class RegisterRequest
{
    /// <summary>
    /// The email address for the new account.
    /// </summary>
    [Required(ErrorMessage = ErrorCodes.Validation.Required)]
    [EmailAddress(ErrorMessage = ErrorCodes.Validation.InvalidEmail)]
    [MaxLength(255, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    public string Email { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The password for the new account.
    /// </summary>
    [Required(ErrorMessage = ErrorCodes.Validation.Required)]
    [MinLength(6, ErrorMessage = ErrorCodes.Validation.MinLength)]
    [MaxLength(255, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    public string Password { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The phone number for the new account.
    /// </summary>
    [MaxLength(20, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    [RegularExpression(@"^(\+\d{1,3})? ?\d{6,14}$",
        ErrorMessage = ErrorCodes.Validation.InvalidPhoneNumber)]
    public string? PhoneNumber { get; [UsedImplicitly] init; }

    /// <summary>
    /// The first name of the user.
    /// </summary>
    [MaxLength(255, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    public string? FirstName { get; [UsedImplicitly] init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [MaxLength(255, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    public string? LastName { get; [UsedImplicitly] init; }
}
