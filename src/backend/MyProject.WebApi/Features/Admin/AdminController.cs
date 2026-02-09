using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Features.Admin;
using MyProject.Application.Identity.Constants;
using MyProject.WebApi.Features.Admin.Dtos;
using MyProject.WebApi.Features.Admin.Dtos.AssignRole;
using MyProject.WebApi.Features.Admin.Dtos.ListUsers;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Admin;

/// <summary>
/// Administrative endpoints for managing users and roles.
/// Requires the Admin or SuperAdmin role. Role hierarchy and self-action protection
/// are enforced at the service layer.
/// </summary>
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.SuperAdmin}")]
public class AdminController(IAdminService adminService) : ApiController
{
    /// <summary>
    /// Gets a paginated list of all users, optionally filtered by a search term.
    /// </summary>
    /// <param name="request">Pagination and search parameters</param>
    /// <returns>A paginated list of users with admin-level details</returns>
    /// <response code="200">Returns the paginated user list</response>
    /// <response code="400">If the pagination parameters are invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the Admin or SuperAdmin role</response>
    [HttpGet("users")]
    [ProducesResponseType(typeof(ListUsersResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ListUsersResponse>> ListUsers(
        [FromQuery] ListUsersRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.GetUsersAsync(
            request.PageNumber, request.PageSize, request.Search, cancellationToken);

        return Ok(result.ToResponse());
    }

    /// <summary>
    /// Gets a single user by ID with full admin-level details.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>The user's admin-level details</returns>
    /// <response code="200">Returns the user details</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the Admin or SuperAdmin role</response>
    /// <response code="404">If the user was not found</response>
    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserResponse>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var output = await adminService.GetUserByIdAsync(id, cancellationToken);

        return Ok(output.ToResponse());
    }

    /// <summary>
    /// Assigns a role to a user. The caller must outrank the target user
    /// and can only assign roles below their own rank.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="request">The role to assign</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Role assigned successfully</response>
    /// <response code="400">If the role is invalid, the user already has it, or hierarchy check fails</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the Admin or SuperAdmin role</response>
    /// <response code="404">If the user was not found</response>
    [HttpPost("users/{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AssignRole(
        Guid id,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        var result = await adminService.AssignRoleAsync(callerUserId, id, request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse { Message = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Removes a role from a user. The caller must outrank the target user
    /// and cannot remove roles at or above their own rank.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <param name="role">The role name to remove</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Role removed successfully</response>
    /// <response code="400">If the role is invalid, the user doesn't have it, or hierarchy check fails</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the Admin or SuperAdmin role</response>
    /// <response code="404">If the user was not found</response>
    [HttpDelete("users/{id:guid}/roles/{role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveRole(Guid id, string role, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        var result = await adminService.RemoveRoleAsync(callerUserId, id, role, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse { Message = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Locks a user account, preventing login. The caller must outrank the target user
    /// and cannot lock themselves.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">User locked successfully</response>
    /// <response code="400">If the lock operation failed or hierarchy check fails</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the Admin or SuperAdmin role</response>
    /// <response code="404">If the user was not found</response>
    [HttpPost("users/{id:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> LockUser(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        var result = await adminService.LockUserAsync(callerUserId, id, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse { Message = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Unlocks a user account, allowing login. The caller must outrank the target user.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">User unlocked successfully</response>
    /// <response code="400">If the unlock operation failed or hierarchy check fails</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the Admin or SuperAdmin role</response>
    /// <response code="404">If the user was not found</response>
    [HttpPost("users/{id:guid}/unlock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UnlockUser(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        var result = await adminService.UnlockUserAsync(callerUserId, id, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse { Message = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Permanently deletes a user account. The caller must outrank the target user
    /// and cannot delete themselves or the last user with an administrative role.
    /// </summary>
    /// <param name="id">The user ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">User deleted successfully</response>
    /// <response code="400">If the delete operation failed or hierarchy check fails</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the Admin or SuperAdmin role</response>
    /// <response code="404">If the user was not found</response>
    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = GetCurrentUserId();
        var result = await adminService.DeleteUserAsync(callerUserId, id, cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(new ErrorResponse { Message = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Gets all roles with user counts.
    /// </summary>
    /// <returns>A list of roles with the number of users in each</returns>
    /// <response code="200">Returns the list of roles</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the Admin or SuperAdmin role</response>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminRoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AdminRoleResponse>>> ListRoles(
        CancellationToken cancellationToken)
    {
        var roles = await adminService.GetRolesAsync(cancellationToken);

        return Ok(roles.Select(r => r.ToResponse()).ToList());
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("Unable to determine the current user identity.");
        }

        return userId;
    }
}
