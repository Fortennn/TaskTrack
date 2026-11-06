using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Application.DTOs.Tasks;

namespace TaskTracker.Application.Services;

public interface ITaskService
{
    Task<TaskDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<TaskDto>> GetPagedAsync(TaskQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<CreateTaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<CreateTaskResponse> CreateForProjectAsync(Guid projectId, CreateTaskRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<CommentDto> AddCommentAsync(Guid taskId, CreateCommentRequest request, CancellationToken cancellationToken = default);
    Task<TaskDetailDto> UploadCoverAsync(Guid id, CoverImageUpload? upload, CancellationToken cancellationToken = default);
    Task<CoverImageFile> GetCoverAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteCoverAsync(Guid id, CancellationToken cancellationToken = default);
}
