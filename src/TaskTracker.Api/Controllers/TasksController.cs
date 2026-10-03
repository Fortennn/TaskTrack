using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository _taskRepository;
    private readonly IProjectRepository _projectRepository;

    public TasksController(ITaskRepository taskRepository, IProjectRepository projectRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
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
        var task = await _taskRepository.GetDetailByIdAsync(id, cancellationToken);
        if (task == null)
        {
            return NotFound(new { message = $"Task with Id '{id}' not found." });
        }

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
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return BadRequest(new { message = $"Project with Id '{request.ProjectId}' does not exist." });
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
        var task = await _taskRepository.GetByIdAsync(taskId, cancellationToken);
        if (task == null)
        {
            return NotFound(new { message = $"Task with Id '{taskId}' not found." });
        }

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
        try
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
