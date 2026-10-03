using System.ComponentModel.DataAnnotations;
using TaskTracker.Domain.Enums;
using TaskStatus = TaskTracker.Domain.Enums.TaskStatus;

namespace TaskTracker.Application.DTOs.Tasks;

public sealed class TaskQueryParameters : IValidatableObject
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than or equal to 1.")]
    public int Page { get; init; } = 1;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value is > 0 and <= MaxPageSize ? value : (value > MaxPageSize ? MaxPageSize : 20);
    }

    public Guid? ProjectId { get; init; }
    public Guid? AssigneeId { get; init; }
    public TaskStatus? Status { get; init; }
    public Priority? Priority { get; init; }
    public string? Search { get; init; }
    public DateOnly? DueDateFrom { get; init; }
    public DateOnly? DueDateTo { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string Order { get; init; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DueDateFrom.HasValue && DueDateTo.HasValue && DueDateFrom > DueDateTo)
        {
            yield return new ValidationResult(
                "DueDateFrom must be earlier than or equal to DueDateTo.",
                [nameof(DueDateFrom), nameof(DueDateTo)]);
        }
    }
}
