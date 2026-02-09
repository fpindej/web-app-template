using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.ChangePassword;

/// <summary>
/// Represents a request to change the current user's password.
/// </summary>
[UsedImplicitly]
public class ChangePasswordRequest
{
    /// <summary>
    /// The user's current password for verification.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string CurrentPassword { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The new password to set for the account.
    /// </summary>
    [Required]
    [MinLength(6)]
    [MaxLength(255)]
    public string NewPassword { get; [UsedImplicitly] init; } = string.Empty;
}
