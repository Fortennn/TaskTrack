using TaskTracker.Domain.Enums;
using TaskStatus = TaskTracker.Domain.Enums.TaskStatus;

namespace TaskTracker.Application.DTOs.Tasks;

/// <summary>
/// Стисла інформація про задачу для списку.
/// </summary>
public sealed record TaskDto(
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
    int CommentsCount,
    DateTime CreatedAt);
