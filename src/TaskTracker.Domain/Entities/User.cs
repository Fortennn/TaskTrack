using TaskTracker.Domain.Common;
using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Role Role { get; private set; }

    // Navigation properties
    public ICollection<TaskItem> AssignedTasks { get; private set; } = [];
    public ICollection<Comment> Comments { get; private set; } = [];

    private User() { } // Для EF Core

    public User(string name, string email, Role role)
    {
        Name = name;
        Email = email;
        Role = role;
    }
}
