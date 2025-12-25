using MyProject.Application;
using Microsoft.EntityFrameworkCore.Storage;
using MyProject.Application.Persistence;

namespace MyProject.Infrastructure.Persistence;

internal class UnitOfWork(MyProjectDbContext dbContext)
    : IUnitOfWork
{
    private IDbContextTransaction _transaction = null!;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Logging implicitly in the error handling middleware
            throw new Exception("An error occurred while saving changes.", ex);
        }
    }

    public void ClearChangeTracker()
    {
        dbContext.ChangeTracker.Clear();
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Logging implicitly in the error handling middleware
            throw new Exception("An error occurred while beginning the transaction.", ex);
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await RollbackTransactionAsync(cancellationToken);
            // Logging implicitly in the error handling middleware
            throw new Exception("An error occurred while committing the transaction.", ex);
        }
        finally
        {
            await _transaction.DisposeAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Logging implicitly in the error handling middleware
            throw new Exception("An error occurred while rolling back the transaction.", ex);
        }
        finally
        {
            await _transaction.DisposeAsync();
            ClearChangeTracker();
        }
    }
}
