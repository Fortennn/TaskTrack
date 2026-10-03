using System.ComponentModel.DataAnnotations;
using TaskTracker.Application.Validation;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs.Tasks;

public sealed record CreateTaskRequest(
    [Required, MaxLength(250)] string Title,
    [Required] Guid ProjectId,
    [MaxLength(4000)] string? Description = null,
    Priority Priority = Priority.Medium,
    [FutureDate] DateTime? DueDate = null,
    Guid? AssigneeId = null);
