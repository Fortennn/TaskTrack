using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs.Projects;

namespace TaskTracker.Application.Services;

public interface IProjectService
{
    Task<ProjectDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProjectDto>> GetPagedAsync(ProjectQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken = default);
}
