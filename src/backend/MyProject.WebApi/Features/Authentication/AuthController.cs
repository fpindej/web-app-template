using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Features.Authentication;
using MyProject.Infrastructure.Features.Authentication.Constants;
using MyProject.WebApi.Features.Authentication.Dtos.Login;
using MyProject.WebApi.Features.Authentication.Dtos.Register;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Authentication;

/// <summary>
/// Controller for authentication operations including login, registration, and token management.
/// Supports both cookie-based (web) and Bearer token (mobile/API) authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and returns JWT tokens.
    /// Tokens are returned both in the response body (for mobile/API clients) and as HttpOnly cookies (for web clients).
    /// </summary>
    /// <param name="request">The login credentials</param>
    /// <returns>Authentication response containing access and refresh tokens</returns>
    /// <response code="200">Returns authentication tokens (also set in HttpOnly cookies)</response>
    /// <response code="400">If the credentials are improperly formatted</response>
    /// <response code="401">If the credentials are invalid</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.Login(request.Username, request.Password, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new ErrorResponse { Message = result.Error });
        }

        return Ok(result.Value!.ToResponse());
    }

    /// <summary>
    /// Refreshes the authentication tokens using a refresh token.
    /// For web clients, the refresh token is read from cookies. For mobile/API clients, pass it in the request body.
    /// Tokens are returned both in the response body and as HttpOnly cookies.
    /// </summary>
    /// <param name="request">Optional request body containing the refresh token (for mobile/API clients)</param>
    /// <returns>Authentication response containing new access and refresh tokens</returns>
    /// <response code="200">Returns new authentication tokens (also set in HttpOnly cookies)</response>
    /// <response code="401">If the refresh token is invalid, expired, or missing</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthenticationResponse>> Refresh([FromBody] RefreshRequest? request, CancellationToken cancellationToken)
    {
        // Priority 1: Request body (mobile/API clients)
        // Priority 2: Cookie (web clients)
        var refreshToken = request?.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
        {
            Request.Cookies.TryGetValue(CookieNames.RefreshToken, out refreshToken);
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized(new ErrorResponse { Message = "Refresh token is missing." });
        }

        var result = await authenticationService.RefreshTokenAsync(refreshToken, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new ErrorResponse { Message = result.Error });
        }

        return Ok(result.Value!.ToResponse());
    }

    /// <summary>
    /// Logs out the current user by clearing authentication cookies
    /// </summary>
    /// <returns>A 204 No Content response</returns>
    /// <response code="204">Successfully logged out</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Logout(CancellationToken cancellationToken)
    {
        await authenticationService.Logout(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="request">The registration details</param>
    /// <returns>Created response with the new user's ID</returns>
    /// <response code="201">User successfully created</response>
    /// <response code="400">If the registration data is invalid</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.Register(request.ToRegisterInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse { Message = result.Error });
        }

        return Created($"/api/users/{result.Value}", new { id = result.Value });
    }
}
