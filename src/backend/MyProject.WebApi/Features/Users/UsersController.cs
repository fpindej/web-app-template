using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Identity;
using MyProject.WebApi.Features.Users.Dtos;

namespace MyProject.WebApi.Features.Users;

/// <summary>
/// Controller for managing user profiles and information.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userResult = await userService.GetCurrentUserAsync();

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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> UpdateCurrentUser([FromBody] UpdateUserRequest request)
    {
        var result = await userService.UpdateProfileAsync(request.ToInput());

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value!.ToResponse());
    }
}
