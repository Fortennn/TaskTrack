using TaskTracker.Domain.Enums;
using TaskStatus = TaskTracker.Domain.Enums.TaskStatus;

namespace TaskTracker.Application.DTOs.Tasks;

/// <summary>
/// Стисла інформація про задачу для списку.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор задачі.</param>
/// <param name="Title">Назва задачі.</param>
/// <param name="Description">Необов’язковий опис задачі.</param>
/// <param name="Status">Статус задачі (Todo, InProgress, Done).</param>
/// <param name="Priority">Пріоритет виконання (Low, Medium, High, Critical).</param>
/// <param name="DueDate">Дедлайн задачі (UTC).</param>
/// <param name="CoverImageUrl">Публічний відносний URL обкладинки задачі або null.</param>
/// <param name="ProjectId">Ідентифікатор проєкту, до якого належить задача.</param>
/// <param name="ProjectName">Назва проєкту.</param>
/// <param name="AssigneeId">Необов’язковий ідентифікатор виконавця.</param>
/// <param name="AssigneeName">Ім'я виконавця.</param>
/// <param name="CommentsCount">Загальна кількість коментарів до задачі.</param>
/// <param name="CreatedAt">Дата та час створення задачі (UTC).</param>
public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    Priority Priority,
    DateTime? DueDate,
    string? CoverImageUrl,
    Guid ProjectId,
    string ProjectName,
    Guid? AssigneeId,
    string? AssigneeName,
    int CommentsCount,
    DateTime CreatedAt);
