using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using MyProject.Component.Tests.Fixtures;
using MyProject.Infrastructure.Caching.Services;
using MyProject.Infrastructure.Cryptography;
using MyProject.Infrastructure.Features.Authentication.Extensions;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Component.Tests.Validation;

public class SecurityStampValidationTests
{
    private const string StampClaimType = "security_stamp";

    private readonly UserManager<ApplicationUser> _userManager = IdentityMockHelpers.CreateMockUserManager();

    private TokenValidatedContext CreateContext(params Claim[] claims)
    {
        var services = new ServiceCollection();
        services.AddSingleton<HybridCache>(new NoOpHybridCache());
        services.AddSingleton(_userManager);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var scheme = new AuthenticationScheme(
            JwtBearerDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            typeof(JwtBearerHandler));

        return new TokenValidatedContext(httpContext, scheme, new JwtBearerOptions())
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
    }

    [Fact]
    public async Task MissingStampClaim_FailsAuthentication()
    {
        // Fail closed: tokens without the security stamp claim must be rejected
        var context = CreateContext(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        await ServiceCollectionExtensions.ValidateSecurityStampAsync(context, StampClaimType);

        Assert.NotNull(context.Result?.Failure);
    }

    [Fact]
    public async Task MatchingStamp_DoesNotFail()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, SecurityStamp = "current-stamp" };
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);

        var context = CreateContext(
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(StampClaimType, HashHelper.Sha256("current-stamp")));

        await ServiceCollectionExtensions.ValidateSecurityStampAsync(context, StampClaimType);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task ChangedStamp_FailsAuthentication()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser { Id = userId, SecurityStamp = "rotated-stamp" };
        _userManager.FindByIdAsync(userId.ToString()).Returns(user);

        var context = CreateContext(
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(StampClaimType, HashHelper.Sha256("old-stamp")));

        await ServiceCollectionExtensions.ValidateSecurityStampAsync(context, StampClaimType);

        Assert.NotNull(context.Result?.Failure);
    }

    [Fact]
    public async Task UserNotFound_FailsAuthentication()
    {
        var userId = Guid.NewGuid();
        _userManager.FindByIdAsync(userId.ToString()).Returns((ApplicationUser?)null);

        var context = CreateContext(
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(StampClaimType, HashHelper.Sha256("any-stamp")));

        await ServiceCollectionExtensions.ValidateSecurityStampAsync(context, StampClaimType);

        Assert.NotNull(context.Result?.Failure);
    }

    [Fact]
    public async Task InvalidUserIdClaim_FailsAuthentication()
    {
        var context = CreateContext(
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
            new Claim(StampClaimType, HashHelper.Sha256("any-stamp")));

        await ServiceCollectionExtensions.ValidateSecurityStampAsync(context, StampClaimType);

        Assert.NotNull(context.Result?.Failure);
    }
}
