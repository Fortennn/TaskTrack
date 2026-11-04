using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs.Projects;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Application.Services;

namespace TaskTracker.Api.Controllers;

/// <summary>
/// Управління проєктами та створення задач у межах проєктів.
/// </summary>
[ApiController]
[Route("api/projects")]
[Produces("application/json")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;

    public ProjectsController(IProjectService projectService, ITaskService taskService)
    {
        _projectService = projectService;
        _taskService = taskService;
    }

    /// <summary>
    /// Повертає список проєктів з пагінацією та підрахованою кількістю задач (без проблеми N+1).
    /// </summary>
    /// <param name="parameters">Параметри пагінації, пошуку та сортування проєктів.</param>
    /// <param name="cancellationToken">Токен скасування асинхронної операції.</param>
    /// <response code="200">Список проєктів успішно отримано.</response>
    /// <response code="400">Помилка валідації параметрів запиту.</response>
    [HttpGet]
    [ProducesResponseType<PagedResult<ProjectDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetProjects(
        [FromQuery] ProjectQueryParameters parameters, 
        CancellationToken cancellationToken)
    {
        var result = await _projectService.GetPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Повертає детальну інформацію про проєкт та всі його задачі.
    /// </summary>
    /// <param name="id">Ідентифікатор проєкту (UUID).</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="200">Проєкт знайдено та успішно повернено.</response>
    /// <response code="404">Проєкт із зазначеним ідентифікатором не існує.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProjectDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailDto>> GetProjectById(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id, cancellationToken);
        return Ok(project);
    }

    /// <summary>
    /// Створює новий проєкт з унікальною назвою.
    /// </summary>
    /// <param name="request">Тіло запиту з назвою та описом проєкту.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="201">Проєкт успішно створено.</response>
    /// <response code="400">Помилка валідації вхідних даних (відсутня або задовга назва).</response>
    /// <response code="409">Проєкт із такою назвою вже існує в системі.</response>
    [HttpPost]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDto>> CreateProject(
        [FromBody] CreateProjectRequest request, 
        CancellationToken cancellationToken)
    {
        var createdProject = await _projectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProjectById), new { id = createdProject.Id }, createdProject);
    }

    /// <summary>
    /// Створює нову задачу безпосередньо у вказаному проєкті (вкладений ендпоінт).
    /// </summary>
    /// <param name="projectId">Ідентифікатор проєкту, у якому створюється задача.</param>
    /// <param name="request">Дані створюваної задачі.</param>
    /// <param name="cancellationToken">Токен скасування операції.</param>
    /// <response code="201">Задачу успішно створено всередині проєкту.</response>
    /// <response code="400">Помилка валідації вхідних даних задачі.</response>
    /// <response code="404">Проєкт або призначений виконавець не знайдені.</response>
    [HttpPost("{projectId:guid}/tasks")]
    [ProducesResponseType<TaskDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDetailDto>> CreateProjectTask(
        Guid projectId, 
        [FromBody] CreateTaskRequest request, 
        CancellationToken cancellationToken)
    {
        var createdTaskDto = await _taskService.CreateForProjectAsync(projectId, request, cancellationToken);
        return CreatedAtAction("GetTaskById", "Tasks", new { id = createdTaskDto.Id }, createdTaskDto);
    }
}
