using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Authentication.Extensions;

/// <summary>
/// Extension methods for seeding default Identity users and roles at startup.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Seeds default roles and test users if they do not already exist.
    /// <para>
    /// Roles are defined in <see cref="AppRoles"/> — this method ensures all roles from
    /// <see cref="AppRoles.All"/> exist, then creates test users for development.
    /// </para>
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    public static async Task SeedIdentityUsersAsync(this IApplicationBuilder appBuilder)
    {
        using var scope = appBuilder.ApplicationServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        await SeedRolesAsync(roleManager);
        await SeedUserAsync(userManager, SeedUsers.TestUserEmail, SeedUsers.TestUserPassword, AppRoles.User);
        await SeedUserAsync(userManager, SeedUsers.AdminEmail, SeedUsers.AdminPassword, AppRoles.Admin);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string role)
    {
        if (await userManager.FindByNameAsync(email) is not null)
        {
            return;
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
    }
}
