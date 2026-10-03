using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Application.DTOs.Projects;

public sealed class ProjectQueryParameters
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

    public string? Search { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string Order { get; init; } = "desc";
}
