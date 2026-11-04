using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Application.DTOs.Projects;

public sealed class CreateProjectRequest
{
    /// <summary>Обов’язкова унікальна назва проєкту.</summary>
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    /// <summary>Необов’язковий опис проєкту.</summary>
    [MaxLength(1000)]
    public string? Description { get; init; }
}
