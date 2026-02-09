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
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration for reading CORS options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var corsSettings = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()
                           ?? throw new InvalidOperationException("CORS options are not configured properly.");

        services.AddCors(options =>
        {
            options.AddPolicy(corsSettings.PolicyName, policy =>
            {
                {
                    policy.ConfigureCorsPolicy(corsSettings);
                }
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
