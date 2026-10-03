using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskRepository _taskRepository;

    public TasksController(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    /// <summary>
    /// Отримання списку задач з пагінацією, фільтрацією та сортуванням (без N+1)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskDto>>> GetTasks(
        [FromQuery] TaskQueryParameters parameters, 
        CancellationToken cancellationToken)
    {
        var result = await _taskRepository.GetPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Отримання детальної інформації про задачу із вкладеними коментарями
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailDto>> GetTaskById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskRepository.GetDetailByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Task with Id '{id}' does not exist.");

        return Ok(task);
    }

    /// <summary>
    /// Створення нової задачі
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TaskDetailDto>> CreateTask(
        [FromBody] CreateTaskRequest request, 
        CancellationToken cancellationToken)
    {
        var task = new TaskItem(
            request.Title,
            request.ProjectId,
            request.Description,
            request.Priority,
            request.DueDate,
            request.AssigneeId);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var createdTaskDto = await _taskRepository.GetDetailByIdAsync(task.Id, cancellationToken);
        return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, createdTaskDto);
    }

    /// <summary>
    /// Вкладений ендпоінт: отримання списку коментарів конкретної задачі
    /// </summary>
    [HttpGet("{taskId:guid}/comments")]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetComments(
        Guid taskId, 
        CancellationToken cancellationToken)
    {
        _ = await _taskRepository.GetByIdAsync(taskId, cancellationToken)
            ?? throw new NotFoundException($"Task with Id '{taskId}' does not exist.");

        var comments = await _taskRepository.GetCommentsByTaskIdAsync(taskId, cancellationToken);
        return Ok(comments);
    }

    /// <summary>
    /// Вкладений ендпоінт: додавання коментаря до задачі
    /// </summary>
    [HttpPost("{taskId:guid}/comments")]
    public async Task<ActionResult<CommentDto>> AddComment(
        Guid taskId, 
        [FromBody] CreateCommentRequest request, 
        CancellationToken cancellationToken)
    {
        var comment = await _taskRepository.AddCommentAsync(taskId, request.AuthorId, request.Text, cancellationToken);
        
        var responseDto = new CommentDto(
            comment.Id,
            comment.TaskId,
            comment.AuthorId,
            string.Empty,
            comment.Text,
            comment.CreatedAt);

        return CreatedAtAction(nameof(GetComments), new { taskId = taskId }, responseDto);
    }
}
