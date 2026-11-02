using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Application.Services;

public sealed class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;

    public TaskService(
        ITaskRepository taskRepository,
        IProjectRepository projectRepository,
        IUserRepository userRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _userRepository = userRepository;
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
        var taskRequest = request with { ProjectId = projectId };
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
}
