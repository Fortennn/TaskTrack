using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Application.DTOs.Events;

public sealed class EventQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    public int? CategoryId { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string? Search { get; init; }
    public string SortBy { get; init; } = "date";
    public string Order { get; init; } = "asc";
}
