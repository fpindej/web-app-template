using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Persistence.Extensions;

namespace MyProject.Infrastructure.Persistence;

/// <summary>
/// Application database context extending <see cref="IdentityDbContext{TUser, TRole, TKey}"/>
/// with refresh token storage and custom model configuration.
/// </summary>
internal class MyProjectDbContext(DbContextOptions<MyProjectDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    /// <summary>
    /// Gets or sets the refresh tokens table for JWT token rotation.
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    /// <summary>
    /// Configures the model by applying all <see cref="IEntityTypeConfiguration{TEntity}"/> from this assembly,
    /// the auth schema, fuzzy search extensions, and default role seed data.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyProjectDbContext).Assembly);
        modelBuilder.ApplyAuthSchema();
        modelBuilder.ApplyFuzzySearch();

        // Seed default roles
        modelBuilder.Entity<ApplicationRole>().HasData(
            new ApplicationRole
            {
                Id = Guid.Parse("76b99507-9cf8-4708-9fe8-4dc4e9ea09ae"),
                Name = "User",
                NormalizedName = "USER",
                ConcurrencyStamp = "76b99507-9cf8-4708-9fe8-4dc4e9ea09ae"
            },
            new ApplicationRole
            {
                Id = Guid.Parse("971e674f-c4fb-4bb2-9170-3ad7a753182c"),
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = "971e674f-c4fb-4bb2-9170-3ad7a753182c"
            }
        );
    }
}
