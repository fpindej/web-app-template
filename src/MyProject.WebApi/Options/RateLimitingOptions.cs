using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Options;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Required]
    public GlobalLimitOptions Global { get; init; } = new();
}

public class GlobalLimitOptions
{
    [Range(1, 1000)]
    public int PermitLimit { get; [UsedImplicitly] init; } = 100;

    public TimeSpan Window { get; [UsedImplicitly] init; } = TimeSpan.FromMinutes(1);
}
