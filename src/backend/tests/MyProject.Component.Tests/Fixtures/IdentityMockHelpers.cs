using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Component.Tests.Fixtures;

internal static class IdentityMockHelpers
{
    public static UserManager<ApplicationUser> CreateMockUserManager()
    {
        var store = Substitute.For<IUserStore<ApplicationUser>>();
        // Real password hasher: UserManager.PasswordHasher is non-virtual and services
        // use it directly (e.g. dummy password verification in AuthenticationService)
        var passwordHasher = new PasswordHasher<ApplicationUser>();
        return Substitute.For<UserManager<ApplicationUser>>(
            store, null, passwordHasher, null, null, null, null, null, null);
    }

    public static SignInManager<ApplicationUser> CreateMockSignInManager(
        UserManager<ApplicationUser> userManager)
    {
        var contextAccessor = Substitute.For<IHttpContextAccessor>();
        var claimsFactory = Substitute.For<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var options = Substitute.For<IOptions<IdentityOptions>>();
        options.Value.Returns(new IdentityOptions());
        var logger = Substitute.For<ILogger<SignInManager<ApplicationUser>>>();
        var schemes = Substitute.For<IAuthenticationSchemeProvider>();
        var confirmation = Substitute.For<IUserConfirmation<ApplicationUser>>();

        return Substitute.For<SignInManager<ApplicationUser>>(
            userManager, contextAccessor, claimsFactory, options, logger, schemes, confirmation);
    }

    public static RoleManager<ApplicationRole> CreateMockRoleManager()
    {
        var store = Substitute.For<IRoleStore<ApplicationRole>>();
        return Substitute.For<RoleManager<ApplicationRole>>(
            store, null, null, null, null);
    }
}
