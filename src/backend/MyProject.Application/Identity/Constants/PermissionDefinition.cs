namespace MyProject.Application.Identity.Constants;

/// <summary>
/// Describes a single permission with its string value and logical category.
/// </summary>
/// <param name="Value">The permission claim value (e.g. <c>"users.view"</c>).</param>
/// <param name="Category">The logical grouping (e.g. <c>"Users"</c>, <c>"Roles"</c>).</param>
public record PermissionDefinition(string Value, string Category);
