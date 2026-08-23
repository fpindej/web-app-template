using MyProject.Application.Features.Admin.Dtos;
using MyProject.Shared;
using MyProject.WebApi.Features.Admin.Dtos;
using MyProject.WebApi.Features.Admin.Dtos.AssignRole;
using MyProject.WebApi.Features.Admin.Dtos.CreateRole;
using MyProject.WebApi.Features.Admin.Dtos.CreateUser;
using MyProject.WebApi.Features.Admin.Dtos.ListUsers;
using MyProject.WebApi.Features.Admin.Dtos.SetPermissions;
using MyProject.WebApi.Features.Admin.Dtos.UpdateRole;

namespace MyProject.WebApi.Features.Admin;

/// <summary>
/// Maps between admin Application layer DTOs and WebApi request/response DTOs.
/// </summary>
internal static class AdminMapper
{
    /// <summary>
    /// Maps an <see cref="AdminUserOutput"/> to an <see cref="AdminUserResponse"/>.
    /// </summary>
    public static AdminUserResponse ToResponse(this AdminUserOutput output) => new()
    {
        Id = output.Id,
        Username = output.UserName,
        Email = output.Email,
        FirstName = output.FirstName,
        LastName = output.LastName,
        PhoneNumber = output.PhoneNumber,
        Bio = output.Bio,
        HasAvatar = output.HasAvatar,
        Roles = output.Roles,
        EmailConfirmed = output.EmailConfirmed,
        LockoutEnabled = output.LockoutEnabled,
        LockoutEnd = output.LockoutEnd,
        AccessFailedCount = output.AccessFailedCount,
        IsLockedOut = output.IsLockedOut,
        TwoFactorEnabled = output.IsTwoFactorEnabled
    };

    /// <summary>
    /// Maps an <see cref="AdminUserListOutput"/> to a <see cref="ListUsersResponse"/>.
    /// </summary>
    public static ListUsersResponse ToResponse(this AdminUserListOutput output) => new()
    {
        Items = output.Users.Select(u => u.ToResponse()).ToList(),
        TotalCount = output.TotalCount,
        PageNumber = output.PageNumber,
        PageSize = output.PageSize
    };

    /// <summary>
    /// Maps an <see cref="AdminRoleOutput"/> to an <see cref="AdminRoleResponse"/>.
    /// </summary>
    public static AdminRoleResponse ToResponse(this AdminRoleOutput output) => new()
    {
        Id = output.Id,
        Name = output.Name,
        Description = output.Description,
        IsSystem = output.IsSystem,
        UserCount = output.UserCount,
        Permissions = output.Permissions,
        Rank = output.Rank,
        GrantsAllPermissions = output.GrantsAllPermissions
    };

    /// <summary>
    /// Maps an <see cref="AssignRoleRequest"/> to an <see cref="AssignRoleInput"/>.
    /// </summary>
    public static AssignRoleInput ToInput(this AssignRoleRequest request) => new(request.Role);

    /// <summary>
    /// Maps a <see cref="CreateRoleRequest"/> to a <see cref="CreateRoleInput"/>.
    /// </summary>
    public static CreateRoleInput ToInput(this CreateRoleRequest request) => new(request.Name, request.Description);

    /// <summary>
    /// Maps an <see cref="UpdateRoleRequest"/> to an <see cref="UpdateRoleInput"/>.
    /// </summary>
    public static UpdateRoleInput ToInput(this UpdateRoleRequest request) => new(request.Name, request.Description);

    /// <summary>
    /// Maps a <see cref="CreateUserRequest"/> to a <see cref="CreateUserInput"/>.
    /// </summary>
    public static CreateUserInput ToInput(this CreateUserRequest request) => new(request.Email, request.FirstName, request.LastName);

    /// <summary>
    /// Maps a <see cref="SetPermissionsRequest"/> to a <see cref="SetRolePermissionsInput"/>.
    /// </summary>
    public static SetRolePermissionsInput ToInput(this SetPermissionsRequest request) => new(request.Permissions);

    /// <summary>
    /// Maps a <see cref="RoleDetailOutput"/> to a <see cref="RoleDetailResponse"/>.
    /// </summary>
    public static RoleDetailResponse ToResponse(this RoleDetailOutput output) => new()
    {
        Id = output.Id,
        Name = output.Name,
        Description = output.Description,
        IsSystem = output.IsSystem,
        Permissions = output.Permissions,
        UserCount = output.UserCount,
        Rank = output.Rank,
        GrantsAllPermissions = output.GrantsAllPermissions
    };

    /// <summary>
    /// Maps a <see cref="PermissionGroupOutput"/> to a <see cref="PermissionGroupResponse"/>.
    /// </summary>
    public static PermissionGroupResponse ToResponse(this PermissionGroupOutput output) => new()
    {
        Category = output.Category,
        Permissions = output.Permissions
    };

    /// <summary>
    /// Returns a copy of the response with email, username, and phone number masked.
    /// </summary>
    public static AdminUserResponse WithMaskedPii(this AdminUserResponse response) => new()
    {
        Id = response.Id,
        // Username is always identical to Email in this system — mask with the same strategy.
        Username = PiiMasker.MaskEmail(response.Username),
        Email = PiiMasker.MaskEmail(response.Email),
        FirstName = response.FirstName,
        LastName = response.LastName,
        PhoneNumber = PiiMasker.MaskPhone(response.PhoneNumber),
        Bio = response.Bio,
        HasAvatar = response.HasAvatar,
        Roles = response.Roles,
        EmailConfirmed = response.EmailConfirmed,
        LockoutEnabled = response.LockoutEnabled,
        LockoutEnd = response.LockoutEnd,
        AccessFailedCount = response.AccessFailedCount,
        IsLockedOut = response.IsLockedOut,
        TwoFactorEnabled = response.TwoFactorEnabled
    };

    /// <summary>
    /// Returns a copy of the list response with PII masked for all users except the caller.
    /// </summary>
    /// <param name="response">The paginated user list.</param>
    /// <param name="callerUserId">The current user's ID — their own entry is never masked.</param>
    public static ListUsersResponse WithMaskedPii(this ListUsersResponse response, Guid callerUserId) => new()
    {
        Items = response.Items
            .Select(u => u.Id == callerUserId ? u : u.WithMaskedPii())
            .ToList(),
        TotalCount = response.TotalCount,
        PageNumber = response.PageNumber,
        PageSize = response.PageSize
    };
}
