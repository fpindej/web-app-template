using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.Login;

/// <summary>
/// Represents a user login request with credentials.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// The username for authentication.
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Username { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The password for authentication.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string Password { get; [UsedImplicitly] init; } = string.Empty;
}
