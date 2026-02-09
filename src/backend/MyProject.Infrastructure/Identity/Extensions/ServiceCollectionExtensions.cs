using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Identity;
using MyProject.Infrastructure.Identity.Services;

namespace MyProject.Infrastructure.Identity.Extensions;

/// <summary>
/// Extension methods for registering user context and user service dependencies.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IUserContext"/> and <see cref="IUserService"/> with their implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddUserContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
