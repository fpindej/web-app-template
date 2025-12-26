using System.Linq.Expressions;
using MyProject.Domain;
using MyProject.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MyProject.Application.Persistence;
using MyProject.Infrastructure.Persistence.Extensions;

namespace MyProject.Infrastructure.Persistence;

internal class BaseEntityRepository<TEntity>(MyProjectDbContext dbContext)
    : IBaseEntityRepository<TEntity>
    where TEntity : BaseEntity
{
    private readonly DbSet<TEntity> _dbSet = dbContext.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(
        Guid id,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = asTracking
            ? _dbSet.AsTracking()
            : _dbSet.AsNoTracking();

        return await query.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        int pageNumber,
        int pageSize,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.CreatedAt)
            .Paginate(pageNumber, pageSize);

        query = asTracking
            ? query.AsTracking()
            : query.AsNoTracking();

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return Result<TEntity>.Success(entity);
        }
        catch (Exception ex)
        {
            return Result<TEntity>.Failure($"Failed to add entity: {ex.Message}");
        }
    }

    public virtual void Update(TEntity entity)
    {
        _dbSet.Update(entity);
    }

    public virtual async Task<Result<TEntity>> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return Result<TEntity>.Failure($"Entity with ID {id} not found or already deleted.");
        }

        entity.SoftDelete();
        _dbSet.Update(entity);
        return Result<TEntity>.Success(entity);
    }

    public virtual async Task<Result<TEntity>> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return Result<TEntity>.Failure($"Entity with ID {id} not found or not deleted.");
        }

        entity.Restore();
        _dbSet.Update(entity);
        return Result<TEntity>.Success(entity);
    }

    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }
}
