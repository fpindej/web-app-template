using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using CorsOptions = MyProject.WebApi.Options.CorsOptions;

namespace MyProject.WebApi.Extensions;

internal static class CorsExtensions
{
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
