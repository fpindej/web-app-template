namespace MyProject.Infrastructure.Logging;

/// <summary>
/// Defines standard environment names used throughout the application.
/// </summary>
public static class Environments
{
    /// <summary>
    /// The development environment.
    /// Used when the application is in development mode, enabling debug-level logging and other developer-oriented features.
    /// </summary>
    public const string Development = "Development";

    /// <summary>
    /// The production environment.
    /// Used when the application is in production mode, enabling optimizations and stricter logging levels for a live environment.
    /// </summary>
    public const string Production = "Production";
}
