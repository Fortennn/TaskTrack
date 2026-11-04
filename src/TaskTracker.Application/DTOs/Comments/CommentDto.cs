namespace TaskTracker.Application.DTOs.Comments;

/// <summary>
/// Інформація про коментар до задачі.
/// </summary>
/// <param name="Id">Унікальний ідентифікатор коментаря.</param>
/// <param name="TaskId">Ідентифікатор задачі, до якої належить коментар.</param>
/// <param name="AuthorId">Ідентифікатор автора коментаря.</param>
/// <param name="AuthorName">Ім'я автора коментаря.</param>
/// <param name="Text">Текст коментаря.</param>
/// <param name="CreatedAt">Дата та час публікації коментаря (UTC).</param>
public sealed record CommentDto(
    Guid Id,
    Guid TaskId,
    Guid AuthorId,
    string AuthorName,
    string Text,
    DateTime CreatedAt);
