using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.Extensions.Caching.Hybrid;
using MyProject.Application.Features.Admin.Dtos;
using MyProject.Application.Features.Audit;
using MyProject.Application.Features.Email;
using MyProject.Application.Features.Email.Models;
using MyProject.Application.Features.FileStorage;
using MyProject.Application.Identity.Constants;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Features.Admin.Services;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Features.Email.Options;
using MyProject.Infrastructure.Persistence;
using MyProject.Shared;

namespace MyProject.Component.Tests.Services;

public class AdminServiceTests : IDisposable
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly HybridCache _hybridCache;
    private readonly ITemplatedEmailSender _templatedEmailSender;
    private readonly IAuditService _auditService;
    private readonly IFileStorageService _fileStorageService;
    private readonly FakeTimeProvider _timeProvider;
    private readonly MyProjectDbContext _dbContext;
    private readonly AdminService _sut;

    private readonly Guid _callerId = Guid.NewGuid();
    private readonly Guid _targetId = Guid.NewGuid();

    public AdminServiceTests()
    {
        _userManager = IdentityMockHelpers.CreateMockUserManager();
        _roleManager = IdentityMockHelpers.CreateMockRoleManager();
        _hybridCache = Substitute.For<HybridCache>();
        _templatedEmailSender = Substitute.For<ITemplatedEmailSender>();
        _timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 15, 12, 0, 0, TimeSpan.Zero));
        _dbContext = TestDbContextFactory.Create();
        var logger = Substitute.For<ILogger<AdminService>>();
        var authOptions = Options.Create(new AuthenticationOptions
        {
            Jwt = new AuthenticationOptions.JwtOptions
            {
                Key = "ThisIsATestSigningKeyThatIsAtLeast64CharactersLongForTestFixtures!",
                Issuer = "test-issuer",
                Audience = "test-audience"
            }
        });
        var emailOptions = Options.Create(new EmailOptions { FrontendBaseUrl = "https://example.com" });
        var emailTokenService = new EmailTokenService(_dbContext, _timeProvider, authOptions);
        _auditService = Substitute.For<IAuditService>();

        _fileStorageService = Substitute.For<IFileStorageService>();

        _sut = new AdminService(
            _userManager, _roleManager, _dbContext, _hybridCache, _timeProvider,
            _templatedEmailSender, emailTokenService, _auditService,
            new PermissionEscalationGuard(_dbContext),
            _fileStorageService, authOptions, emailOptions, logger);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _userManager.Dispose();
    }

    /// <summary>
    /// Mocks the caller as an Admin and seeds the Admin role assignment (with the given
    /// permission claims) into the database so rank and escalation checks can resolve it.
    /// </summary>
    private ApplicationUser SetupCallerAsAdmin(params string[] heldPermissions)
    {
        var caller = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Admin });
        TestRoles.SeedAssigned(_dbContext, _callerId, AppRoles.Admin, heldPermissions);
        return caller;
    }

    private ApplicationUser SetupTargetAsUser(bool emailConfirmed = true)
    {
        var target = new ApplicationUser { Id = _targetId, UserName = "user@test.com", EmailConfirmed = emailConfirmed };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.User });
        TestRoles.Seed(_dbContext, AppRoles.User);
        return target;
    }

    #region AssignRole

    [Fact]
    public async Task AssignRole_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("User").Returns(TestRoles.Create(AppRoles.User));
        _userManager.IsInRoleAsync(target, "User").Returns(false);
        _userManager.AddToRoleAsync(target, "User").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsSuccess);
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminAssignRole,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: _targetId,
            metadata: Arg.Is<string>(m => m.Contains("User")),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AssignRole_RoleDoesNotExist_ReturnsFailure()
    {
        _roleManager.FindByNameAsync("NonExistent").Returns((ApplicationRole?)null);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("NonExistent"));

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task AssignRole_UserNotFound_ReturnsNotFound()
    {
        _roleManager.FindByNameAsync("User").Returns(TestRoles.Create(AppRoles.User));
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task AssignRole_HigherRoleThanCaller_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        SetupTargetAsUser();
        _roleManager.FindByNameAsync("Admin").Returns(TestRoles.Create(AppRoles.Admin));

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("Admin"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleAssignAboveRank, result.Error);
    }

    [Fact]
    public async Task AssignRole_UserAlreadyHasRole_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("User").Returns(TestRoles.Create(AppRoles.User));
        _userManager.IsInRoleAsync(target, "User").Returns(true);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleAlreadyAssigned, result.Error);
    }

    [Fact]
    public async Task AssignRole_SystemRole_UnverifiedEmail_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = new ApplicationUser { Id = _targetId, UserName = "user@test.com", EmailConfirmed = false };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string>());
        _roleManager.FindByNameAsync("User").Returns(TestRoles.Create(AppRoles.User));
        _userManager.IsInRoleAsync(target, "User").Returns(false);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.EmailVerificationRequired, result.Error);
    }

    [Fact]
    public async Task AssignRole_CustomRole_UnverifiedEmail_Succeeds()
    {
        SetupCallerAsAdmin();
        var target = new ApplicationUser { Id = _targetId, UserName = "user@test.com", EmailConfirmed = false };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string>());
        _roleManager.FindByNameAsync("CustomRole").Returns(new ApplicationRole { Name = "CustomRole" });
        _userManager.IsInRoleAsync(target, "CustomRole").Returns(false);
        _userManager.AddToRoleAsync(target, "CustomRole").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("CustomRole"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AssignRole_CustomRoleWithUnheldPermissions_ReturnsForbidden()
    {
        SetupCallerAsAdmin(AppPermissions.Users.View);
        SetupTargetAsUser();

        var customRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "PrivilegedRole" };
        _roleManager.FindByNameAsync("PrivilegedRole").Returns(customRole);
        _roleManager.GetClaimsAsync(customRole).Returns(
            [new Claim(AppPermissions.ClaimType, AppPermissions.Roles.Manage)]);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("PrivilegedRole"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleAssignEscalation, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task AssignRole_CustomRoleWithHeldPermissions_Succeeds()
    {
        SetupCallerAsAdmin(AppPermissions.Users.View);
        var target = SetupTargetAsUser();

        var customRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "ViewerRole" };
        _roleManager.FindByNameAsync("ViewerRole").Returns(customRole);
        _roleManager.GetClaimsAsync(customRole).Returns(
            [new Claim(AppPermissions.ClaimType, AppPermissions.Users.View)]);

        _userManager.IsInRoleAsync(target, "ViewerRole").Returns(false);
        _userManager.AddToRoleAsync(target, "ViewerRole").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("ViewerRole"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AssignRole_MultipleCallerRoles_AggregatesPermissions()
    {
        // Caller has Admin (with users.view) + a custom "Ops" role (with roles.manage).
        // Target custom role requires both - should succeed via union of caller permissions.
        var caller = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Admin, "Ops" });
        TestRoles.SeedAssigned(_dbContext, _callerId, AppRoles.Admin, AppPermissions.Users.View);
        TestRoles.SeedAssigned(_dbContext, _callerId, "Ops", AppPermissions.Roles.Manage);

        var target = SetupTargetAsUser();

        var customRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "PowerRole" };
        _roleManager.FindByNameAsync("PowerRole").Returns(customRole);
        _roleManager.GetClaimsAsync(customRole).Returns(
        [
            new Claim(AppPermissions.ClaimType, AppPermissions.Users.View),
            new Claim(AppPermissions.ClaimType, AppPermissions.Roles.Manage)
        ]);

        _userManager.IsInRoleAsync(target, "PowerRole").Returns(false);
        _userManager.AddToRoleAsync(target, "PowerRole").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("PowerRole"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AssignRole_Superuser_CanAssignAnyCustomRole()
    {
        var caller = new ApplicationUser { Id = _callerId, UserName = "superuser@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Superuser });
        TestRoles.SeedAssigned(_dbContext, _callerId, AppRoles.Superuser);

        var target = SetupTargetAsUser();

        var customRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "PrivilegedRole" };
        _roleManager.FindByNameAsync("PrivilegedRole").Returns(customRole);
        _roleManager.GetClaimsAsync(customRole).Returns(
            [new Claim(AppPermissions.ClaimType, AppPermissions.Roles.Manage)]);

        _userManager.IsInRoleAsync(target, "PrivilegedRole").Returns(false);
        _userManager.AddToRoleAsync(target, "PrivilegedRole").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("PrivilegedRole"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AssignRole_SystemRoleWithoutStoredPermissions_Succeeds()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();

        _roleManager.FindByNameAsync("User").Returns(TestRoles.Create(AppRoles.User));
        _userManager.IsInRoleAsync(target, "User").Returns(false);
        _userManager.AddToRoleAsync(target, "User").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AssignRole_SystemRoleWithUnheldPermissions_ReturnsForbidden()
    {
        // Operators can expand a system role's permission set at runtime, so rank >= 1
        // roles are escalation-checked too: the caller holds no permissions here.
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();

        var userRole = TestRoles.Create(AppRoles.User);
        _roleManager.FindByNameAsync("User").Returns(userRole);
        _roleManager.GetClaimsAsync(userRole).Returns(
            [new Claim(AppPermissions.ClaimType, AppPermissions.Roles.View)]);
        _userManager.IsInRoleAsync(target, "User").Returns(false);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("User"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleAssignEscalation, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task AssignRole_GrantsAllRole_CallerWithoutWildcard_ReturnsForbidden()
    {
        // A grants-all role has no stored claims; assigning it hands out wildcard access.
        // An admin holding users.assign_roles but not the wildcard must be blocked.
        SetupCallerAsAdmin(AppPermissions.Users.AssignRoles, AppPermissions.Users.View);
        SetupTargetAsUser();

        var ownerRole = TestRoles.Create("Owner");
        ownerRole.GrantsAllPermissions = true;
        _roleManager.FindByNameAsync("Owner").Returns(ownerRole);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("Owner"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleAssignEscalation, result.Error);
        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        // The wildcard requirement is synthesized; stored claims are never consulted.
        await _roleManager.DidNotReceive().GetClaimsAsync(Arg.Any<ApplicationRole>());
    }

    [Fact]
    public async Task AssignRole_GrantsAllRole_SuperuserCaller_Succeeds()
    {
        var caller = new ApplicationUser { Id = _callerId, UserName = "superuser@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Superuser });
        TestRoles.SeedAssigned(_dbContext, _callerId, AppRoles.Superuser);

        var target = SetupTargetAsUser();

        var ownerRole = TestRoles.Create("Owner");
        ownerRole.GrantsAllPermissions = true;
        _roleManager.FindByNameAsync("Owner").Returns(ownerRole);
        _userManager.IsInRoleAsync(target, "Owner").Returns(false);
        _userManager.AddToRoleAsync(target, "Owner").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("Owner"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AssignRole_CustomRoleNoPermissions_Succeeds()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();

        var customRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "LabelRole" };
        _roleManager.FindByNameAsync("LabelRole").Returns(customRole);
        _roleManager.GetClaimsAsync(customRole).Returns(new List<Claim>());

        _userManager.IsInRoleAsync(target, "LabelRole").Returns(false);
        _userManager.AddToRoleAsync(target, "LabelRole").Returns(IdentityResult.Success);

        var result = await _sut.AssignRoleAsync(_callerId, _targetId, new AssignRoleInput("LabelRole"));

        Assert.True(result.IsSuccess);
    }

    #endregion

    #region RemoveRole

    [Fact]
    public async Task RemoveRole_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("User").Returns(TestRoles.Create(AppRoles.User));
        _userManager.IsInRoleAsync(target, "User").Returns(true);
        _userManager.RemoveFromRoleAsync(target, "User").Returns(IdentityResult.Success);

        var result = await _sut.RemoveRoleAsync(_callerId, _targetId, "User");

        Assert.True(result.IsSuccess);
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminRemoveRole,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: _targetId,
            metadata: Arg.Is<string>(m => m.Contains("User")),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveRole_SelfRemoval_ReturnsFailure()
    {
        _roleManager.FindByNameAsync("User").Returns(TestRoles.Create(AppRoles.User));
        var user = new ApplicationUser { Id = _callerId, UserName = "self@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(user);

        var result = await _sut.RemoveRoleAsync(_callerId, _callerId, "User");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleSelfRemove, result.Error);
    }

    [Fact]
    public async Task RemoveRole_RoleAboveCallerRank_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("Admin").Returns(TestRoles.Create(AppRoles.Admin));
        _userManager.IsInRoleAsync(target, "Admin").Returns(true);

        var result = await _sut.RemoveRoleAsync(_callerId, _targetId, "Admin");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleRemoveAboveRank, result.Error);
    }

    [Fact]
    public async Task RemoveRole_UserDoesNotHaveRole_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _roleManager.FindByNameAsync("User").Returns(TestRoles.Create(AppRoles.User));
        _userManager.IsInRoleAsync(target, "User").Returns(false);

        var result = await _sut.RemoveRoleAsync(_callerId, _targetId, "User");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.RoleNotAssigned, result.Error);
    }

    [Fact]
    public async Task RemoveRole_LastGrantsAllHolder_ReturnsFailure()
    {
        // Removing the only assignment of any grants-all role would lock everyone out.
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();

        var ownerRole = TestRoles.Create("Owner");
        ownerRole.GrantsAllPermissions = true;
        _dbContext.Roles.Add(ownerRole);
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = ownerRole.Id, UserId = _targetId });
        await _dbContext.SaveChangesAsync();

        _roleManager.FindByNameAsync("Owner").Returns(ownerRole);
        _userManager.IsInRoleAsync(target, "Owner").Returns(true);

        var result = await _sut.RemoveRoleAsync(_callerId, _targetId, "Owner");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.LastRoleHolder, result.Error);
    }

    [Fact]
    public async Task RemoveRole_GrantsAllRole_SecondHolderUnblocksRemoval()
    {
        // A second user holding another grants-all role satisfies the lockout invariant.
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();

        var ownerRole = TestRoles.Create("Owner");
        ownerRole.GrantsAllPermissions = true;
        _dbContext.Roles.Add(ownerRole);
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = ownerRole.Id, UserId = _targetId });
        await _dbContext.SaveChangesAsync();
        TestRoles.SeedAssigned(_dbContext, Guid.NewGuid(), AppRoles.Superuser);

        _roleManager.FindByNameAsync("Owner").Returns(ownerRole);
        _userManager.IsInRoleAsync(target, "Owner").Returns(true);
        _userManager.RemoveFromRoleAsync(target, "Owner").Returns(IdentityResult.Success);

        var result = await _sut.RemoveRoleAsync(_callerId, _targetId, "Owner");

        Assert.True(result.IsSuccess);
    }

    #endregion

    #region LockUser

    [Fact]
    public async Task LockUser_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetLockoutEndDateAsync(target, Arg.Any<DateTimeOffset>())
            .Returns(IdentityResult.Success);

        var result = await _sut.LockUserAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminLockUser,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: _targetId,
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LockUser_Valid_RevokesRefreshTokens()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetLockoutEndDateAsync(target, Arg.Any<DateTimeOffset>())
            .Returns(IdentityResult.Success);

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "hashed",
            UserId = _targetId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        await _sut.LockUserAsync(_callerId, _targetId);

        var token = Assert.Single(_dbContext.RefreshTokens);
        Assert.True(token.IsInvalidated);
    }

    [Fact]
    public async Task LockUser_SelfLock_ReturnsFailure()
    {
        var user = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(user);

        var result = await _sut.LockUserAsync(_callerId, _callerId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.LockSelfAction, result.Error);
    }

    [Fact]
    public async Task LockUser_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.LockUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task LockUser_InsufficientHierarchy_ReturnsFailure()
    {
        // Caller is User (rank 1), target is Admin (rank 2)
        var caller = new ApplicationUser { Id = _callerId, UserName = "user@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.User });
        TestRoles.Seed(_dbContext, AppRoles.User);

        var target = new ApplicationUser { Id = _targetId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.Admin });
        TestRoles.Seed(_dbContext, AppRoles.Admin);

        var result = await _sut.LockUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.HierarchyInsufficient, result.Error);
    }

    #endregion

    #region UnlockUser

    [Fact]
    public async Task UnlockUser_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetLockoutEndDateAsync(target, null).Returns(IdentityResult.Success);
        _userManager.ResetAccessFailedCountAsync(target).Returns(IdentityResult.Success);

        var result = await _sut.UnlockUserAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminUnlockUser,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: _targetId,
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnlockUser_Valid_ResetsAccessFailedCount()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.SetLockoutEndDateAsync(target, null).Returns(IdentityResult.Success);
        _userManager.ResetAccessFailedCountAsync(target).Returns(IdentityResult.Success);

        await _sut.UnlockUserAsync(_callerId, _targetId);

        await _userManager.Received(1).ResetAccessFailedCountAsync(target);
    }

    [Fact]
    public async Task UnlockUser_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.UnlockUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
    }

    #endregion

    #region DeleteUser

    [Fact]
    public async Task DeleteUser_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        _userManager.DeleteAsync(target).Returns(IdentityResult.Success);

        var result = await _sut.DeleteUserAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminDeleteUser,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: _targetId,
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUser_SelfDelete_ReturnsFailure()
    {
        var user = new ApplicationUser { Id = _callerId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(user);

        var result = await _sut.DeleteUserAsync(_callerId, _callerId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.DeleteSelfAction, result.Error);
    }

    [Fact]
    public async Task DeleteUser_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.DeleteUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteUser_LastAdmin_Succeeds()
    {
        // Superuser (rank 3) can delete the last Admin (rank 2) - only grants-all roles are protected
        var caller = new ApplicationUser { Id = _callerId, UserName = "superuser@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Superuser });
        TestRoles.Seed(_dbContext, AppRoles.Superuser);

        var target = new ApplicationUser { Id = _targetId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.Admin });

        // Only 1 user in Admin role - this should not block deletion (Admin does not grant all permissions)
        TestRoles.SeedAssigned(_dbContext, _targetId, AppRoles.Admin);

        _userManager.DeleteAsync(target).Returns(IdentityResult.Success);

        var result = await _sut.DeleteUserAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteUser_LastSuperuser_ReturnsFailure()
    {
        // Caller is Superuser; target holds Superuser in the database (the lockout check
        // reads assignments from the database). EnforceHierarchyAsync needs
        // callerRank > targetRank to pass, so the target's mocked role list stays at User.
        var caller = new ApplicationUser { Id = _callerId, UserName = "superuser@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Superuser });

        var target = new ApplicationUser { Id = _targetId, UserName = "target@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.User });

        // Only 1 user holds a grants-all role - this should block deletion
        TestRoles.SeedAssigned(_dbContext, _targetId, AppRoles.Superuser);

        var result = await _sut.DeleteUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.LastSuperuserCannotDelete, result.Error);
    }

    [Fact]
    public async Task DeleteUser_DeleteFails_LeavesSessionsAndAvatarIntact()
    {
        // Destructive side effects (session revocation, avatar deletion) must only run
        // after a committed delete - a failed delete leaves the user fully intact.
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        target.HasAvatar = true;
        _userManager.DeleteAsync(target).Returns(IdentityResult.Failed());

        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = "hashed",
            UserId = _targetId,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiredAt = _timeProvider.GetUtcNow().UtcDateTime.AddDays(7),
            IsUsed = false,
            IsInvalidated = false
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.DeleteUserAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.DeleteFailed, result.Error);
        var token = Assert.Single(_dbContext.RefreshTokens);
        Assert.False(token.IsInvalidated);
        await _fileStorageService.DidNotReceive()
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteUser_SuperuserWithSecondGrantsAllHolder_Succeeds()
    {
        // A second user holding a grants-all role unblocks deleting a superuser.
        var caller = new ApplicationUser { Id = _callerId, UserName = "superuser@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.Superuser });

        var target = new ApplicationUser { Id = _targetId, UserName = "target@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.User });

        var superuserRole = TestRoles.SeedAssigned(_dbContext, _targetId, AppRoles.Superuser);
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = superuserRole.Id, UserId = _callerId });
        await _dbContext.SaveChangesAsync();

        _userManager.DeleteAsync(target).Returns(IdentityResult.Success);

        var result = await _sut.DeleteUserAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
    }

    #endregion

    #region GetUsersAsync

    [Fact(Skip = "EF InMemory provider does not support GroupBy — requires Testcontainers (issue #174)")]
    public async Task GetUsers_ReturnsPagedResults()
    {
        // Seed users into InMemory database
        for (var i = 0; i < 5; i++)
        {
            _dbContext.Users.Add(new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"user{i}@test.com",
                Email = $"user{i}@test.com",
                NormalizedUserName = $"USER{i}@TEST.COM"
            });
        }
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(1, 3);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.Users.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(3, result.PageSize);
    }

    [Fact(Skip = "EF InMemory provider does not support GroupBy — requires Testcontainers (issue #174)")]
    public async Task GetUsers_WithSearch_FiltersResults()
    {
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = "alice@test.com",
            NormalizedUserName = "ALICE@TEST.COM", FirstName = "Alice"
        });
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(), UserName = "bob@test.com",
            NormalizedUserName = "BOB@TEST.COM", FirstName = "Bob"
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(1, 10, "alice");

        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Users);
        Assert.Equal("alice@test.com", result.Users[0].UserName);
    }

    [Fact(Skip = "EF InMemory provider does not support GroupBy — requires Testcontainers (issue #174)")]
    public async Task GetUsers_MapsRolesCorrectly()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _dbContext.Users.Add(new ApplicationUser
        {
            Id = userId, UserName = "withRole@test.com",
            NormalizedUserName = "WITHROLE@TEST.COM"
        });
        _dbContext.Roles.Add(new ApplicationRole { Id = roleId, Name = "Admin", NormalizedName = "ADMIN" });
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { UserId = userId, RoleId = roleId });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(1, 10);

        var user = Assert.Single(result.Users);
        Assert.Contains("Admin", user.Roles);
    }

    [Fact(Skip = "EF InMemory provider does not support GroupBy — requires Testcontainers (issue #174)")]
    public async Task GetUsers_CalculatesLockoutStatus()
    {
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "locked@test.com",
            NormalizedUserName = "LOCKED@TEST.COM",
            LockoutEnd = _timeProvider.GetUtcNow().AddYears(100)
        });
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "unlocked@test.com",
            NormalizedUserName = "UNLOCKED@TEST.COM",
            LockoutEnd = null
        });
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetUsersAsync(1, 10);

        Assert.Equal(2, result.Users.Count);
        var locked = result.Users.First(u => u.UserName == "locked@test.com");
        var unlocked = result.Users.First(u => u.UserName == "unlocked@test.com");
        Assert.True(locked.IsLockedOut);
        Assert.False(unlocked.IsLockedOut);
    }

    #endregion

    #region GetRolesAsync

    [Fact]
    public async Task GetRoles_ReturnsAllRoles()
    {
        var role1 = new ApplicationRole { Id = Guid.NewGuid(), Name = AppRoles.Admin, NormalizedName = "ADMIN" };
        var role2 = new ApplicationRole { Id = Guid.NewGuid(), Name = "Custom", NormalizedName = "CUSTOM" };

        // Use the InMemory DbContext roles so roleManager.Roles resolves via EF
        _dbContext.Roles.Add(role1);
        _dbContext.Roles.Add(role2);
        await _dbContext.SaveChangesAsync();
        _roleManager.Roles.Returns(_dbContext.Roles);

        var result = await _sut.GetRolesAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetRoles_IdentifiesSystemRoles()
    {
        var adminRole = TestRoles.Create(AppRoles.Admin);
        var customRole = new ApplicationRole { Id = Guid.NewGuid(), Name = "Custom", NormalizedName = "CUSTOM" };

        _dbContext.Roles.Add(adminRole);
        _dbContext.Roles.Add(customRole);
        await _dbContext.SaveChangesAsync();
        _roleManager.Roles.Returns(_dbContext.Roles);

        var result = await _sut.GetRolesAsync();

        var admin = result.First(r => r.Name == AppRoles.Admin);
        var custom = result.First(r => r.Name == "Custom");
        Assert.True(admin.IsSystem);
        Assert.Equal(2, admin.Rank);
        Assert.False(admin.GrantsAllPermissions);
        Assert.False(custom.IsSystem);
        Assert.Equal(0, custom.Rank);
        Assert.False(custom.GrantsAllPermissions);
    }

    [Fact]
    public async Task GetRoles_CountsUsersPerRole()
    {
        var roleId = Guid.NewGuid();
        _dbContext.Roles.Add(new ApplicationRole { Id = roleId, Name = "TestRole", NormalizedName = "TESTROLE" });
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = roleId, UserId = Guid.NewGuid() });
        _dbContext.UserRoles.Add(new IdentityUserRole<Guid> { RoleId = roleId, UserId = Guid.NewGuid() });
        await _dbContext.SaveChangesAsync();
        _roleManager.Roles.Returns(_dbContext.Roles);

        var result = await _sut.GetRolesAsync();

        var role = Assert.Single(result);
        Assert.Equal(2, role.UserCount);
    }

    #endregion

    #region VerifyEmail

    [Fact]
    public async Task VerifyEmail_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        target.EmailConfirmed = false;
        _userManager.GenerateEmailConfirmationTokenAsync(target).Returns("token");
        _userManager.ConfirmEmailAsync(target, "token").Returns(IdentityResult.Success);

        var result = await _sut.VerifyEmailAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminVerifyEmail,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: _targetId,
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task VerifyEmail_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.VerifyEmailAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task VerifyEmail_AlreadyVerified_ReturnsFailure()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        target.EmailConfirmed = true;

        var result = await _sut.VerifyEmailAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Auth.EmailAlreadyVerified, result.Error);
    }

    [Fact]
    public async Task VerifyEmail_InsufficientHierarchy_ReturnsFailure()
    {
        // Caller is User (rank 1), target is Admin (rank 2)
        var caller = new ApplicationUser { Id = _callerId, UserName = "user@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.User });
        TestRoles.Seed(_dbContext, AppRoles.User);

        var target = new ApplicationUser { Id = _targetId, UserName = "admin@test.com", EmailConfirmed = false };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.Admin });
        TestRoles.Seed(_dbContext, AppRoles.Admin);

        var result = await _sut.VerifyEmailAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.HierarchyInsufficient, result.Error);
    }

    #endregion

    #region SendPasswordReset

    [Fact]
    public async Task SendPasswordReset_Valid_ReturnsSuccess()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        target.Email = "user@test.com";
        _userManager.GeneratePasswordResetTokenAsync(target).Returns("reset-token");

        var result = await _sut.SendPasswordResetAsync(_callerId, _targetId);

        Assert.True(result.IsSuccess);
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminSendPasswordReset,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: _targetId,
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
        await _templatedEmailSender.Received(1).SendSafeAsync(
            "admin-reset-password",
            Arg.Is<AdminResetPasswordModel>(m =>
                m.ResetUrl.Contains("https://example.com/reset-password?token=") &&
                !m.ResetUrl.Contains("email=")),
            "user@test.com",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPasswordReset_CreatesOpaqueEmailToken()
    {
        SetupCallerAsAdmin();
        var target = SetupTargetAsUser();
        target.Email = "user@test.com";
        _userManager.GeneratePasswordResetTokenAsync(target).Returns("reset-token");

        await _sut.SendPasswordResetAsync(_callerId, _targetId);

        var emailToken = Assert.Single(_dbContext.EmailTokens);
        Assert.Equal(_targetId, emailToken.UserId);
        Assert.Equal(EmailTokenPurpose.PasswordReset, emailToken.Purpose);
        Assert.Equal("reset-token", emailToken.IdentityToken);
        Assert.False(emailToken.IsUsed);
    }

    [Fact]
    public async Task SendPasswordReset_UserNotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.SendPasswordResetAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task SendPasswordReset_InsufficientHierarchy_ReturnsFailure()
    {
        var caller = new ApplicationUser { Id = _callerId, UserName = "user@test.com" };
        _userManager.FindByIdAsync(_callerId.ToString()).Returns(caller);
        _userManager.GetRolesAsync(caller).Returns(new List<string> { AppRoles.User });

        var target = new ApplicationUser { Id = _targetId, UserName = "admin@test.com" };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(target);
        _userManager.GetRolesAsync(target).Returns(new List<string> { AppRoles.Admin });

        var result = await _sut.SendPasswordResetAsync(_callerId, _targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.HierarchyInsufficient, result.Error);
    }

    #endregion

    #region CreateUser

    [Fact]
    public async Task CreateUser_Valid_ReturnsUserId()
    {
        var newUserId = Guid.NewGuid();
        _userManager.FindByEmailAsync("new@test.com").Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(callInfo =>
            {
                callInfo.Arg<ApplicationUser>().Id = newUserId;
                return IdentityResult.Success;
            });
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), AppRoles.User)
            .Returns(IdentityResult.Success);
        _userManager.GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>())
            .Returns("reset-token");

        var result = await _sut.CreateUserAsync(_callerId, new CreateUserInput("new@test.com", "John", "Doe"));

        Assert.True(result.IsSuccess);
        Assert.Equal(newUserId, result.Value);
        await _auditService.Received(1).LogAsync(
            AuditActions.AdminCreateUser,
            userId: _callerId,
            targetEntityType: "User",
            targetEntityId: newUserId,
            metadata: Arg.Any<string?>(),
            ct: Arg.Any<CancellationToken>());
        await _templatedEmailSender.Received(1).SendSafeAsync(
            "invitation",
            Arg.Any<InvitationModel>(),
            "new@test.com",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_ReturnsFailure()
    {
        _userManager.FindByEmailAsync("existing@test.com")
            .Returns(new ApplicationUser { Id = Guid.NewGuid(), Email = "existing@test.com" });

        var result = await _sut.CreateUserAsync(_callerId, new CreateUserInput("existing@test.com", null, null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.EmailAlreadyRegistered, result.Error);
    }

    [Fact]
    public async Task CreateUser_SendsInvitationEmail_WithOpaqueToken()
    {
        _userManager.FindByEmailAsync("invite@test.com").Returns((ApplicationUser?)null);
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(IdentityResult.Success);
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), AppRoles.User)
            .Returns(IdentityResult.Success);
        _userManager.GeneratePasswordResetTokenAsync(Arg.Any<ApplicationUser>())
            .Returns("reset-token");

        await _sut.CreateUserAsync(_callerId, new CreateUserInput("invite@test.com", null, null));

        await _templatedEmailSender.Received(1).SendSafeAsync(
            "invitation",
            Arg.Is<InvitationModel>(m =>
                m.SetPasswordUrl.Contains("https://example.com/reset-password?token=") &&
                m.SetPasswordUrl.Contains("&invited=1") &&
                !m.SetPasswordUrl.Contains("email=")),
            "invite@test.com",
            Arg.Any<CancellationToken>());

        var emailToken = Assert.Single(_dbContext.EmailTokens);
        Assert.Equal(EmailTokenPurpose.PasswordReset, emailToken.Purpose);
        Assert.Equal("reset-token", emailToken.IdentityToken);
    }

    #endregion

    #region GetUserById

    [Fact]
    public async Task GetUserById_Found_ReturnsSuccess()
    {
        var user = new ApplicationUser
        {
            Id = _targetId,
            UserName = "user@test.com",
            Email = "user@test.com",
            FirstName = "John",
            LastName = "Doe"
        };
        _userManager.FindByIdAsync(_targetId.ToString()).Returns(user);
        _userManager.GetRolesAsync(user).Returns(new List<string> { AppRoles.User });

        var result = await _sut.GetUserByIdAsync(_targetId);

        Assert.True(result.IsSuccess);
        Assert.Equal("user@test.com", result.Value.UserName);
        Assert.Equal("John", result.Value.FirstName);
    }

    [Fact]
    public async Task GetUserById_NotFound_ReturnsNotFound()
    {
        _userManager.FindByIdAsync(_targetId.ToString()).Returns((ApplicationUser?)null);

        var result = await _sut.GetUserByIdAsync(_targetId);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorMessages.Admin.UserNotFound, result.Error);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    #endregion
}
