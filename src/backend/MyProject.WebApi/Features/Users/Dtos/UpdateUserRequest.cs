using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using MyProject.Application.Errors;
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
    [MaxLength(255, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    public string? FirstName { get; [UsedImplicitly] init; }

    /// <summary>
    /// The last name of the user.
    /// </summary>
    [MaxLength(255, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    public string? LastName { get; [UsedImplicitly] init; }

    /// <summary>
    /// The phone number of the user.
    /// </summary>
    [MaxLength(20, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    [RegularExpression(@"^(\+\d{1,3})? ?\d{6,14}$",
        ErrorMessage = ErrorCodes.Validation.InvalidPhoneNumber)]
    public string? PhoneNumber { get; [UsedImplicitly] init; }

    /// <summary>
    /// A short biography or description of the user.
    /// </summary>
    [MaxLength(1000, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    public string? Bio { get; [UsedImplicitly] init; }

    /// <summary>
    /// The URL to the user's avatar image.
    /// </summary>
    [MaxLength(500, ErrorMessage = ErrorCodes.Validation.MaxLength)]
    [Url(ErrorMessage = ErrorCodes.Validation.InvalidUrl)]
    public string? AvatarUrl { get; [UsedImplicitly] init; }

    /// <summary>
    /// Converts the request to an application layer input.
    /// </summary>
    public UpdateProfileInput ToInput() => new(FirstName, LastName, PhoneNumber, Bio, AvatarUrl);
}
