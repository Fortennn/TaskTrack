using System.Threading.Channels;
using TaskTracker.Application.Interfaces;

namespace TaskTracker.Api.BackgroundJobs;

public sealed class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private const int Capacity = 100;
    private readonly Channel<Func<CancellationToken, Task>> _queue =
        Channel.CreateBounded<Func<CancellationToken, Task>>(
            new BoundedChannelOptions(Capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

    public ValueTask QueueAsync(Func<CancellationToken, Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        return _queue.Writer.WriteAsync(workItem);
    }

    public ValueTask<Func<CancellationToken, Task>> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return _queue.Reader.ReadAsync(cancellationToken);
    }
}
