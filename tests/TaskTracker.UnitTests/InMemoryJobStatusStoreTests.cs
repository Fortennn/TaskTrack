using TaskTracker.Api.BackgroundJobs;
using TaskTracker.Application.BackgroundJobs;

namespace TaskTracker.UnitTests;

public sealed class InMemoryJobStatusStoreTests
{
    [Fact]
    public void CreateJob_ShouldInitializeStatusToPending()
    {
        // Arrange
        var store = new InMemoryJobStatusStore();

        // Act
        var jobId = store.CreateJob();
        var status = store.GetStatus(jobId);

        // Assert
        Assert.NotEqual(Guid.Empty, jobId);
        Assert.Equal(BackgroundJobStatus.Pending, status);
    }

    [Fact]
    public void SetStatus_ShouldUpdateExistingJobStatus()
    {
        // Arrange
        var store = new InMemoryJobStatusStore();
        var jobId = store.CreateJob();

        // Act
        store.SetStatus(jobId, BackgroundJobStatus.Done);
        var status = store.GetStatus(jobId);

        // Assert
        Assert.Equal(BackgroundJobStatus.Done, status);
    }

    [Fact]
    public void GetStatus_NonExistentJobId_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryJobStatusStore();
        var randomId = Guid.NewGuid();

        // Act
        var status = store.GetStatus(randomId);

        // Assert
        Assert.Null(status);
    }
}
