using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Domain.Enums;
using TaskStatus = TaskTracker.Domain.Enums.TaskStatus;

namespace TaskTracker.Application.DTOs.Tasks;

public sealed record TaskDetailDto(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    Priority Priority,
    DateTime? DueDate,
    Guid ProjectId,
    string ProjectName,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime CreatedAt,
    IReadOnlyList<CommentDto> Comments);
