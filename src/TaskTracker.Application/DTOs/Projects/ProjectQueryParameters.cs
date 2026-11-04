using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Application.DTOs.Projects;

public sealed class ProjectQueryParameters : IValidatableObject
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

    /// <summary>Необов’язковий пошуковий фрагмент для назви проєкту без урахування регістру.</summary>
    public string? Search { get; init; }

    /// <summary>Поле сортування: name або createdat.</summary>
    public string SortBy { get; init; } = "createdat";

    /// <summary>Напрям сортування: asc або desc.</summary>
    public string Order { get; init; } = "desc";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validSortFields = new[] { "name", "createdat" };
        if (!validSortFields.Contains(SortBy.ToLowerInvariant()))
        {
            yield return new ValidationResult(
                "SortBy must be either 'name' or 'createdat'.",
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
