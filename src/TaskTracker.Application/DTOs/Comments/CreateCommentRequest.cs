namespace TaskTracker.Application.DTOs.Comments;

public sealed record CreateCommentRequest(
    Guid AuthorId,
    string Text);
