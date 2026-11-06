using TaskTracker.Application.Interfaces;

namespace TaskTracker.Api.BackgroundJobs;

public sealed class BackgroundTaskWorker(
    IBackgroundTaskQueue taskQueue,
    ILogger<BackgroundTaskWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await taskQueue.DequeueAsync(stoppingToken);
                await workItem(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "A background job failed.");
            }
        }
    }
}
