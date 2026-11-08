using TaskTracker.Api.BackgroundJobs;
using TaskTracker.Application.BackgroundJobs;
using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Projects;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Services;
using TaskTracker.Application.Validation;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Enums;

namespace TaskTracker.UnitTests;

public sealed class TaskServiceBackgroundJobTests
{
    [Fact]
    public async Task CreateAsync_QueuesBackgroundJob_WithoutImmediateSenderCall_AndTransitionsStatus()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        var fakeTaskRepo = new FakeTaskRepository();
        var fakeProjectRepo = new FakeProjectRepository([projectId]);
        var fakeUserRepo = new FakeUserRepository(new User("Dev", "dev@example.com", Role.Member), assigneeId);

        var queueCapture = new CapturingTaskQueue();
        var statusStore = new InMemoryJobStatusStore();
        var recordingSender = new RecordingNotificationSender();
        var validator = new CoverImageUploadValidator(5);

        var service = new TaskService(
            fakeTaskRepo,
            fakeProjectRepo,
            fakeUserRepo,
            new FakeCoverImageStorage(),
            validator,
            queueCapture,
            statusStore,
            recordingSender,
            new FakeGeocodingService());

        var request = new CreateTaskRequest
        {
            Title = "Background Job Task",
            ProjectId = projectId,
            Description = "Testing async background queue",
            Priority = Priority.High,
            AssigneeId = assigneeId
        };

        // Act - Step 1: CreateAsync
        var result = await service.CreateAsync(request, CancellationToken.None);

        // Assert - Step 2: Task created and jobId returned, status is Pending
        Assert.NotNull(result);
        Assert.NotNull(result.Task);
        Assert.NotEqual(Guid.Empty, result.JobId);
        Assert.Equal(BackgroundJobStatus.Pending, statusStore.GetStatus(result.JobId));

        // Assert - Step 3: Notification sender has NOT been called yet
        Assert.Equal(0, recordingSender.CallCount);
        Assert.NotNull(queueCapture.LastWorkItem);

        // Act - Step 4: Manually invoke the captured delegate
        await queueCapture.LastWorkItem!(CancellationToken.None);

        // Assert - Step 5: Sender was called exactly once, status is Done
        Assert.Equal(1, recordingSender.CallCount);
        Assert.Equal(BackgroundJobStatus.Done, statusStore.GetStatus(result.JobId));
    }

    private sealed class CapturingTaskQueue : IBackgroundTaskQueue
    {
        public Func<CancellationToken, Task>? LastWorkItem { get; private set; }

        public ValueTask QueueAsync(Func<CancellationToken, Task> workItem)
        {
            LastWorkItem = workItem;
            return ValueTask.CompletedTask;
        }

        public ValueTask<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(LastWorkItem ?? (_ => Task.CompletedTask));
        }
    }

    private sealed class RecordingNotificationSender : ITaskNotificationSender
    {
        public int CallCount { get; private set; }
        public Guid? LastTaskId { get; private set; }
        public string? LastRecipientEmail { get; private set; }

        public Task SendAsync(Guid taskId, string taskTitle, string? recipientEmail, CancellationToken cancellationToken)
        {
            CallCount++;
            LastTaskId = taskId;
            LastRecipientEmail = recipientEmail;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        private TaskItem? _saved;

        public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_saved?.Id == id ? _saved : null);

        public Task<TaskDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_saved is null || _saved.Id != id) return Task.FromResult<TaskDetailDto?>(null);

            var detail = new TaskDetailDto(
                _saved.Id,
                _saved.Title,
                _saved.Description,
                _saved.Status,
                _saved.Priority,
                _saved.DueDate,
                null,
                _saved.Location,
                _saved.Latitude,
                _saved.Longitude,
                _saved.ProjectId,
                "Project",
                _saved.AssigneeId,
                "Dev",
                _saved.CreatedAt,
                []);
            return Task.FromResult<TaskDetailDto?>(detail);
        }

        public Task<PagedResult<TaskDto>> GetPagedAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
        {
            _saved = task;
            return Task.CompletedTask;
        }

        public void Update(TaskItem task) { }

        public void Delete(TaskItem task) { }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_saved?.Id == id);

        public Task<IReadOnlyList<CommentDto>> GetCommentsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CommentDto>>([]);

        public Task AddCommentAsync(Comment comment, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }

    private sealed class FakeProjectRepository(IEnumerable<Guid> existingIds) : IProjectRepository
    {
        private readonly HashSet<Guid> _existing = [.. existingIds];

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ProjectDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResult<ProjectDto>> GetPagedAsync(ProjectQueryParameters parameters, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddAsync(Project project, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public void Update(Project project) { }

        public void Delete(Project project) { }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_existing.Contains(id));

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }

    private sealed class FakeUserRepository(User user, Guid expectedId) : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == expectedId ? user : null);

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == expectedId);
    }

    private sealed class FakeCoverImageStorage : ICoverImageStorage
    {
        public Task<string> SaveAsync(Stream content, string originalFileName, CancellationToken cancellationToken = default) =>
            Task.FromResult("/uploads/test.png");

        public Task<CoverImageFile?> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<CoverImageFile?>(null);

        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeGeocodingService : IGeocodingService
    {
        public Task<Coordinates?> GeocodeAsync(string address, CancellationToken cancellationToken = default) =>
            Task.FromResult<Coordinates?>(new Coordinates(50.4501, 30.5234));
    }
}
