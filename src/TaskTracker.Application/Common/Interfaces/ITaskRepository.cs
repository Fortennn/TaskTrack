using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Common.Interfaces;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TaskDetailDto?> GetDetailByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<TaskDto>> GetPagedAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentDto>> GetCommentsByTaskIdAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<Comment> AddCommentAsync(Guid taskId, Guid authorId, string text, CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    void Update(TaskItem task);
    void Delete(TaskItem task);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
