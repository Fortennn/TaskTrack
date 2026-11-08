using TaskTracker.Domain.Common;

namespace TaskTracker.Domain.Entities;

public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime Date { get; set; }
    public int Capacity { get; set; }
    public int CategoryId { get; set; }

    public Event() { }

    public Event(
        string title,
        string description,
        string location,
        DateTime date,
        int capacity,
        int categoryId,
        double? latitude = null,
        double? longitude = null)
    {
        Title = title;
        Description = description;
        Location = location;
        Date = date;
        Capacity = capacity;
        CategoryId = categoryId;
        Latitude = latitude;
        Longitude = longitude;
    }
}
