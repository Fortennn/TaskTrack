using Microsoft.AspNetCore.Mvc;
using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs.Projects;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Api.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController : ControllerBase
{
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;

    public ProjectsController(IProjectRepository projectRepository, ITaskRepository taskRepository)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
    }

    /// <summary>
    /// Отримання списку проектів з пагінацією та підрахунком задач без N+1
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetProjects(
        [FromQuery] ProjectQueryParameters parameters, 
        CancellationToken cancellationToken)
    {
        var result = await _projectRepository.GetPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Отримання детальної інформації про проект та його задачі
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> GetProjectById(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetDetailByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Project with Id '{id}' does not exist.");

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
        var project = new Project(request.Name, request.Description);
        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        var dto = new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            0,
            project.CreatedAt);

        return CreatedAtAction(nameof(GetProjectById), new { id = project.Id }, dto);
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
        var projectExists = await _projectRepository.ExistsAsync(projectId, cancellationToken);
        if (!projectExists)
        {
            throw new NotFoundException($"Project with Id '{projectId}' does not exist.");
        }

        var task = new TaskItem(
            request.Title,
            projectId,
            request.Description,
            request.Priority,
            request.DueDate,
            request.AssigneeId);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var createdTaskDto = await _taskRepository.GetDetailByIdAsync(task.Id, cancellationToken);
        return CreatedAtAction("GetTaskById", "Tasks", new { id = task.Id }, createdTaskDto);
    }
}
