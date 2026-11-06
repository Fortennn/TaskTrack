using TaskTracker.Application.BackgroundJobs;

namespace TaskTracker.Application.Interfaces;

public interface IJobStatusStore
{
    Guid CreateJob();
    BackgroundJobStatus? GetStatus(Guid jobId);
    void SetStatus(Guid jobId, BackgroundJobStatus status);
}
