using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base EF Core configuration for all entities extending <see cref="BaseEntity"/>.
/// Configures primary key, audit columns, and soft-delete index.
/// </summary>
/// <remarks>Pattern documented in src/backend/AGENTS.md — update both when changing.</remarks>
public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity> where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .IsRequired(false);

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DeletedAt)
            .IsRequired(false);

        // Add index on IsDeleted for better performance with soft delete filtering
        builder.HasIndex(e => e.IsDeleted);

        // Global query filter — automatically excludes soft-deleted entities from all queries.
        // Use .IgnoreQueryFilters() to explicitly query deleted entities (e.g., RestoreAsync).
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Configure entity-specific properties
        ConfigureEntity(builder);
    }

    /// <summary>
    /// Configure entity-specific properties, relationships, and indexes
    /// </summary>
    /// <param name="builder">The entity type builder</param>
    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
