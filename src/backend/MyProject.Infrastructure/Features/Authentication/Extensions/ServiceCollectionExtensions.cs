using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MyProject.Infrastructure.Features.Authentication.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Application.Features.Authentication;

namespace MyProject.Infrastructure.Features.Authentication.Extensions;

/// <summary>
/// Extension methods for registering ASP.NET Identity, JWT authentication, and token services.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures ASP.NET Identity with the given DbContext, registers JWT bearer authentication,
        /// and adds token provider and authentication service implementations.
        /// </summary>
        /// <typeparam name="TContext">The <see cref="DbContext"/> type used by Identity stores.</typeparam>
        /// <param name="configuration">The application configuration for reading authentication options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddIdentity<TContext>(IConfiguration configuration) where TContext : DbContext
        {
            services.ConfigureIdentity<TContext>(configuration);
            services.ConfigureJwtAuthentication(configuration);

            services.AddScoped<ITokenProvider, JwtTokenProvider>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            return services;
        }

        private IServiceCollection ConfigureIdentity<TContext>(IConfiguration configuration) where TContext : DbContext
        {
            services.AddIdentity<ApplicationUser, ApplicationRole>(opt =>
                {
                    opt.Password.RequireDigit = true;
                    opt.Password.RequireLowercase = true;
                    opt.Password.RequireUppercase = true;
                    opt.Password.RequireNonAlphanumeric = false;
                    opt.Password.RequiredLength = 6;

                    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    opt.Lockout.MaxFailedAccessAttempts = 5;
                    opt.Lockout.AllowedForNewUsers = true;

                    opt.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
                    opt.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;

                    opt.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;

                opt.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<TContext>()
                .AddDefaultTokenProviders();

            return services;
        }

        private IServiceCollection ConfigureJwtAuthentication(IConfiguration configuration)
        {
            services.AddOptions<AuthenticationOptions>()
                .BindConfiguration(AuthenticationOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var authOptions = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()!;
            var jwtOptions = authOptions.Jwt;
            var key = Encoding.UTF8.GetBytes(jwtOptions.Key);

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(opt =>
                {
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(key)
                    };

                    // Configure dual authentication: Bearer header (mobile/API) + cookie fallback (web)
                    opt.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Priority 1: Authorization header (standard Bearer token for mobile/API clients)
                            // The JWT middleware handles this automatically if we don't set context.Token,
                            // so we only need to handle the cookie fallback case.

                            // Priority 2: Cookie fallback (web clients using HttpOnly cookies)
                            var authHeader = context.Request.Headers.Authorization.ToString();
                            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                if (context.Request.Cookies.TryGetValue(CookieNames.AccessToken, out var accessToken))
                                {
                                    context.Token = accessToken;
                                }
                            }

                            return Task.CompletedTask;
                        },
                    };

                    opt.SaveToken = true;
                });

            return services;
        }
    }
}
