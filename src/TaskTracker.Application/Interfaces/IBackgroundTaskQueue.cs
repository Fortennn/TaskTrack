namespace TaskTracker.Application.Interfaces;

public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(Func<CancellationToken, Task> workItem);
    ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken);
}
