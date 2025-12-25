namespace MyProject.Application.Persistence;

/// <summary>
/// Defines operations for managing database transactions
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes made in the context to the database
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation if needed</param>
    /// <returns>The number of affected records in the database</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the change tracker, removing all tracked entities
    /// </summary>
    void ClearChangeTracker();

    /// <summary>
    /// Begins a new database transaction asynchronously
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation if needed</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction and saves changes to the database
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation if needed</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction and discards changes
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation if needed</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
