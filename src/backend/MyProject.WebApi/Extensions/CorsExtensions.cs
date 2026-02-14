using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using CorsOptions = MyProject.WebApi.Options.CorsOptions;

namespace MyProject.WebApi.Extensions;

/// <summary>
/// Extension methods for registering and applying the CORS policy.
/// </summary>
internal static class CorsExtensions
{
    /// <summary>
    /// Registers CORS services and configures the policy from <see cref="CorsOptions"/>.
    /// Rejects <c>AllowAllOrigins = true</c> in non-development environments at startup
    /// to prevent credential-leaking CORS misconfiguration in production.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration for reading CORS options.</param>
    /// <param name="environment">The hosting environment, used to enforce production safeguards.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>AllowAllOrigins</c> is <c>true</c> in a non-development environment.
    /// </exception>
    public static IServiceCollection AddCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var corsSettings = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
                           ?? throw new InvalidOperationException("CORS options are not configured properly.");

        if (corsSettings.AllowAllOrigins && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "AllowAllOrigins is enabled in a non-development environment. " +
                "This allows any origin to make authenticated cross-origin requests and is a security vulnerability. " +
                "Set Cors:AllowAllOrigins to false and configure Cors:AllowedOrigins with your production domain(s).");
        }

        services.AddCors(options =>
        {
            options.AddPolicy(corsSettings.PolicyName, policy =>
            {
                policy.ConfigureCorsPolicy(corsSettings);
            });
        });

        return services;
    }

    /// <summary>
    /// Applies the configured CORS policy to the request pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseCors(this IApplicationBuilder app)
    {
        var corsOptions = app.ApplicationServices.GetRequiredService<IOptions<CorsOptions>>().Value;

        app.UseCors(corsOptions.PolicyName);

        return app;
    }

    private static CorsPolicyBuilder ConfigureCorsPolicy(this CorsPolicyBuilder policy, CorsOptions corsOptions)
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

        return corsOptions.AllowAllOrigins switch
        {
            true => policy.SetIsOriginAllowed(_ => true),
            false => policy.WithOrigins(corsOptions.AllowedOrigins)
        };
    }
}
