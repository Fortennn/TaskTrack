using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Application.DTOs.Comments;

public sealed record CreateCommentRequest(
    [Required] Guid AuthorId,
    [Required, MaxLength(2000)] string Text);
