using TaskTracker.Domain.Common;

namespace TaskTracker.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid TaskId { get; private set; }
    public TaskItem Task { get; private set; } = null!;

    public Guid AuthorId { get; private set; }
    public User Author { get; private set; } = null!;

    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    private Comment() { } // Для EF Core

    public Comment(Guid taskId, Guid authorId, string text)
    {
        TaskId = taskId;
        AuthorId = authorId;
        Text = text;
        CreatedAt = DateTime.UtcNow;
    }
}
