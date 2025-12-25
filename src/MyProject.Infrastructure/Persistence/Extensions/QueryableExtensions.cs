using System.Linq.Expressions;

namespace MyProject.Infrastructure.Persistence.Extensions;

/// <summary>
/// Extension methods for IQueryable to support conditional filtering
/// </summary>
public static class QueryableExtensions
{
    /// <param name="query">The query to extend</param>
    /// <typeparam name="T">The type of the entity</typeparam>
    extension<T>(IQueryable<T> query)
    {
        /// <summary>
        /// Conditionally applies a Where clause if the condition is not null
        /// </summary>
        /// <typeparam name="TValue">The type of the condition value</typeparam>
        /// <param name="condition">The condition value (if null, no filter is applied)</param>
        /// <param name="predicate">The predicate to apply when condition is not null</param>
        /// <returns>The filtered query if condition is not null, otherwise the original query</returns>
        public IQueryable<T> ConditionalWhere<TValue>(TValue? condition,
            Expression<Func<T, bool>> predicate)
            where TValue : struct
        {
            return condition.HasValue ? query.Where(predicate) : query;
        }

        /// <summary>
        /// Conditionally applies a Where clause if the condition is not null or empty
        /// </summary>
        /// <param name="condition">The condition value (if null or empty, no filter is applied)</param>
        /// <param name="predicate">The predicate to apply when condition is not null or empty</param>
        /// <returns>The filtered query if condition is not null or empty, otherwise the original query</returns>
        public IQueryable<T> ConditionalWhere(string? condition,
            Expression<Func<T, bool>> predicate)
        {
            return !string.IsNullOrEmpty(condition) ? query.Where(predicate) : query;
        }

        /// <summary>
        /// Conditionally applies a Where clause if the collection has at least one value
        /// </summary>
        /// <typeparam name="TValue">The element type of the collection</typeparam>
        /// <param name="values">The values collection (if null or empty, no filter is applied)</param>
        /// <param name="predicate">The predicate to apply when values has at least one element</param>
        /// <returns>The filtered query if values has elements, otherwise the original query</returns>
        public IQueryable<T> ConditionalWhere<TValue>(IEnumerable<TValue>? values,
            Expression<Func<T, bool>> predicate)
            where TValue : struct
        {
            return values != null && values.Any() ? query.Where(predicate) : query;
        }
    }
}
