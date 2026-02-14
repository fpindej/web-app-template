using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Jobs.Models;
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
    /// Gets or sets the paused jobs table for persisting pause state across restarts.
    /// </summary>
    public DbSet<PausedJob> PausedJobs { get; set; }

    /// <summary>
    /// Configures the model by applying all <see cref="IEntityTypeConfiguration{TEntity}"/> from this assembly,
    /// the auth schema, and fuzzy search extensions.
    /// <para>
    /// Role seed data is handled at runtime by <see cref="Extensions.ApplicationBuilderExtensions.InitializeDatabaseAsync"/>
    /// via <c>RoleManager</c>, which correctly handles normalization.
    /// See <see cref="MyProject.Application.Identity.Constants.AppRoles"/>.
    /// </para>
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyProjectDbContext).Assembly);
        modelBuilder.ApplyAuthSchema();
        modelBuilder.ApplyFuzzySearch();
    }
}
