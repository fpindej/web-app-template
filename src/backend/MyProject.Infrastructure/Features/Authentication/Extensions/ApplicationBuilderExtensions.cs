using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Authentication.Extensions;

/// <summary>
/// Extension methods for seeding Identity roles and development test users at startup.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Seeds all roles defined in <see cref="AppRoles.All"/> if they do not already exist.
    /// This should run in every environment to ensure the role set is consistent.
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    public static async Task SeedRolesAsync(this IApplicationBuilder appBuilder)
    {
        using var scope = appBuilder.ApplicationServices.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }

    /// <summary>
    /// Seeds test users for local development. Must not be called in production.
    /// <para>
    /// User credentials are defined in <see cref="SeedUsers"/>.
    /// Roles must already exist — call <see cref="SeedRolesAsync"/> first.
    /// </para>
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    public static async Task SeedDevelopmentUsersAsync(this IApplicationBuilder appBuilder)
    {
        using var scope = appBuilder.ApplicationServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await SeedUserAsync(userManager, SeedUsers.TestUserEmail, SeedUsers.TestUserPassword, AppRoles.User);
        await SeedUserAsync(userManager, SeedUsers.AdminEmail, SeedUsers.AdminPassword, AppRoles.Admin);
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
