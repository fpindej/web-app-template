using System.ComponentModel;

namespace MyProject.WebApi.Shared;

/// <summary>
/// Response DTO for error information
/// </summary>
[Description("Standard error response for API errors")]
public class ErrorResponse
{
    /// <summary>
    /// The main error message
    /// </summary>
    [Description("The main error message")]
    public string? Message { get; init; }

    /// <summary>
    /// Additional error details or technical information
    /// </summary>
    [Description("Additional error details or technical information")]
    public string? Details { get; init; }
}
