using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Application.Services;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Отримання списку задач з пагінацією, фільтрацією та сортуванням (без N+1)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaskDto>>> GetTasks(
        [FromQuery] TaskQueryParameters parameters, 
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Отримання детальної інформації про задачу із вкладеними коментарями
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDetailDto>> GetTaskById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
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
        var createdTask = await _taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
    }

    /// <summary>
    /// Вкладений ендпоінт: отримання списку коментарів конкретної задачі
    /// </summary>
    [HttpGet("{taskId:guid}/comments")]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetComments(
        Guid taskId, 
        CancellationToken cancellationToken)
    {
        var comments = await _taskService.GetCommentsAsync(taskId, cancellationToken);
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
        var comment = await _taskService.AddCommentAsync(taskId, request, cancellationToken);
        return CreatedAtAction(nameof(GetComments), new { taskId = taskId }, comment);
    }
}
