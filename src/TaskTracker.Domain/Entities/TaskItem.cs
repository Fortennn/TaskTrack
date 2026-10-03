using TaskTracker.Domain.Common;
using TaskTracker.Domain.Enums;
using TaskStatus = TaskTracker.Domain.Enums.TaskStatus;

namespace TaskTracker.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TaskStatus Status { get; private set; } = TaskStatus.Todo;
    public Priority Priority { get; private set; } = Priority.Medium;
    public DateTime? DueDate { get; private set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;

    public Guid? AssigneeId { get; private set; }
    public User? Assignee { get; private set; }

    // Navigation properties
    public ICollection<Comment> Comments { get; private set; } = [];

    private TaskItem() { } // Для EF Core

    public TaskItem(
        string title, 
        Guid projectId, 
        string? description = null, 
        Priority priority = Priority.Medium, 
        DateTime? dueDate = null, 
        Guid? assigneeId = null)
    {
        Title = title;
        ProjectId = projectId;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        AssigneeId = assigneeId;
        Status = TaskStatus.Todo;
        CreatedAt = DateTime.UtcNow;
    }
}
