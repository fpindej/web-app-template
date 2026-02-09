using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Cookies;

namespace MyProject.Infrastructure.Cookies.Extensions;

/// <summary>
/// Extension methods for registering cookie management services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICookieService"/> with its implementation.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCookieServices(this IServiceCollection services)
    {
        services.AddScoped<ICookieService, CookieService>();
        return services;
    }
}
