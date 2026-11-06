using System.ComponentModel.DataAnnotations;
using TaskTracker.Application.Validation;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Application.DTOs.Tasks;

public sealed class CreateTaskRequest
{
    /// <summary>Обов’язкова назва задачі.</summary>
    [Required, MaxLength(250)]
    public string Title { get; init; } = string.Empty;

    /// <summary>Ідентифікатор проєкту, до якого належить задача (якщо не вказано у маршруті URL).</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Необов’язковий детальний опис задачі.</summary>
    [MaxLength(4000)]
    public string? Description { get; init; }

    /// <summary>Пріоритет задачі (Low, Medium, High, Critical).</summary>
    public Priority Priority { get; init; } = Priority.Medium;

    /// <summary>Необов’язковий дедлайн задачі у майбутньому (UTC).</summary>
    [FutureDate]
    public DateTime? DueDate { get; init; }

    /// <summary>Необов’язковий ідентифікатор користувача-виконавця.</summary>
    public Guid? AssigneeId { get; init; }
}
