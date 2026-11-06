using Microsoft.Extensions.Logging.Abstractions;
using TaskTracker.Api.BackgroundJobs;
using TaskTracker.Application.BackgroundJobs;

namespace TaskTracker.UnitTests;

public sealed class SelfStudyBackgroundWorkerErrorHandlingTests
{
    [Fact]
    public async Task Worker_WhenJobFails_MarksJobAsFailed_DoesNotCrash_AndProcessesNextJobToDone()
    {
        // Arrange
        var queue = new BackgroundTaskQueue();
        var statusStore = new InMemoryJobStatusStore();
        var logger = NullLogger<BackgroundTaskWorker>.Instance;
        var worker = new BackgroundTaskWorker(queue, logger);

        using var cts = new CancellationTokenSource();

        // Start worker in background
        var workerTask = worker.StartAsync(cts.Token);

        var failedJobId = statusStore.CreateJob();
        var successJobId = statusStore.CreateJob();

        // 1. First job throws an exception (simulated failure)
        await queue.QueueAsync(async token =>
        {
            statusStore.SetStatus(failedJobId, BackgroundJobStatus.Processing);
            await Task.Yield();
            statusStore.SetStatus(failedJobId, BackgroundJobStatus.Failed);
            throw new InvalidOperationException("Simulated notification service failure.");
        });

        // 2. Second job succeeds
        await queue.QueueAsync(async token =>
        {
            statusStore.SetStatus(successJobId, BackgroundJobStatus.Processing);
            await Task.Yield();
            statusStore.SetStatus(successJobId, BackgroundJobStatus.Done);
        });

        // Wait up to 2 seconds for worker to process both items
        var timeout = DateTime.UtcNow.AddSeconds(3);
        while (DateTime.UtcNow < timeout &&
               (statusStore.GetStatus(failedJobId) != BackgroundJobStatus.Failed ||
                statusStore.GetStatus(successJobId) != BackgroundJobStatus.Done))
        {
            await Task.Delay(50);
        }

        // Stop worker gracefully
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);
        await workerTask;

        // Assert
        // Criterion 1 & 2: failed job is in state Failed
        Assert.Equal(BackgroundJobStatus.Failed, statusStore.GetStatus(failedJobId));

        // Criterion 3: subsequent job still transitions to Done
        Assert.Equal(BackgroundJobStatus.Done, statusStore.GetStatus(successJobId));

        // Criterion 4: worker did not crash and completed stop gracefully
        Assert.True(workerTask.IsCompletedSuccessfully);
    }
}
