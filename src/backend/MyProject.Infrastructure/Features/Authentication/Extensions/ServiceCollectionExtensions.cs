using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Hybrid;
using MyProject.Application.Caching.Constants;
using MyProject.Application.Cookies.Constants;
using MyProject.Application.Cryptography;
using MyProject.Application.Features.Authentication;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Models;
using MyProject.Infrastructure.Features.Authentication.Options;
using MyProject.Infrastructure.Features.Authentication.Services;
using MyProject.Infrastructure.Features.Authentication.Services.ExternalProviders;

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
            services.AddScoped<ITokenSessionService, TokenSessionService>();
            services.AddScoped<EmailTokenService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<ITwoFactorService, TwoFactorService>();
            services.AddScoped<IExternalAuthService, ExternalAuthService>();

            services.ConfigureExternalProviders(configuration);

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

                    opt.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;

                opt.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<TContext>()
                .AddDefaultTokenProviders();

            var authOptions = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
                ?? throw new InvalidOperationException($"Missing '{AuthenticationOptions.SectionName}' configuration section.");
            services.Configure<DataProtectionTokenProviderOptions>(opt =>
                opt.TokenLifespan = authOptions.EmailToken.Lifetime);

            return services;
        }

        private IServiceCollection ConfigureJwtAuthentication(IConfiguration configuration)
        {
            services.AddOptions<AuthenticationOptions>()
                .BindConfiguration(AuthenticationOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            var authOptions = configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>()
                ?? throw new InvalidOperationException($"Missing '{AuthenticationOptions.SectionName}' configuration section.");
            var jwtOptions = authOptions.Jwt;
            var key = Encoding.UTF8.GetBytes(jwtOptions.Key);
            var securityStampClaimType = jwtOptions.SecurityStampClaimType;

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

                        OnTokenValidated = async context =>
                        {
                            await ValidateSecurityStampAsync(context, securityStampClaimType);
                        },
                    };

                    opt.SaveToken = true;
                });

            return services;
        }

        private void ConfigureExternalProviders(IConfiguration configuration)
        {
            services.AddOptions<ExternalAuthOptions>()
                .BindConfiguration(ExternalAuthOptions.SectionName)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHttpClient(GoogleAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, GoogleAuthProvider>();

            services.AddHttpClient(GitHubAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, GitHubAuthProvider>();

            services.AddHttpClient(DiscordAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, DiscordAuthProvider>();

            services.AddHttpClient(AppleAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, AppleAuthProvider>();

            services.AddHttpClient(MicrosoftAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, MicrosoftAuthProvider>();

            services.AddHttpClient(LinkedInAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, LinkedInAuthProvider>();

            services.AddHttpClient(GitLabAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, GitLabAuthProvider>();

            services.AddHttpClient(FacebookAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, FacebookAuthProvider>();

            services.AddHttpClient(SlackAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, SlackAuthProvider>();

            services.AddHttpClient(TwitchAuthProvider.HttpClientName);
            services.AddSingleton<IExternalAuthProvider, TwitchAuthProvider>();

            services.AddSingleton<ISecretEncryptionService, AesGcmEncryptionService>();
            services.AddScoped<IProviderConfigService, ProviderConfigService>();
        }
    }

    /// <summary>
    /// Validates the security stamp claim in the JWT token against the current stamp in the database.
    /// Uses HybridCache (5 minute TTL) to avoid a database hit on every request.
    /// Tokens without the claim are rejected (fail closed), as are tokens whose stamp has changed
    /// (password change, role update, session revocation).
    /// </summary>
    internal static async Task ValidateSecurityStampAsync(TokenValidatedContext context, string securityStampClaimType)
    {
        var stampClaim = context.Principal?.FindFirstValue(securityStampClaimType);
        if (string.IsNullOrEmpty(stampClaim))
        {
            // Fail closed: every token issued by this API carries the stamp claim,
            // so a token without it is stale or forged and must be rejected.
            context.Fail("Missing security stamp claim.");
            return;
        }

        var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            context.Fail("Invalid user identifier.");
            return;
        }

        var hybridCache = context.HttpContext.RequestServices.GetRequiredService<HybridCache>();
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

        var cacheOptions = new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) };

        var currentStampHash = await hybridCache.GetOrCreateAsync(
            CacheKeys.SecurityStamp(userId),
            async ct =>
            {
                var user = await userManager.FindByIdAsync(userId.ToString());
                return user?.SecurityStamp is not null ? HashHelper.Sha256(user.SecurityStamp) : string.Empty;
            },
            cacheOptions,
            cancellationToken: context.HttpContext.RequestAborted);

        if (string.IsNullOrEmpty(currentStampHash))
        {
            // User not found or has no stamp - reject
            context.Fail("User not found.");
            return;
        }

        if (!string.Equals(stampClaim, currentStampHash, StringComparison.Ordinal))
        {
            context.Fail("Security stamp has changed.");
        }
    }
}
