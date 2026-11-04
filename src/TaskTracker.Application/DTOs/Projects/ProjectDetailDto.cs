using TaskTracker.Application.DTOs.Tasks;

namespace TaskTracker.Application.DTOs.Projects;

/// <summary>
/// Детальна інформація про проєкт із переліком задач.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор проєкту.</param>
/// <param name="Name">Унікальна назва проєкту.</param>
/// <param name="Description">Опис проєкту.</param>
/// <param name="CreatedAt">Дата створення проєкту (UTC).</param>
/// <param name="Tasks">Впорядкований перелік задач проєкту.</param>
public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    IReadOnlyList<TaskDto> Tasks);
