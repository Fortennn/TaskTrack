namespace TaskTracker.Application.DTOs.Events;

public sealed record EventDto(
    Guid Id,
    string Title,
    string? Description,
    string Location,
    double? Latitude,
    double? Longitude,
    DateTime Date,
    int Capacity,
    int CategoryId);

public sealed record EventDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string Location,
    double? Latitude,
    double? Longitude,
    DateTime Date,
    int Capacity,
    int CategoryId);

public sealed record CreateEventRequest(
    string Title,
    string? Description,
    string Location,
    DateTime Date,
    int Capacity,
    int CategoryId);

public sealed record UpdateEventRequest(
    string Title,
    string? Description,
    string Location,
    DateTime Date,
    int Capacity,
    int CategoryId);
