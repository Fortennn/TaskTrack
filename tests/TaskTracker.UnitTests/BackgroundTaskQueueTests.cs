using TaskTracker.Api.BackgroundJobs;

namespace TaskTracker.UnitTests;

public sealed class BackgroundTaskQueueTests
{
    [Fact]
    public async Task QueueAsync_And_DequeueAsync_ReturnsSameWorkItem()
    {
        // Arrange
        var queue = new BackgroundTaskQueue();
        Func<CancellationToken, Task> workItem = _ => Task.CompletedTask;

        // Act
        await queue.QueueAsync(workItem);
        var dequeued = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.Same(workItem, dequeued);
    }
}
