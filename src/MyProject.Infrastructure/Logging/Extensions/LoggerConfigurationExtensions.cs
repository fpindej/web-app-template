using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace MyProject.Infrastructure.Logging.Extensions;

public static class LoggerConfigurationExtensions
{
    private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception:j}";

    [Description("Configures a minimal logging setup for early startup events before the full configuration from appsettings.json is available.")]
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

    public static void SetupLogger(IConfiguration configuration, LoggerConfiguration loggerConfiguration)
    {
        loggerConfiguration
            .ReadFrom.Configuration(configuration)
            .WriteTo.Async(a => a.Console(theme: AnsiConsoleTheme.Code, outputTemplate: OutputTemplate));
    }
}
