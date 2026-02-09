using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Extended Identity user with additional profile fields.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    [MaxLength(255)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    [MaxLength(255)]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the user's biography text.
    /// </summary>
    [MaxLength(1000)]
    public string? Bio { get; set; }

    /// <summary>
    /// Gets or sets the URL to the user's avatar image.
    /// </summary>
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}
