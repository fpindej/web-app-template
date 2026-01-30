using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace MyProject.Infrastructure.Logging.Extensions;

/// <summary>
/// Extension methods for configuring Serilog logging.
/// </summary>
public static class LoggerConfigurationExtensions
{
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception:j}";

    /// <summary>
    /// Configures a minimal logging setup for early startup events before the full configuration
    /// from appsettings.json is available.
    /// </summary>
    /// <param name="environmentName">The current environment name (Development, Production, etc.).</param>
    /// <returns>A bootstrap logger for early startup logging.</returns>
    public static ILogger ConfigureMinimalLogging(string environmentName)
    {
        var loggerConfiguration = new LoggerConfiguration();

        switch (environmentName)
        {
            case Environments.Development:
                loggerConfiguration.MinimumLevel.Debug();
                break;
            case Environments.Production:
                loggerConfiguration.MinimumLevel.Information();
                break;

            default:
                loggerConfiguration.MinimumLevel.Information();
                break;
        }

        loggerConfiguration.WriteTo.Console(theme: AnsiConsoleTheme.Code, outputTemplate: OutputTemplate);

        return loggerConfiguration.CreateBootstrapLogger();
    }

    /// <summary>
    /// Configures the full Serilog logger using application configuration.
    /// Additional sinks (e.g., Seq) are configured via appsettings.json WriteTo section.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="loggerConfiguration">The logger configuration to set up.</param>
    public static void SetupLogger(IConfiguration configuration, LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .WriteTo.Async(a => a.Console(theme: AnsiConsoleTheme.Code, outputTemplate: OutputTemplate));
    }
}
