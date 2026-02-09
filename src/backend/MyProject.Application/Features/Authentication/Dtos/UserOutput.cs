namespace MyProject.Application.Features.Authentication.Dtos;

/// <summary>
/// Output representing a user's profile information.
/// </summary>
/// <param name="Id">The user's unique identifier.</param>
/// <param name="UserName">The user's username (same as email in this system).</param>
/// <param name="FirstName">The user's first name, or <c>null</c> if not set.</param>
/// <param name="LastName">The user's last name, or <c>null</c> if not set.</param>
/// <param name="PhoneNumber">The user's phone number, or <c>null</c> if not set.</param>
/// <param name="Bio">The user's biography text, or <c>null</c> if not set.</param>
/// <param name="AvatarUrl">The user's avatar URL, or <c>null</c> if not set.</param>
/// <param name="Roles">The roles assigned to the user.</param>
public record UserOutput(
    Guid Id,
    string UserName,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Bio,
    string? AvatarUrl,
    IEnumerable<string> Roles
)
{
    /// <summary>
    /// Email is derived from UserName (they are the same value in this system).
    /// </summary>
    public string Email => UserName;
}
