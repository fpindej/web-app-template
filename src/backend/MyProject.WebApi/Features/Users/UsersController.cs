using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyProject.Application.Identity;
using MyProject.WebApi.Features.Users.Dtos;
using MyProject.WebApi.Features.Users.Dtos.DeleteAccount;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Users;

/// <summary>
/// Controller for managing user profiles and information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Users")]
public class UsersController(IUserService userService) : ControllerBase
{
    /// <summary>
    /// Gets the current authenticated user's information
    /// </summary>
    /// <returns>User information if authenticated</returns>
    /// <response code="200">Returns user information</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userResult = await userService.GetCurrentUserAsync(cancellationToken);

        if (!userResult.IsSuccess)
        {
            return Unauthorized();
        }

        return Ok(userResult.Value!.ToResponse());
    }

    /// <summary>
    /// Updates the current authenticated user's profile information
    /// </summary>
    /// <param name="request">The profile update request</param>
    /// <returns>Updated user information</returns>
    /// <response code="200">Returns updated user information</response>
    /// <response code="400">If the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> UpdateCurrentUser(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.UpdateProfileAsync(request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse { Message = result.Error });
        }

        return Ok(result.Value!.ToResponse());
    }

    /// <summary>
    /// Permanently deletes the current authenticated user's account.
    /// Requires password confirmation. Revokes all tokens and clears auth cookies.
    /// </summary>
    /// <param name="request">The account deletion request containing the user's password</param>
    /// <response code="204">Account successfully deleted</response>
    /// <response code="400">If the password is incorrect or the request is invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    [HttpDelete("me")]
    [EnableRateLimiting(RateLimitPolicies.Sensitive)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> DeleteAccount(
        [FromBody] DeleteAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.DeleteAccountAsync(request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse { Message = result.Error });
        }

        return NoContent();
    }
}
