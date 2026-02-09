using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MyProject.Application.Identity.Constants;
using MyProject.Infrastructure.Features.Authentication.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for database initialization at startup — migrations, role seeding, and development data.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Initializes the database: applies migrations (development only), seeds roles (always),
    /// and seeds test users (development only).
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    public static async Task InitializeDatabaseAsync(this IApplicationBuilder appBuilder)
    {
        using var scope = appBuilder.ApplicationServices.CreateScope();
        var services = scope.ServiceProvider;
        var isDevelopment = services.GetRequiredService<IHostEnvironment>().IsDevelopment();

        if (isDevelopment)
        {
            ApplyMigrations(services);
        }

        await SeedRolesAsync(services);

        if (isDevelopment)
        {
            await SeedDevelopmentUsersAsync(services);
        }
    }

    private static void ApplyMigrations(IServiceProvider serviceProvider)
    {
        var dbContext = serviceProvider.GetRequiredService<MyProjectDbContext>();
        dbContext.Database.Migrate();
    }

    private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }

    private static async Task SeedDevelopmentUsersAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

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
