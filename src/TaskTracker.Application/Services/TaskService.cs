using TaskTracker.Application.BackgroundJobs;
using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Validation;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICoverImageStorage _coverImageStorage;
    private readonly CoverImageUploadValidator _coverImageUploadValidator;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IJobStatusStore _jobStatusStore;
    private readonly ITaskNotificationSender _notificationSender;
    private readonly IGeocodingService _geocodingService;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        ICoverImageStorage coverImageStorage,
        CoverImageUploadValidator coverImageUploadValidator,
        IBackgroundTaskQueue taskQueue,
        IJobStatusStore jobStatusStore,
        ITaskNotificationSender notificationSender,
        IGeocodingService geocodingService)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _coverImageStorage = coverImageStorage;
        _coverImageUploadValidator = coverImageUploadValidator;
        _taskQueue = taskQueue;
        _jobStatusStore = jobStatusStore;
        _notificationSender = notificationSender;
        _geocodingService = geocodingService;
    }

    public async Task<TaskDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetDetailByIdAsync(id, cancellationToken);
        if (task is null)
        {
            throw new NotFoundException($"Task with Id '{id}' does not exist.");
        }

        return task;
    }

    public async Task<PagedResult<TaskDto>> GetPagedAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        return await _taskRepository.GetPagedAsync(parameters, cancellationToken);
    }

    public async Task<CreateTaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new NotFoundException($"Project with Id '{request.ProjectId}' does not exist.");
        }

        User? assignee = null;
        if (request.AssigneeId.HasValue)
        {
            var assigneeExists = await _userRepository.ExistsAsync(request.AssigneeId.Value, cancellationToken);
            if (!assigneeExists)
            {
                throw new NotFoundException($"Assignee User with Id '{request.AssigneeId.Value}' does not exist.");
            }

            assignee = await _userRepository.GetByIdAsync(request.AssigneeId.Value, cancellationToken);
        }

        Coordinates? coordinates = null;
        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            coordinates = await _geocodingService.GeocodeAsync(request.Location, cancellationToken);
        }

        var task = new TaskItem(
            request.Title,
            request.ProjectId,
            request.Description,
            request.Priority,
            request.DueDate,
            request.AssigneeId,
            request.Location,
            coordinates?.Latitude,
            coordinates?.Longitude);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var createdDto = await _taskRepository.GetDetailByIdAsync(task.Id, cancellationToken);

        var jobId = _jobStatusStore.CreateJob();
        var statusStore = _jobStatusStore;
        var sender = _notificationSender;
        var taskId = task.Id;
        var taskTitle = task.Title;
        var recipientEmail = assignee?.Email;

        await _taskQueue.QueueAsync(stoppingToken =>
            ProcessNotificationJobAsync(
                jobId,
                taskId,
                taskTitle,
                recipientEmail,
                statusStore,
                sender,
                stoppingToken));

        return new CreateTaskResponse(createdDto!, jobId);
    }

    public async Task<CreateTaskResponse> CreateForProjectAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var taskRequest = new CreateTaskRequest
        {
            Title = request.Title,
            ProjectId = projectId,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
            AssigneeId = request.AssigneeId,
            Location = request.Location
        };
        return await CreateAsync(taskRequest, cancellationToken);
    }

    private static async Task ProcessNotificationJobAsync(
        Guid jobId,
        Guid taskId,
        string taskTitle,
        string? recipientEmail,
        IJobStatusStore statusStore,
        ITaskNotificationSender sender,
        CancellationToken cancellationToken)
    {
        statusStore.SetStatus(
            jobId,
            BackgroundJobStatus.Processing);

        try
        {
            await sender.SendAsync(
                taskId,
                taskTitle,
                recipientEmail,
                cancellationToken);

            statusStore.SetStatus(
                jobId,
                BackgroundJobStatus.Done);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            statusStore.SetStatus(
                jobId,
                BackgroundJobStatus.Cancelled);
            throw;
        }
        catch
        {
            statusStore.SetStatus(
                jobId,
                BackgroundJobStatus.Failed);
            throw;
        }
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var taskExists = await _taskRepository.ExistsAsync(taskId, cancellationToken);
        if (!taskExists)
        {
            throw new NotFoundException($"Task with Id '{taskId}' does not exist.");
        }

        return await _taskRepository.GetCommentsByTaskIdAsync(taskId, cancellationToken);
    }

    public async Task<CommentDto> AddCommentAsync(Guid taskId, CreateCommentRequest request, CancellationToken cancellationToken = default)
    {
        var taskExists = await _taskRepository.ExistsAsync(taskId, cancellationToken);
        if (!taskExists)
        {
            throw new NotFoundException($"Task with Id '{taskId}' does not exist.");
        }

        var authorExists = await _userRepository.ExistsAsync(request.AuthorId, cancellationToken);
        if (!authorExists)
        {
            throw new NotFoundException($"User with Id '{request.AuthorId}' does not exist.");
        }

        var comment = new Comment(taskId, request.AuthorId, request.Text.Trim());
        await _taskRepository.AddCommentAsync(comment, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return new CommentDto(
            comment.Id,
            comment.TaskId,
            comment.AuthorId,
            string.Empty,
            comment.Text,
            comment.CreatedAt);
    }

    public async Task<TaskDetailDto> UploadCoverAsync(Guid id, CoverImageUpload? upload, CancellationToken cancellationToken = default)
    {
        var extension = _coverImageUploadValidator.ValidateAndGetExtension(upload);

        var taskItem = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (taskItem is null)
        {
            throw new NotFoundException($"Task with Id '{id}' does not exist.");
        }

        var previousCover = taskItem.CoverImagePath;
        var newCover = await _coverImageStorage.SaveAsync(upload!.Content, extension, cancellationToken);

        try
        {
            taskItem.CoverImagePath = newCover;
            _taskRepository.Update(taskItem);
            await _taskRepository.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Компенсаційна дія: видалення щойно створеного файлу у разі збою запису в БД
            await _coverImageStorage.DeleteAsync(newCover, cancellationToken);
            throw;
        }

        // Якщо в БД успішно оновлено нове посилання — безпечно видаляємо старий файл
        if (!string.IsNullOrWhiteSpace(previousCover))
        {
            await _coverImageStorage.DeleteAsync(previousCover, cancellationToken);
        }

        return (await _taskRepository.GetDetailByIdAsync(taskItem.Id, cancellationToken))!;
    }

    public async Task<CoverImageFile> GetCoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var taskItem = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (taskItem is null)
        {
            throw new NotFoundException($"Task with Id '{id}' does not exist.");
        }

        if (string.IsNullOrWhiteSpace(taskItem.CoverImagePath))
        {
            throw new NotFoundException($"Task with Id '{id}' does not have a cover image.");
        }

        return await _coverImageStorage.OpenReadAsync(taskItem.CoverImagePath, cancellationToken)
            ?? throw new NotFoundException($"The cover image for task with Id '{id}' is unavailable.");
    }

    public async Task DeleteCoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var taskItem = await _taskRepository.GetByIdAsync(id, cancellationToken);
        if (taskItem is null)
        {
            throw new NotFoundException($"Task with Id '{id}' does not exist.");
        }

        if (string.IsNullOrWhiteSpace(taskItem.CoverImagePath))
        {
            // Ідемпотентна поведінка: якщо обкладинка вже відсутня, повертаємо успіх без помилки
            return;
        }

        var coverToDelete = taskItem.CoverImagePath;
        taskItem.CoverImagePath = null;
        _taskRepository.Update(taskItem);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        await _coverImageStorage.DeleteAsync(coverToDelete, cancellationToken);
    }
}
