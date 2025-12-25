using System.ComponentModel.DataAnnotations;

namespace MyProject.WebApi.Options;

public class CorsOptions : IValidatableObject
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Gets or sets a value indicating whether all origins are allowed.
    /// If true, AllowedOrigins will be ignored.
    /// </summary>
    public bool AllowAllOrigins { get; init; } = false;

    /// <summary>
    /// Gets or sets the list of allowed origins.
    /// Only used when AllowAllOrigins is false.
    /// </summary>
    public string[] AllowedOrigins { get; init; } = [];

    /// <summary>
    /// Gets or sets the policy name.
    /// </summary>
    [Required]
    public string PolicyName { get; init; } = "DefaultCorsPolicy";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!AllowAllOrigins && (AllowedOrigins.Length is 0))
        {
            yield return new ValidationResult(
                "AllowedOrigins cannot be empty when AllowAllOrigins is false.",
                [nameof(AllowedOrigins)]);
        }
    }
}
