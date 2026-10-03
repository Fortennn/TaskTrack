using TaskTracker.Domain.Common;

namespace TaskTracker.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<TaskItem> Tasks { get; private set; } = [];

    private Project() { } // Для EF Core

    public Project(string name, string? description = null)
    {
        Name = name;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}
