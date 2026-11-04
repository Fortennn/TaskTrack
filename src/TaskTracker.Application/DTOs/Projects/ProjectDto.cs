namespace TaskTracker.Application.DTOs.Projects;

/// <summary>
/// Стисла інформація про проєкт для списку.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор проєкту.</param>
/// <param name="Name">Унікальна назва проєкту.</param>
/// <param name="Description">Опис проєкту.</param>
/// <param name="TasksCount">Загальна кількість задач у проєкті.</param>
/// <param name="CreatedAt">Дата та час створення проєкту (UTC).</param>
public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    int TasksCount,
    DateTime CreatedAt);
