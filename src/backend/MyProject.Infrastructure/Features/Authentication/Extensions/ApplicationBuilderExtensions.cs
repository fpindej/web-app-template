using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Features.Authentication.Extensions;

/// <summary>
/// Extension methods for seeding default Identity users and roles at startup.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Seeds default roles (USER, ADMIN) and test users if they do not already exist.
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    public static async Task SeedIdentityUsersAsync(this IApplicationBuilder appBuilder)
    {
        using var scope = appBuilder.ApplicationServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        string[] roles = ["USER", "ADMIN"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }

        var testUser = await userManager.FindByNameAsync("testuser@test.com");
        if (testUser is null)
        {
            testUser = new ApplicationUser { UserName = "testuser@test.com", Email = "testuser@example.com", EmailConfirmed = true };
            await userManager.CreateAsync(testUser, "TestUser123!");
            await userManager.AddToRoleAsync(testUser, "USER");
        }

        var adminUser = await userManager.FindByNameAsync("admin@test.com");
        if (adminUser is null)
        {
            adminUser = new ApplicationUser { UserName = "admin@test.com", Email = "admin@example.com", EmailConfirmed = true };
            await userManager.CreateAsync(adminUser, "AdminUser123!");
            await userManager.AddToRoleAsync(adminUser, "ADMIN");
        }
    }
}
