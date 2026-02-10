using System.Linq.Expressions;
using MyProject.Domain;
using static MyProject.Domain.ErrorMessages;
using MyProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyProject.Application.Persistence;
using MyProject.Infrastructure.Persistence.Extensions;

namespace MyProject.Infrastructure.Persistence;

/// <summary>
/// Generic EF Core implementation of <see cref="IBaseEntityRepository{TEntity}"/> with soft-delete support.
/// </summary>
/// <remarks>Pattern documented in src/backend/AGENTS.md — update both when changing.</remarks>
internal class BaseEntityRepository<TEntity>(
    MyProjectDbContext dbContext,
    ILogger<BaseEntityRepository<TEntity>> logger)
    : IBaseEntityRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly DbSet<TEntity> _dbSet = dbContext.Set<TEntity>();

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetByIdAsync(
        Guid id,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = asTracking
            ? _dbSet.AsTracking()
            : _dbSet.AsNoTracking();

        return await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        int pageNumber,
        int pageSize,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .OrderByDescending(e => e.CreatedAt)
            .Paginate(pageNumber, pageSize);

        query = asTracking
            ? query.AsTracking()
            : query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return Result<TEntity>.Success(entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to add entity of type {EntityType}.", typeof(TEntity).Name);
            return Result<TEntity>.Failure(Entity.AddFailed);
        }
    }

    /// <inheritdoc />
    public virtual void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual async Task<Result<TEntity>> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result<TEntity>.Failure(Entity.NotFound);
        }

        entity.SoftDelete();
        _dbSet.Update(entity);
        return Result<TEntity>.Success(entity);
    }

    /// <inheritdoc />
    public virtual async Task<Result<TEntity>> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return Result<TEntity>.Failure(Entity.NotDeleted);
        }

        entity.Restore();
        _dbSet.Update(entity);
        return Result<TEntity>.Success(entity);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(predicate, cancellationToken);
    }
}
