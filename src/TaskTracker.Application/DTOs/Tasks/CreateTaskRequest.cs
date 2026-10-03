using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs.Tasks;

public sealed record CreateTaskRequest(
    string Title,
    Guid ProjectId,
    string? Description = null,
    Priority Priority = Priority.Medium,
    DateTime? DueDate = null,
    Guid? AssigneeId = null);
