using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.Login;

/// <summary>
/// Request to refresh authentication tokens using a refresh token.
/// For web clients using cookies, this request body is optional.
/// For mobile/API clients, the refresh token must be provided in the request body.
/// </summary>
public class RefreshRequest
{
    /// <summary>
    /// The refresh token obtained from a previous login or refresh response.
    /// Required for mobile/API clients. Optional for web clients (will fall back to cookie).
    /// </summary>
    public string? RefreshToken { [UsedImplicitly] get; [UsedImplicitly] init; }
}
