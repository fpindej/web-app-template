using MyProject.Application.Features.Jobs.Dtos;
using MyProject.WebApi.Features.Admin.Dtos.Jobs;

namespace MyProject.WebApi.Features.Admin;

/// <summary>
/// Maps between job Application layer DTOs and WebApi response DTOs.
/// </summary>
internal static class JobsMapper
{
    /// <summary>
    /// Maps a <see cref="RecurringJobOutput"/> to a <see cref="RecurringJobResponse"/>.
    /// </summary>
    public static RecurringJobResponse ToResponse(this RecurringJobOutput output) => new()
    {
        Id = output.Id,
        Cron = output.Cron,
        NextExecution = output.NextExecution,
        LastExecution = output.LastExecution,
        LastStatus = output.LastStatus,
        IsPaused = output.IsPaused,
        CreatedAt = output.CreatedAt
    };

    /// <summary>
    /// Maps a <see cref="RecurringJobDetailOutput"/> to a <see cref="RecurringJobDetailResponse"/>.
    /// </summary>
    public static RecurringJobDetailResponse ToResponse(this RecurringJobDetailOutput output) => new()
    {
        Id = output.Id,
        Cron = output.Cron,
        NextExecution = output.NextExecution,
        LastExecution = output.LastExecution,
        LastStatus = output.LastStatus,
        IsPaused = output.IsPaused,
        CreatedAt = output.CreatedAt,
        ExecutionHistory = output.ExecutionHistory.Select(e => e.ToResponse()).ToList()
    };

    /// <summary>
    /// Maps a <see cref="JobExecutionOutput"/> to a <see cref="JobExecutionResponse"/>.
    /// </summary>
    public static JobExecutionResponse ToResponse(this JobExecutionOutput output) => new()
    {
        JobId = output.JobId,
        Status = output.Status,
        StartedAt = output.StartedAt,
        Duration = output.Duration,
        Error = output.Error
    };
}
