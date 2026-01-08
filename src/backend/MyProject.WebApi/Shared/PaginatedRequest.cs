using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace MyProject.WebApi.Shared;

/// <summary>
/// Base class for all paginated requests.
/// </summary>
public abstract class PaginatedRequest
{
    /// <summary>
    /// The page number to retrieve (1-based).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; [UsedImplicitly] set; } = 1;

    /// <summary>
    /// The number of items per page (maximum 100).
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; [UsedImplicitly] set; } = 10;
}
