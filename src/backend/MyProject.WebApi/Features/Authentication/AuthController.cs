using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Features.Authentication;
using MyProject.Application.Identity;
using MyProject.Infrastructure.Features.Authentication.Constants;
using MyProject.WebApi.Features.Authentication.Dtos.Login;
using MyProject.WebApi.Features.Authentication.Dtos.Me;

using MyProject.WebApi.Features.Authentication.Dtos.Register;

namespace MyProject.WebApi.Features.Authentication;

/// <summary>
/// Controller for managing user accounts, including login, registration, and token management using HttpOnly cookies.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthenticationService authenticationService,
    IUserService userService) : ControllerBase
{
    /// <summary>
    /// Authenticates a user and returns a http-only cookie with the JWT access token and a refresh token
    /// </summary>
    /// <param name="request">The login credentials</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Authentication response (access token and refresh token set in HttpOnly cookies)</returns>
    /// <response code="200">Returns success response (access token and refresh token set in HttpOnly cookies)</response>
    /// <response code="400">If the credentials are invalid or improperly formatted</response>
    /// <response code="401">If the credentials are invalid</response>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.Login(request.Username, request.Password, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(result.Error);
        }

        return Ok();
    }

    /// <summary>
    /// Refreshes the authentication tokens using the refresh token from cookies
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>Success response with refreshed tokens set in HttpOnly cookies</returns>
    /// <response code="200">Returns success response with refreshed tokens set in HttpOnly cookies</response>
    /// <response code="401">If the refresh token is invalid, expired, or missing</response>
    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(CookieNames.RefreshToken, out var refreshToken))
        {
            return Unauthorized(new { message = "Refresh token is missing." });
        }

        var result = await authenticationService.RefreshTokenAsync(refreshToken, cancellationToken);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Error });
        }

        return Ok();
    }

    /// <summary>
    /// Logs out the current user by clearing authentication cookies
    /// </summary>
    /// <returns>A 204 No Content response</returns>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await authenticationService.Logout();
        return NoContent();
    }

    /// <summary>
    /// Gets the current authenticated user's information
    /// </summary>
    /// <returns>User information if authenticated</returns>
    /// <response code="200">Returns user information</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var userResult = await userService.GetCurrentUserAsync();

        if (!userResult.IsSuccess)
        {
            return Unauthorized();
        }

        var user = userResult.Value!;
        var roles = await userService.GetUserRolesAsync(user.Id);

        return Ok(new MeResponse
        {
            Id = user.Id,
            Username = user.UserName,
            Roles = roles
        });
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.Register(request.ToRegisterInput());

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(Me), new { id = result.Value });
    }
}
