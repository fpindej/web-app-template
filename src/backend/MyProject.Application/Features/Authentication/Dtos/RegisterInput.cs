namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for registering a new user account.
/// </summary>
/// <param name="Email">The user's email address (also used as the username).</param>
/// <param name="Password">The password for the new account.</param>
/// <param name="FirstName">The user's first name, or <c>null</c> if not provided.</param>
/// <param name="LastName">The user's last name, or <c>null</c> if not provided.</param>
/// <param name="PhoneNumber">The user's phone number, or <c>null</c> if not provided.</param>
public record RegisterInput(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    string? PhoneNumber
);
