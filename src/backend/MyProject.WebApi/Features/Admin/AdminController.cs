using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MyProject.Application.Features.Admin;
using MyProject.Application.Identity;
using MyProject.Application.Identity.Constants;
using MyProject.Domain;
using MyProject.WebApi.Authorization;
using MyProject.WebApi.Features.Admin.Dtos;
using MyProject.WebApi.Features.Admin.Dtos.AssignRole;
using MyProject.WebApi.Features.Admin.Dtos.CreateRole;
using MyProject.WebApi.Features.Admin.Dtos.ListUsers;
using MyProject.WebApi.Features.Admin.Dtos.SetPermissions;
using MyProject.WebApi.Features.Admin.Dtos.UpdateRole;
using MyProject.WebApi.Shared;

namespace MyProject.WebApi.Features.Admin;

/// <summary>
/// Administrative endpoints for managing users and roles.
/// Individual actions are protected by permission-based authorization via <see cref="RequirePermissionAttribute"/>.
/// Role hierarchy and self-action protection are enforced at the service layer.
/// </summary>
public class AdminController(IAdminService adminService, IRoleManagementService roleManagementService, IUserContext userContext) : ApiController
{
    /// <summary>
    /// Gets a paginated list of all users, optionally filtered by a search term.
    /// </summary>
    /// <param name="request">Pagination and search parameters</param>
    /// <returns>A paginated list of users with admin-level details</returns>
    /// <response code="200">Returns the paginated user list</response>
    /// <response code="400">If the pagination parameters are invalid</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    [HttpGet("users")]
    [RequirePermission(AppPermissions.Users.View)]
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
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the user was not found</response>
    [HttpGet("users/{id:guid}")]
    [RequirePermission(AppPermissions.Users.View)]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserResponse>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminService.GetUserByIdAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return NotFound(new ErrorResponse { Message = result.Error });
        }

        return Ok(result.Value!.ToResponse());
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
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the user was not found</response>
    [HttpPost("users/{id:guid}/roles")]
    [RequirePermission(AppPermissions.Users.AssignRoles)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> AssignRole(
        Guid id,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var callerUserId = userContext.AuthenticatedUserId;
        var result = await adminService.AssignRoleAsync(callerUserId, id, request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
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
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the user was not found</response>
    [HttpDelete("users/{id:guid}/roles/{role:roleName}")]
    [RequirePermission(AppPermissions.Users.AssignRoles)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> RemoveRole(Guid id, string role, CancellationToken cancellationToken)
    {
        var callerUserId = userContext.AuthenticatedUserId;
        var result = await adminService.RemoveRoleAsync(callerUserId, id, role, cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
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
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the user was not found</response>
    [HttpPost("users/{id:guid}/lock")]
    [RequirePermission(AppPermissions.Users.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> LockUser(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = userContext.AuthenticatedUserId;
        var result = await adminService.LockUserAsync(callerUserId, id, cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
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
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the user was not found</response>
    [HttpPost("users/{id:guid}/unlock")]
    [RequirePermission(AppPermissions.Users.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> UnlockUser(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = userContext.AuthenticatedUserId;
        var result = await adminService.UnlockUserAsync(callerUserId, id, cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
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
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the user was not found</response>
    [HttpDelete("users/{id:guid}")]
    [RequirePermission(AppPermissions.Users.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = userContext.AuthenticatedUserId;
        var result = await adminService.DeleteUserAsync(callerUserId, id, cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Gets all roles with user counts.
    /// </summary>
    /// <returns>A list of roles with the number of users in each</returns>
    /// <response code="200">Returns the list of roles</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    [HttpGet("roles")]
    [RequirePermission(AppPermissions.Roles.View)]
    [ProducesResponseType(typeof(IReadOnlyList<AdminRoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AdminRoleResponse>>> ListRoles(
        CancellationToken cancellationToken)
    {
        var roles = await adminService.GetRolesAsync(cancellationToken);

        return Ok(roles.Select(r => r.ToResponse()).ToList());
    }

    /// <summary>
    /// Gets detailed information about a single role, including its permissions and user count.
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <returns>The role details with permissions</returns>
    /// <response code="200">Returns the role details</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the role was not found</response>
    [HttpGet("roles/{id:guid}")]
    [RequirePermission(AppPermissions.Roles.View)]
    [ProducesResponseType(typeof(RoleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDetailResponse>> GetRole(Guid id, CancellationToken cancellationToken)
    {
        var result = await roleManagementService.GetRoleDetailAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return Ok(result.Value!.ToResponse());
    }

    /// <summary>
    /// Creates a new custom role.
    /// </summary>
    /// <param name="request">The role name and optional description</param>
    /// <returns>The created role's ID</returns>
    /// <response code="201">Role created successfully</response>
    /// <response code="400">If the role name is taken or validation fails</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    [HttpPost("roles")]
    [RequirePermission(AppPermissions.Roles.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> CreateRole(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await roleManagementService.CreateRoleAsync(request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return CreatedAtAction(nameof(GetRole), new { id = result.Value }, null);
    }

    /// <summary>
    /// Updates an existing role's name and/or description. System roles cannot be renamed.
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <param name="request">The fields to update</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Role updated successfully</response>
    /// <response code="400">If the update is invalid (system role rename, name taken, etc.)</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the role was not found</response>
    [HttpPut("roles/{id:guid}")]
    [RequirePermission(AppPermissions.Roles.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await roleManagementService.UpdateRoleAsync(id, request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a custom role. System roles and roles with assigned users cannot be deleted.
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Role deleted successfully</response>
    /// <response code="400">If the role is a system role or has users assigned</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the role was not found</response>
    [HttpDelete("roles/{id:guid}")]
    [RequirePermission(AppPermissions.Roles.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var result = await roleManagementService.DeleteRoleAsync(id, cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Replaces all permissions on a role. Rotates security stamps for affected users.
    /// SuperAdmin permissions cannot be modified.
    /// </summary>
    /// <param name="id">The role ID</param>
    /// <param name="request">The new set of permissions</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Permissions updated successfully</response>
    /// <response code="400">If the permissions are invalid or SuperAdmin is targeted</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    /// <response code="404">If the role was not found</response>
    [HttpPut("roles/{id:guid}/permissions")]
    [RequirePermission(AppPermissions.Roles.Manage)]
    [EnableRateLimiting(RateLimitPolicies.AdminMutations)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> SetRolePermissions(
        Guid id,
        [FromBody] SetPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await roleManagementService.SetRolePermissionsAsync(id, request.ToInput(), cancellationToken);

        if (!result.IsSuccess)
        {
            return ToErrorResult(result.Error);
        }

        return NoContent();
    }

    /// <summary>
    /// Returns all available permissions grouped by category.
    /// </summary>
    /// <returns>Permission groups</returns>
    /// <response code="200">Returns the permission groups</response>
    /// <response code="401">If the user is not authenticated</response>
    /// <response code="403">If the user does not have the required permission</response>
    [HttpGet("permissions")]
    [RequirePermission(AppPermissions.Roles.View)]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionGroupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IReadOnlyList<PermissionGroupResponse>> GetAllPermissions()
    {
        var permissions = roleManagementService.GetAllPermissions();
        return Ok(permissions.Select(p => p.ToResponse()).ToList());
    }

    /// <summary>
    /// Returns <see cref="NotFoundObjectResult"/> for not-found errors, <see cref="BadRequestObjectResult"/> otherwise.
    /// </summary>
    private ActionResult ToErrorResult(string? error)
    {
        if (error is ErrorMessages.Admin.UserNotFound or ErrorMessages.Roles.RoleNotFound)
        {
            return NotFound(new ErrorResponse { Message = error });
        }

        return BadRequest(new ErrorResponse { Message = error });
    }

}
