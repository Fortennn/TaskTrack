using TaskTracker.Application.DTOs.Comments;
using TaskTracker.Domain.Enums;
using TaskStatus = TaskTracker.Domain.Enums.TaskStatus;

namespace TaskTracker.Application.DTOs.Tasks;

/// <summary>
/// Детальна інформація про задачу із вкладеною колекцією коментарів.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор задачі.</param>
/// <param name="Title">Назва задачі.</param>
/// <param name="Description">Детальний опис задачі.</param>
/// <param name="Status">Поточний статус задачі.</param>
/// <param name="Priority">Пріоритет виконання.</param>
/// <param name="DueDate">Дедлайн задачі (UTC).</param>
/// <param name="ProjectId">Ідентифікатор проєкту.</param>
/// <param name="ProjectName">Назва проєкту.</param>
/// <param name="AssigneeId">Ідентифікатор призначеного виконавця.</param>
/// <param name="AssigneeName">Ім'я виконавця.</param>
/// <param name="CreatedAt">Дата створення задачі (UTC).</param>
/// <param name="Comments">Впорядкований за спаданням дати список коментарів.</param>
public sealed record TaskDetailDto(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    Priority Priority,
    DateTime? DueDate,
    Guid ProjectId,
    string ProjectName,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime CreatedAt,
    IReadOnlyList<CommentDto> Comments);
