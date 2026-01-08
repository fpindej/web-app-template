namespace MyProject.WebApi.Shared;

/// <summary>
/// Response DTO for error information.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// The main error message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Additional error details or technical information.
    /// </summary>
    public string? Details { get; init; }
}
