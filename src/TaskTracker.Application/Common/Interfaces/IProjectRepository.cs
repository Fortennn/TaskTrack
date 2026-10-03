using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs.Projects;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProjectDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProjectDto>> GetPagedAsync(ProjectQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    void Update(Project project);
    void Delete(Project project);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
