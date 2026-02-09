namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for changing the current user's password.
/// </summary>
/// <param name="CurrentPassword">The user's current password for verification.</param>
/// <param name="NewPassword">The new password to set.</param>
public record ChangePasswordInput(
    string CurrentPassword,
    string NewPassword
);
