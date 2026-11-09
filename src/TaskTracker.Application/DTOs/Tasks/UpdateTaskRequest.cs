using System.ComponentModel.DataAnnotations;
using TaskTracker.Domain.Enums;
using TaskStatus = TaskTracker.Domain.Enums.TaskStatus;

namespace TaskTracker.Application.DTOs.Tasks;

public sealed class UpdateTaskRequest
{
    [Required, MaxLength(250)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(4000)]
    public string? Description { get; init; }

    public TaskStatus Status { get; init; } = TaskStatus.Todo;

    public Priority Priority { get; init; } = Priority.Medium;

    public DateTime? DueDate { get; init; }

    public Guid? AssigneeId { get; init; }

    [MaxLength(500)]
    public string? Location { get; init; }
}
