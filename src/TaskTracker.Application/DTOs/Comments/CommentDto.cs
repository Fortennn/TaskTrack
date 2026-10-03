namespace TaskTracker.Application.DTOs.Comments;

public sealed record CommentDto(
    Guid Id,
    Guid TaskId,
    Guid AuthorId,
    string AuthorName,
    string Text,
    DateTime CreatedAt);
