using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
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

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository,
        ICoverImageStorage coverImageStorage,
        CoverImageUploadValidator coverImageUploadValidator)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
        _coverImageStorage = coverImageStorage;
        _coverImageUploadValidator = coverImageUploadValidator;
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

    public async Task<TaskDetailDto> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            throw new NotFoundException($"Project with Id '{request.ProjectId}' does not exist.");
        }

        if (request.AssigneeId.HasValue)
        {
            var assigneeExists = await _userRepository.ExistsAsync(request.AssigneeId.Value, cancellationToken);
            if (!assigneeExists)
            {
                throw new NotFoundException($"Assignee User with Id '{request.AssigneeId.Value}' does not exist.");
            }
        }

        var task = new TaskItem(
            request.Title,
            request.ProjectId,
            request.Description,
            request.Priority,
            request.DueDate,
            request.AssigneeId);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var createdDto = await _taskRepository.GetDetailByIdAsync(task.Id, cancellationToken);
        return createdDto!;
    }

    public async Task<TaskDetailDto> CreateForProjectAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default)
    {
        var taskRequest = new CreateTaskRequest
        {
            Title = request.Title,
            ProjectId = projectId,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
            AssigneeId = request.AssigneeId
        };
        return await CreateAsync(taskRequest, cancellationToken);
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
