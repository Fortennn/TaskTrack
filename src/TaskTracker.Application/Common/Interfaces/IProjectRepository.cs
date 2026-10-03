using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Common.Interfaces;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Project>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    void Update(Project project);
    void Delete(Project project);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
