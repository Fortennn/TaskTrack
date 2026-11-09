using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Application.Services;

namespace TaskTracker.Api.Controllers;

/// <summary>
/// Управління задачами, вкладеними коментарями та обкладинками.
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
    /// Створює нову задачу та реєструє фонову задачу для надсилання сповіщення.
    /// </summary>
    /// <param name="request">Дані для створення задачі.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="201">Задачу успішно створено, повернуто дані задачі та jobId фонової задачі.</response>
    /// <response code="400">Помилка валідації вхідних даних (відсутні обов'язкові поля або дата дедлайну в минулому).</response>
    /// <response code="404">Вказаний проєкт або призначений виконавець не існують.</response>
    [HttpPost]
    [ProducesResponseType<CreateTaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateTaskResponse>> CreateTask(
        [FromBody] CreateTaskRequest request, 
        CancellationToken cancellationToken)
    {
        var response = await _taskService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTaskById), new { id = response.Task.Id }, response);
    }

    /// <summary>
    /// Оновлює задачу (назву, опис, статус, пріоритет, дедлайн, виконавця або адресу).
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор задачі (UUID).</param>
    /// <param name="request">Оновлені дані задачі.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="200">Задачу успішно оновлено.</response>
    /// <response code="400">Помилка валідації вхідних даних.</response>
    /// <response code="404">Задачу або виконавця не знайдено.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TaskDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> UpdateTask(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _taskService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Видаляє задачу за її ідентифікатором.
    /// </summary>
    /// <param name="id">Унікальний ідентифікатор задачі (UUID).</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="204">Задачу успішно видалено.</response>
    /// <response code="404">Задачу не знайдено.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Завантажує або оновлює зображення-обкладинку для задачі (JPEG або PNG до 5 МБ).
    /// </summary>
    /// <param name="id">Ідентифікатор задачі (UUID).</param>
    /// <param name="cover">Файл зображення у форматі multipart/form-data.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="200">Обкладинку успішно завантажено, повернуто оновлену задачу з посиланням на обкладинку.</response>
    /// <response code="400">Некоректний файл (порожній, непідтримуваний формат або перевищено 5 МБ).</response>
    /// <response code="404">Задачу не знайдено.</response>
    [HttpPost("{id:guid}/cover")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<TaskDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> UploadCover(
        Guid id,
        IFormFile? cover,
        CancellationToken cancellationToken)
    {
        await using var content = cover?.OpenReadStream();
        var upload = cover is null || content is null
            ? null
            : new CoverImageUpload(
                content,
                cover.FileName,
                cover.ContentType,
                cover.Length);

        var taskItem = await _taskService.UploadCoverAsync(id, upload, cancellationToken);
        return Ok(taskItem);
    }

    /// <summary>
    /// Отримує бінарний вміст зображення обкладинки задачі.
    /// </summary>
    /// <param name="id">Ідентифікатор задачі.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="200">Файл знайдено та повернуто у відповіді.</response>
    /// <response code="404">Задачу або обкладинку не знайдено.</response>
    [HttpGet("{id:guid}/cover")]
    [Produces("image/jpeg", "image/png")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCover(Guid id, CancellationToken cancellationToken)
    {
        var cover = await _taskService.GetCoverAsync(id, cancellationToken);
        return File(cover.Content, cover.ContentType, enableRangeProcessing: true);
    }

    /// <summary>
    /// Видаляє зображення обкладинки задачі.
    /// </summary>
    /// <param name="id">Ідентифікатор задачі.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="204">Обкладинку успішно видалено.</response>
    /// <response code="404">Задачу не знайдено.</response>
    [HttpDelete("{id:guid}/cover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCover(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteCoverAsync(id, cancellationToken);
        return NoContent();
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
