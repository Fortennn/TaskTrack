using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Application.DTOs.Comments;

public sealed class CreateCommentRequest
{
    /// <summary>Обов’язковий ідентифікатор автора коментаря.</summary>
    [Required]
    public Guid AuthorId { get; init; }

    /// <summary>Обов’язковий текст коментаря.</summary>
    [Required, MaxLength(2000)]
    public string Text { get; init; } = string.Empty;
}
