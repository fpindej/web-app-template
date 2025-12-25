using System.Linq.Expressions;
using MyProject.Domain;
using MyProject.Domain.Entities;

namespace MyProject.Application.Persistence;

/// <summary>
/// Generic repository interface for entities supporting soft delete and basic CRUD operations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IBaseEntityRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// Gets an entity by its unique identifier, excluding soft-deleted entities.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="asTracking">Whether to track the entity for changes.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The entity if found and not deleted; otherwise, null.</returns>
    Task<TEntity?> GetByIdAsync(Guid id, bool asTracking = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of all entities, excluding soft-deleted entities.
    /// </summary>
    /// <param name="pageNumber">The page number for pagination.</param>
    /// <param name="pageSize">The page size for pagination.</param>
    /// <param name="asTracking">Whether to track the entities for changes.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A read-only list of entities.</returns>
    Task<IReadOnlyList<TEntity>> GetAllAsync(
        int pageNumber,
        int pageSize,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result"/> containing the added entity or an error.</returns>
    Task<Result<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    void Update(TEntity entity);

    /// <summary>
    /// Soft deletes an entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{TEntity}"/> indicating the success or failure of the operation.</returns>
    Task<Result<TEntity>> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a soft-deleted entity by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A <see cref="Result{TEntity}"/> indicating the success or failure of the operation.</returns>
    Task<Result<TEntity>> RestoreAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any entity matches the given predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match entities.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if any entity matches; otherwise, false.</returns>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}
