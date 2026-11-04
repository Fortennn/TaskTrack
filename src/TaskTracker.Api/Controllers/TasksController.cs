using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Application.Services;

namespace TaskTracker.Api.Controllers;

/// <summary>
/// Управління задачами та вкладеними коментарями.
/// </summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public sealed class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Повертає відфільтрований список задач із детермінованим сортуванням та пагінацією.
    /// </summary>
    /// <param name="parameters">Параметри фільтрації, сортування та пагінації.</param>
    /// <param name="cancellationToken">Токен скасування асинхронної операції.</param>
    /// <response code="200">Успішне отримання сторінки списку задач.</response>
    /// <response code="400">Помилка валідації параметрів запиту (наприклад, DueDateFrom > DueDateTo або некоректні параметри пагінації).</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<TaskDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TaskDto>>> GetTasks(
        [FromQuery] TaskQueryParameters parameters, 
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Повертає детальну інформацію про задачу за її ідентифікатором із вкладеними коментарями.
    /// </summary>
    /// <param name="id">Унікальний глобальний ідентифікатор задачі (UUID).</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="200">Задачу знайдено та успішно повернуто.</response>
    /// <response code="404">Задачу з указаним ідентифікатором не знайдено.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> GetTaskById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
        return Ok(task);
    }

    /// <summary>
    /// Створює нову задачу.
    /// </summary>
    /// <param name="request">Дані для створення задачі.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="201">Задачу успішно створено.</response>
    /// <response code="400">Помилка валідації вхідних даних (відсутні обов'язкові поля або дата дедлайну в минулому).</response>
    /// <response code="404">Вказаний проєкт або призначений виконавець не існують.</response>
    [HttpPost]
    [ProducesResponseType<TaskDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> CreateTask(
        [FromBody] CreateTaskRequest request, 
        CancellationToken cancellationToken)
    {
        var createdTask = await _taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
    }

    /// <summary>
    /// Повертає впорядкований список усіх коментарів конкретної задачі.
    /// </summary>
    /// <param name="taskId">Ідентифікатор задачі, коментарі якої запитуються.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="200">Список коментарів успішно отримано.</response>
    /// <response code="404">Задачу з указаним ідентифікатором не знайдено.</response>
    [HttpGet("{taskId:guid}/comments")]
    [ProducesResponseType<IReadOnlyList<CommentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CommentDto>>> GetComments(
        Guid taskId, 
        CancellationToken cancellationToken)
    {
        var comments = await _taskService.GetCommentsAsync(taskId, cancellationToken);
        return Ok(comments);
    }

    /// <summary>
    /// Додає новий коментар до задачі (вкладений ендпоінт).
    /// </summary>
    /// <param name="taskId">Ідентифікатор задачі, до якої додається коментар.</param>
    /// <param name="request">Тіло запиту з ідентифікатором автора та текстом коментаря.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="201">Коментар успішно створено та додано до задачі.</response>
    /// <response code="400">Помилка валідації даних коментаря (порожній або задовгий текст).</response>
    /// <response code="404">Задачу або вказаного автора коментаря не знайдено.</response>
    [HttpPost("{taskId:guid}/comments")]
    [ProducesResponseType<CommentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> AddComment(
        Guid taskId, 
        [FromBody] CreateCommentRequest request, 
        CancellationToken cancellationToken)
    {
        var comment = await _taskService.AddCommentAsync(taskId, request, cancellationToken);
        return CreatedAtAction(nameof(GetComments), new { taskId = taskId }, comment);
    }
}
