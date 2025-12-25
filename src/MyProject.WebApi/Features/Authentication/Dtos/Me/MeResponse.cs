using System.ComponentModel;
using JetBrains.Annotations;

namespace MyProject.WebApi.Features.Authentication.Dtos.Me;

/// <summary>
/// Represents the authenticated user's information.
/// </summary>
public class MeResponse
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    [Description("The unique identifier of the user")]
    public Guid Id { get; [UsedImplicitly] init; }

    /// <summary>
    /// The username of the user.
    /// </summary>
    [Description("The username of the user")]
    public string Username { get; [UsedImplicitly] init; } = string.Empty;

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    [Description("The roles assigned to the user")]
    public IEnumerable<string> Roles { get; [UsedImplicitly] init; } = [];
}
