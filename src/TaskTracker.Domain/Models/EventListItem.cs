using TaskTracker.Domain.Entities;

namespace TaskTracker.Domain.Models;

public sealed record EventListItem(
    Event Event,
    string CategoryName,
    int RegistrationCount);
