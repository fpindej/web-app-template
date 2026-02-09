namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Input for updating the current user's profile information.
/// </summary>
/// <param name="FirstName">The updated first name, or <c>null</c> to leave unchanged.</param>
/// <param name="LastName">The updated last name, or <c>null</c> to leave unchanged.</param>
/// <param name="PhoneNumber">The updated phone number, or <c>null</c> to leave unchanged.</param>
/// <param name="Bio">The updated biography text, or <c>null</c> to leave unchanged.</param>
/// <param name="AvatarUrl">The updated avatar URL, or <c>null</c> to leave unchanged.</param>
public record UpdateProfileInput(
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Bio,
    string? AvatarUrl
);
