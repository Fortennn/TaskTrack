using TaskTracker.Application.Common;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs.Projects;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Application.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<ProjectDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _projectRepository.GetDetailByIdAsync(id, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException($"Project with Id '{id}' does not exist.");
        }

        return project;
    }

    public async Task<PagedResult<ProjectDto>> GetPagedAsync(ProjectQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        return await _projectRepository.GetPagedAsync(parameters, cancellationToken);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        var nameExists = await _projectRepository.ExistsByNameAsync(request.Name, cancellationToken);
        if (nameExists)
        {
            throw new ConflictException($"Project with name '{request.Name}' already exists.");
        }

        var project = new Project(request.Name, request.Description);
        await _projectRepository.AddAsync(project, cancellationToken);
        await _projectRepository.SaveChangesAsync(cancellationToken);

        return new ProjectDto(
            project.Id,
            project.Name,
            project.Description,
            0,
            project.CreatedAt);
    }
}
