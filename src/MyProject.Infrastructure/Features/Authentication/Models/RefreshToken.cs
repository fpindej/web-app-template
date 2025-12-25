namespace MyProject.Infrastructure.Features.Authentication.Models;

public class RefreshToken
{
    public Guid Id { get; set; }

    public string Token { get; set; } = string.Empty;

    public string JwtId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiredAt { get; set; }

    public bool Used { get; set; }

    public bool Invalidated { get; set; }

    public Guid UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
