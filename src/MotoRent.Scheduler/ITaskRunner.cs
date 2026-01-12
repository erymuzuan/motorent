namespace MotoRent.Scheduler;

/// <summary>
/// Interface for scheduled task runners.
/// </summary>
public interface ITaskRunner
{
    /// <summary>
    /// Name of the task runner.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Description of what this runner does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Run the scheduled task.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RunAsync(CancellationToken cancellationToken = default);
}
