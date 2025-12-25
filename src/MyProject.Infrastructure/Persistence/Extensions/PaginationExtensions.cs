using MyProject.Infrastructure.Persistence.Exceptions;

namespace MyProject.Infrastructure.Persistence.Extensions;

/// <summary>
/// Provides extension methods for implementing pagination on IQueryable collections
/// </summary>
public static class PaginationExtensions
{
    private const int MaxPageSize = 100;

    /// <summary>
    /// Applies pagination to an IQueryable collection
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection</typeparam>
    /// <param name="ts">The IQueryable collection to paginate</param>
    /// <param name="pageNumber">The page number to retrieve (1-based indexing)</param>
    /// <param name="pageSize">The number of items per page (maximum 100)</param>
    /// <returns>A paginated IQueryable collection</returns>
    /// <exception cref="PaginationException">Thrown when page number is less than or equal to 0, or when page size is less than or equal to 0</exception>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> ts, int pageNumber, int pageSize)
    {
        if (pageNumber <= 0) throw new PaginationException(nameof(pageNumber), "Page number must be positive.");

        pageSize = pageSize switch
        {
            <= 0 => throw new PaginationException(nameof(pageSize), "Page size must be positive."),
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };

        return ts.Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }
}
