using Microsoft.Extensions.Logging;

namespace MyProject.Infrastructure.Features.Jobs.Examples;

/// <summary>
/// Example fire-and-forget background job demonstrating the Hangfire one-time execution pattern.
/// <para>
/// Unlike recurring jobs (<see cref="IRecurringJobDefinition"/>), fire-and-forget jobs are
/// ad-hoc — they are enqueued on demand from services or controllers via <c>IBackgroundJobClient</c>
/// and executed once in the background. Hangfire handles persistence, retries, and DI scoping.
/// </para>
/// <para>
/// Usage — inject <c>IBackgroundJobClient</c> into any service or controller:
/// <code>
/// // Fire-and-forget (immediate background execution)
/// backgroundJobClient.Enqueue&lt;ExampleFireAndForgetJob&gt;(
///     job => job.ExecuteAsync("Hello from background!"));
///
/// // Delayed (execute after a time span)
/// backgroundJobClient.Schedule&lt;ExampleFireAndForgetJob&gt;(
///     job => job.ExecuteAsync("Hello after delay!"),
///     TimeSpan.FromMinutes(30));
/// </code>
/// </para>
/// <para>
/// <b>Remove this file when you no longer need the example.</b>
/// </para>
/// </summary>
internal sealed class ExampleFireAndForgetJob(ILogger<ExampleFireAndForgetJob> logger)
{
    /// <summary>
    /// Executes the example background task. In a real application, this would send an email,
    /// process a file, call an external API, etc.
    /// <para>
    /// All parameters must be JSON-serializable — Hangfire persists them to the database.
    /// Do not pass non-serializable objects (e.g. <c>IServiceProvider</c>, <c>HttpContext</c>).
    /// </para>
    /// </summary>
    /// <param name="message">An example serializable argument.</param>
    public Task ExecuteAsync(string message)
    {
        logger.LogInformation("Fire-and-forget example executed with message: '{Message}'", message);
        return Task.CompletedTask;
    }
}
