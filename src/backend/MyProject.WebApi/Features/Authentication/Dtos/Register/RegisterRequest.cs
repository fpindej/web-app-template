using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

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
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The password for the new account.
    /// </summary>
    [Required]
    [MinLength(6)]
    [MaxLength(255)]
    public string Password { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The phone number for the new account.
    /// </summary>
    [MaxLength(20)]
    [RegularExpression(@"^(\+\d{1,3})? ?\d{6,14}$",
        ErrorMessage = "Phone number must be a valid format (e.g. +420123456789)")]
    public string? PhoneNumber { get; [UsedImplicitly] init; }

    /// <summary>
    /// The first name of the user.
    /// </summary>
    [MaxLength(255)]
    public string? FirstName { get; [UsedImplicitly] init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [MaxLength(255)]
    public string? LastName { get; [UsedImplicitly] init; }
}
