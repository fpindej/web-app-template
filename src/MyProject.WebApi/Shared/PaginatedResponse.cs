using System.ComponentModel;

namespace MyProject.WebApi.Shared;

/// <summary>
/// Base class for all paginated responses
/// </summary>
[Description("Base response with pagination metadata")]
public abstract class PaginatedResponse
{
    /// <summary>
    /// The total number of items (across all pages)
    /// </summary>
    [Description("The total number of items across all pages")]
    public int TotalCount { get; set; }
    
    /// <summary>
    /// The current page number
    /// </summary>
    [Description("The current page number")]
    public int PageNumber { get; set; }
    
    /// <summary>
    /// The number of items per page
    /// </summary>
    [Description("The number of items per page")]
    public int PageSize { get; set; }
    
    /// <summary>
    /// The total number of pages
    /// </summary>
    [Description("The total number of pages")]
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    
    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    [Description("Indicates if there is a previous page")]
    public bool HasPreviousPage => PageNumber > 1;
    
    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    [Description("Indicates if there is a next page")]
    public bool HasNextPage => PageNumber < TotalPages;
}
