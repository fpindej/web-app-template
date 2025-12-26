using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyProject.Application.Caching;
using MyProject.Application.Caching.Constants;
using MyProject.Infrastructure.Features.Authentication.Models;

namespace MyProject.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that automatically invalidates user cache when a user or their roles are modified.
/// </summary>
internal class UserCacheInvalidationInterceptor(ICacheService cacheService) : SaveChangesInterceptor
{
    private readonly List<Guid> _userIdsToInvalidate = [];

    /// <summary>
    /// Intercepts the saving changes operation to detect user modifications.
    /// </summary>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        _userIdsToInvalidate.Clear();

        var context = eventData.Context;
        if (context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var modifiedUsers = context.ChangeTracker.Entries<ApplicationUser>()
            .Where(e => e.State == EntityState.Modified)
            .Select(e => e.Entity.Id);

        _userIdsToInvalidate.AddRange(modifiedUsers);

        var modifiedUserRoles = context.ChangeTracker.Entries<IdentityUserRole<Guid>>()
            .Where(e => e.State is EntityState.Added or EntityState.Deleted or EntityState.Modified)
            .Select(e => e.Entity.UserId);

        _userIdsToInvalidate.AddRange(modifiedUserRoles);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Intercepts the saved changes operation to invalidate cache after successful save.
    /// </summary>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_userIdsToInvalidate.Count <= 0)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        await Task.WhenAll(_userIdsToInvalidate
            .Distinct()
            .Select(userId => cacheService.RemoveAsync(CacheKeys.User(userId), cancellationToken)));

        _userIdsToInvalidate.Clear();

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
