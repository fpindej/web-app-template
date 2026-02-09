namespace MyProject.Application.Features.Admin.Dtos;

/// <summary>
/// Output for paginated admin user list results.
/// </summary>
/// <param name="Users">The list of users for the current page.</param>
/// <param name="TotalCount">The total number of users matching the query.</param>
/// <param name="PageNumber">The current page number.</param>
/// <param name="PageSize">The number of items per page.</param>
public record AdminUserListOutput(
    IReadOnlyList<AdminUserOutput> Users,
    int TotalCount,
    int PageNumber,
    int PageSize
);
