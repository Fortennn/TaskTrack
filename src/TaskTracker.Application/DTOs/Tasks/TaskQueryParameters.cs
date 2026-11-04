using System.ComponentModel.DataAnnotations;
using TaskTracker.Domain.Enums;
using TaskStatus = TaskTracker.Domain.Enums.TaskStatus;

namespace TaskTracker.Application.DTOs.Tasks;

public sealed class TaskQueryParameters : IValidatableObject
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    /// <summary>Номер сторінки, починаючи з одиниці.</summary>
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    /// <summary>Кількість елементів на сторінці; максимальне значення — 100.</summary>
    [Range(1, 100)]
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value is > 0 and <= MaxPageSize ? value : (value > MaxPageSize ? MaxPageSize : 20);
    }

    /// <summary>Необов’язковий фільтр за ідентифікатором проєкту.</summary>
    public Guid? ProjectId { get; init; }

    /// <summary>Необов’язковий фільтр за ідентифікатором виконавця.</summary>
    public Guid? AssigneeId { get; init; }

    /// <summary>Необов’язковий фільтр за статусом задачі (Todo, InProgress, Done).</summary>
    public TaskStatus? Status { get; init; }

    /// <summary>Необов’язковий фільтр за пріоритетом (Low, Medium, High, Critical).</summary>
    public Priority? Priority { get; init; }

    /// <summary>Необов’язковий пошуковий фрагмент для назви задачі без урахування регістру.</summary>
    public string? Search { get; init; }

    /// <summary>Необов’язкова нижня межа дедлайну за UTC.</summary>
    public DateOnly? DueDateFrom { get; init; }

    /// <summary>Необов’язкова верхня межа дедлайну за UTC.</summary>
    public DateOnly? DueDateTo { get; init; }

    /// <summary>Поле сортування: title, duedate, priority, status, createdat.</summary>
    public string SortBy { get; init; } = "createdat";

    /// <summary>Напрям сортування: asc або desc.</summary>
    public string Order { get; init; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueDateFrom.HasValue && DueDateTo.HasValue && DueDateFrom > DueDateTo)
        {
            yield return new ValidationResult(
                "DueDateFrom must be earlier than or equal to DueDateTo.",
                [nameof(DueDateFrom), nameof(DueDateTo)]);
        }

        var validSortFields = new[] { "title", "duedate", "priority", "status", "createdat" };
        if (!validSortFields.Contains(SortBy.ToLowerInvariant()))
        {
            yield return new ValidationResult(
                "SortBy must be one of: 'title', 'duedate', 'priority', 'status', 'createdat'.",
                [nameof(SortBy)]);
        }

        if (!string.Equals(Order, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(Order, "desc", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Order must be either 'asc' or 'desc'.",
                [nameof(Order)]);
        }
    }
}
