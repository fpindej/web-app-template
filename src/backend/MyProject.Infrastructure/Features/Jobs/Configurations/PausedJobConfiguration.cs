using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyProject.Infrastructure.Features.Jobs.Models;

namespace MyProject.Infrastructure.Features.Jobs.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="PausedJob"/> entity in the <c>hangfire</c> schema.
/// </summary>
internal class PausedJobConfiguration : IEntityTypeConfiguration<PausedJob>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PausedJob> builder)
    {
        builder.ToTable("pausedjobs", "hangfire");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.OriginalCron)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.PausedAt)
            .IsRequired();

        builder.HasIndex(x => x.JobId)
            .IsUnique();
    }
}
