using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using MyProject.Application.Errors;

namespace MyProject.WebApi.Features.Authentication.Dtos.Login;

/// <summary>
/// Represents a user login request with credentials.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// The username for authentication.
    /// </summary>
    [Required(ErrorMessage = ErrorCodes.Validation.Required)]
    [EmailAddress(ErrorMessage = ErrorCodes.Validation.InvalidEmail)]
    [MaxLength(255, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    public string Username { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The password for authentication.
    /// </summary>
    [Required(ErrorMessage = ErrorCodes.Validation.Required)]
    [MaxLength(255, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    public string Password { get; [UsedImplicitly] init; } = string.Empty;
}
