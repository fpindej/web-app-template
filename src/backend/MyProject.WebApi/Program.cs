using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using MyProject.Infrastructure.Features.Admin.Extensions;
using MyProject.Infrastructure.Features.Jobs.Extensions;
using MyProject.Infrastructure.Persistence.Extensions;
using MyProject.Infrastructure.Caching.Extensions;
using MyProject.Infrastructure.Cookies.Extensions;
using MyProject.Infrastructure.Identity.Extensions;
using MyProject.WebApi.Authorization;
using MyProject.WebApi.Extensions;
using MyProject.WebApi.Features.OpenApi.Extensions;
using MyProject.WebApi.Middlewares;
using MyProject.WebApi.Routing;
using Serilog;
using LoggerConfigurationExtensions = MyProject.Infrastructure.Logging.Extensions.LoggerConfigurationExtensions;

var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environments.Production;

Log.Logger = LoggerConfigurationExtensions.ConfigureMinimalLogging(environmentName);

try
{
    Log.Information("Starting web host");
    var builder = WebApplication.CreateBuilder(args);

    Log.Debug("Use Serilog");
    builder.Host.UseSerilog((context, _, loggerConfiguration) =>
    {
        LoggerConfigurationExtensions.SetupLogger(context.Configuration, loggerConfiguration);
    }, true);

    try
    {
        Log.Debug("Adding TimeProvider");
        builder.Services.AddSingleton(TimeProvider.System);

        Log.Debug("Adding persistence services");
        builder.Services.AddPersistence(builder.Configuration);

        Log.Debug("Adding identity services");
        builder.Services.AddIdentityServices(builder.Configuration);

        Log.Debug("Adding user context");
        builder.Services.AddUserContext();

        Log.Debug("Adding caching");
        builder.Services.AddCaching(builder.Configuration);

        Log.Debug("Adding cookie services");
        builder.Services.AddCookieServices();

        Log.Debug("Adding admin services");
        builder.Services.AddAdminServices();

        Log.Debug("Adding job scheduling");
        builder.Services.AddJobScheduling(builder.Configuration);

        Log.Debug("Adding permission-based authorization");
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Failed to configure essential services or dependencies.");
        throw;
    }

    Log.Debug("Adding Cors Feature");
    builder.Services.AddCors(builder.Configuration, builder.Environment);

    Log.Debug("Adding Routing => LowercaseUrls, Custom Constraints");
    builder.Services.AddRouting(options =>
    {
        options.LowercaseUrls = true;
        options.ConstraintMap.Add("roleName", typeof(RoleNameRouteConstraint));
        options.ConstraintMap.Add("jobId", typeof(JobIdRouteConstraint));
    });

    Log.Debug("Adding Controllers");
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    Log.Debug("Adding FluentValidation");
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    Log.Debug("Adding rate limiting");
    builder.Services.AddRateLimiting(builder.Configuration);

    Log.Debug("ConfigureServices => Setting AddHealthChecks");
    builder.Services.AddHealthChecks();

    Log.Debug("ConfigureServices => Setting AddApiDefinition");
    builder.AddOpenApiSpecification();

    var app = builder.Build();

    Log.Debug("Setting UseForwardedHeaders");
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    if (app.Environment.IsProduction())
    {
        app.Use(async (context, next) =>
        {
            context.Request.Scheme = "https";
            await next();
        });
    }
    else
    {
        Log.Debug("Setting Scalar OpenApi Documentation");
        app.UseOpenApiDocumentation();
    }

    Log.Debug("Setting security headers");
    app.UseSecurityHeaders();

    if (!app.Environment.IsDevelopment())
    {
        Log.Debug("Setting HSTS");
        app.UseHsts();
    }

    Log.Debug("Initializing database");
    await app.InitializeDatabaseAsync();

    Log.Debug("Setting UseCors");
    CorsExtensions.UseCors(app);

    Log.Debug("Setting UseSerilogRequestLogging");
    app.UseSerilogRequestLogging();

    Log.Debug("Setting UseMiddleware => ExceptionHandlingMiddleware");
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (!app.Environment.IsDevelopment())
    {
        Log.Debug("Setting UseHttpsRedirection");
        app.UseHttpsRedirection();
    }

    Log.Debug("Setting UseRouting");
    app.UseRouting();

    Log.Debug("Setting UseAuthentication");
    app.UseAuthentication();

    Log.Debug("Setting UseRateLimiter");
    app.UseRateLimiter();

    Log.Debug("Setting UseAuthorization");
    app.UseAuthorization();

    Log.Debug("Setting up job scheduling");
    app.UseJobScheduling();

    Log.Debug("Setting \"security\" measure => Redirect to YouTube video to confuse enemies");
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Value is "/")
        {
            context.Response.Redirect("https://www.youtube.com/watch?v=dQw4w9WgXcQ", permanent: false);
            return;
        }

        await next();
    });

    Log.Debug("Setting endpoints => MapControllers");
    app.MapControllers();

    Log.Debug("Setting endpoints => MapHealthChecks");
    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.Information("Shutting down application");
    await Log.CloseAndFlushAsync();
}
