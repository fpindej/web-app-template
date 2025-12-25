using System.ComponentModel;
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
    [Description("The username for authentication (email)")]
    [EmailAddress]
    [DataType(DataType.EmailAddress)]
    public string Username { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The password for authentication.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    [Description("The password for authentication")]
    public string Password { get; [UsedImplicitly] init; } = string.Empty;
}
