using System.ComponentModel;
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
    [Description("The email address for the new account")]
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// The password for the new account.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    [MinLength(6)]
    [Description("The password for the new account, must be at least 6 characters")]
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// The phone number for the new account.
    /// </summary>
    [RegularExpression(@"^(\+\d{1,3})? ?\d{6,14}$",
        ErrorMessage = "Phone number must be a valid European format (e.g. +420123456789)")]
    [Description("The phone number for the new account (optional), must be a valid European format")]
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// The first name of the user.
    /// </summary>
    [MaxLength(255)]
    [Description("The first name of the user (optional), maximum 255 characters")]
    public string? FirstName { get; init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [MaxLength(255)]
    [Description("The last name of the user (optional), maximum 255 characters")]
    public string? LastName { get; init; }
}
