using System.ComponentModel;
using JetBrains.Annotations;

namespace MyProject.WebApi.Shared;

/// <summary>
/// Base class for all paginated requests
/// </summary>
[Description("Base request with pagination parameters")]
public abstract class PaginatedRequest
{
    /// <summary>
    /// The page number to retrieve (1-based)
    /// </summary>
    [Description("The page number to retrieve (1-based indexing)")]
    public int PageNumber { get; [UsedImplicitly] set; } = 1;

    /// <summary>
    /// The number of items per page
    /// </summary>
    [Description("The number of items per page (maximum 100)")]
    public int PageSize { get; [UsedImplicitly] set; } = 10;
}
