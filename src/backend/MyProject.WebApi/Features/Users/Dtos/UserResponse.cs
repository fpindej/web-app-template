using System.ComponentModel;
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
    [Description("The unique identifier of the user")]
    public Guid Id { get; [UsedImplicitly] init; }

    /// <summary>
    /// The username of the user (same as email).
    /// </summary>
    [Description("The username of the user (same as email)")]
    public string Username { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The email address of the user (same as username).
    /// </summary>
    [Description("The email address of the user (same as username)")]
    public string Email { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The first name of the user.
    /// </summary>
    [Description("The first name of the user")]
    public string? FirstName { get; [UsedImplicitly] init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [Description("The last name of the user")]
    public string? LastName { get; [UsedImplicitly] init; }

    /// <summary>
    /// The phone number of the user.
    /// </summary>
    [Description("The phone number of the user")]
    public string? PhoneNumber { get; [UsedImplicitly] init; }

    /// <summary>
    /// A short biography or description of the user.
    /// </summary>
    [Description("A short biography or description of the user")]
    public string? Bio { get; [UsedImplicitly] init; }

    /// <summary>
    /// The URL to the user's avatar image.
    /// </summary>
    [Description("The URL to the user's avatar image")]
    public string? AvatarUrl { get; [UsedImplicitly] init; }

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    [Description("The roles assigned to the user")]
    public IEnumerable<string> Roles { get; [UsedImplicitly] init; } = [];
}
