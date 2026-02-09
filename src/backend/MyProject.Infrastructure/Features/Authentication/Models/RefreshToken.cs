namespace MyProject.Infrastructure.Features.Authentication.Models;

/// <summary>
/// Represents a refresh token stored in the database for JWT token rotation.
/// Tokens are SHA-256 hashed before storage.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 hash of the refresh token value.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when the token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the token expires.
    /// </summary>
    public DateTime ExpiredAt { get; set; }

    /// <summary>
    /// Gets or sets whether the token has been consumed in a refresh operation.
    /// </summary>
    public bool Used { get; set; }

    /// <summary>
    /// Gets or sets whether the token has been revoked (e.g., on logout or reuse detection).
    /// </summary>
    public bool Invalidated { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns this token.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the owning user.
    /// </summary>
    public ApplicationUser? User { get; set; }
}
