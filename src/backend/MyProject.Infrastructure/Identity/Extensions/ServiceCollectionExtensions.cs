using Microsoft.Extensions.DependencyInjection;
using MyProject.Application.Identity;
using MyProject.Infrastructure.Identity.Services;

namespace MyProject.Infrastructure.Identity.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
