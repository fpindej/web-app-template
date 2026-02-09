using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MyProject.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for applying EF Core database migrations at startup.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Applies any pending EF Core migrations to the database.
    /// </summary>
    /// <param name="appBuilder">The application builder.</param>
    public static void ApplyMigrations(this IApplicationBuilder appBuilder)
    {
        using var scope = appBuilder.ApplicationServices.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<MyProjectDbContext>();

        dbContext.Database.Migrate();
    }
}
