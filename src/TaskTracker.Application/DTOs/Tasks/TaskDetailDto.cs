using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Domain.Enums;
using TaskStatus = TaskTracker.Domain.Enums.TaskStatus;

namespace TaskTracker.Application.DTOs.Tasks;

/// <summary>
/// Детальна інформація про задачу із вкладеною колекцією коментарів та координатами.
/// </summary>
public sealed record TaskDetailDto(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    Priority Priority,
    DateTime? DueDate,
    string? CoverImageUrl,
    string? Location,
    double? Latitude,
    double? Longitude,
    Guid ProjectId,
    string ProjectName,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime CreatedAt,
    IReadOnlyList<CommentDto> Comments);
