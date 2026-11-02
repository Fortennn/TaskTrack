using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs.Projects;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Application.Services;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/projects")]
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
    /// Отримання списку проектів з пагінацією та підрахунком задач без N+1
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetProjects(
        [FromQuery] ProjectQueryParameters parameters, 
        CancellationToken cancellationToken)
    {
        var result = await _projectService.GetPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Отримання детальної інформації про проект та його задачі
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetProjectById(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetByIdAsync(id, cancellationToken);
        return Ok(project);
    }

    /// <summary>
    /// Створення нового проекту
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject(
        [FromBody] CreateProjectRequest request, 
        CancellationToken cancellationToken)
    {
        var createdProject = await _projectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetProjectById), new { id = createdProject.Id }, createdProject);
    }

    /// <summary>
    /// Вкладений ендпоінт: створення задачі всередині конкретного проекту
    /// </summary>
    [HttpPost("{projectId:guid}/tasks")]
    public async Task<ActionResult<TaskDetailDto>> CreateProjectTask(
        Guid projectId, 
        [FromBody] CreateTaskRequest request, 
        CancellationToken cancellationToken)
    {
        var createdTaskDto = await _taskService.CreateForProjectAsync(projectId, request, cancellationToken);
        return CreatedAtAction("GetTaskById", "Tasks", new { id = createdTaskDto.Id }, createdTaskDto);
    }
}
