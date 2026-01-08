using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Users.Dtos;

/// <summary>
/// Represents the current user's information.
/// </summary>
public class UserResponse
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    public Guid Id { [UsedImplicitly] get; [UsedImplicitly] init; }

    /// <summary>
    /// The username of the user (same as email).
    /// </summary>
    public string Username { [UsedImplicitly] get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The email address of the user (same as username).
    /// </summary>
    public string Email { [UsedImplicitly] get; init; } = string.Empty;

    /// <summary>
    /// The first name of the user.
    /// </summary>
    public string? FirstName { [UsedImplicitly] get; init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    public string? LastName { [UsedImplicitly] get; init; }

    /// <summary>
    /// The phone number of the user.
    /// </summary>
    public string? PhoneNumber { [UsedImplicitly] get; init; }

    /// <summary>
    /// A short biography or description of the user.
    /// </summary>
    public string? Bio { [UsedImplicitly] get; init; }

    /// <summary>
    /// The URL to the user's avatar image.
    /// </summary>
    public string? AvatarUrl { [UsedImplicitly] get; init; }

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    public IEnumerable<string> Roles { [UsedImplicitly] get; init; } = [];
}
