using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using MyProject.Application.Features.Authentication.Dtos;

namespace MyProject.WebApi.Features.Users.Dtos;

/// <summary>
/// Represents a request to update the user's profile information.
/// </summary>
[UsedImplicitly]
public class UpdateUserRequest
{
    /// <summary>
    /// The first name of the user.
    /// </summary>
    [MaxLength(255)]
    public string? FirstName { get; [UsedImplicitly] init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [MaxLength(255)]
    public string? LastName { get; [UsedImplicitly] init; }

    /// <summary>
    /// The phone number of the user.
    /// </summary>
    [MaxLength(20)]
    [RegularExpression(@"^(\+\d{1,3})? ?\d{6,14}$",
        ErrorMessage = "Phone number must be a valid format (e.g. +420123456789)")]
    public string? PhoneNumber { get; [UsedImplicitly] init; }

    /// <summary>
    /// A short biography or description of the user.
    /// </summary>
    [MaxLength(1000)]
    public string? Bio { get; [UsedImplicitly] init; }

    /// <summary>
    /// The URL to the user's avatar image.
    /// </summary>
    [MaxLength(500)]
    [Url]
    public string? AvatarUrl { get; [UsedImplicitly] init; }

    /// <summary>
    /// Converts the request to an application layer input.
    /// </summary>
    public UpdateProfileInput ToInput() => new(FirstName, LastName, PhoneNumber, Bio, AvatarUrl);
}
