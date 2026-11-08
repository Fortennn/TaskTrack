namespace TaskTracker.Domain.Entities;

public class Registration
{
    public int Id { get; set; }
    public Guid EventId { get; set; }
    public Event? Event { get; set; }
    public string AttendeeName { get; set; } = string.Empty;
    public string AttendeeEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
