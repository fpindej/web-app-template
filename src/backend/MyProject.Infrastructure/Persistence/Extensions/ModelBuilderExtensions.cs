using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for configuring the EF Core model with auth schema and fuzzy search support.
/// </summary>
internal static class ModelBuilderExtensions
{
    extension(ModelBuilder builder)
    {
        /// <summary>
        /// Moves all ASP.NET Identity tables into the <c>auth</c> schema.
        /// </summary>
        public void ApplyAuthSchema()
        {
            const string schema = "auth";

            _ = builder.Entity<ApplicationUser>().ToTable(name: "Users", schema);
            _ = builder.Entity<ApplicationRole>().ToTable(name: "Roles", schema);
            _ = builder.Entity<IdentityUserRole<Guid>>().ToTable(name: "UserRoles", schema);
            _ = builder.Entity<IdentityUserClaim<Guid>>().ToTable(name: "UserClaims", schema);
            _ = builder.Entity<IdentityUserLogin<Guid>>().ToTable(name: "UserLogins", schema);
            _ = builder.Entity<IdentityRoleClaim<Guid>>().ToTable(name: "RoleClaims", schema);
            _ = builder.Entity<IdentityUserToken<Guid>>().ToTable(name: "UserTokens", schema);
        }

        /// <summary>
        /// Registers the PostgreSQL <c>similarity</c> function for use in LINQ queries.
        /// </summary>
        public void ApplyFuzzySearch() =>
            builder
                .HasDbFunction(
                    typeof(StringExtensions)
                        .GetMethod(nameof(StringExtensions.Similarity))!)
                .HasName("similarity")
                .IsBuiltIn();
    }
}
