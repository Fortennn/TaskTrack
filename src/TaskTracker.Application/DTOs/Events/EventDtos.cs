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
    int CategoryId,
    string? CategoryName = null,
    int RegistrationCount = 0,
    string? CoverImageUrl = null,
    DateTime? CreatedAt = null);

public sealed record EventDetailDto(
    Guid Id,
    string Title,
    string? Description,
    string Location,
    double? Latitude,
    double? Longitude,
    DateTime Date,
    int Capacity,
    int CategoryId,
    string? CategoryName = null,
    int RegistrationCount = 0,
    string? CoverImageUrl = null,
    DateTime? CreatedAt = null);

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

public sealed record CreateRegistrationRequest(
    string AttendeeName,
    string AttendeeEmail);

public sealed record RegistrationDto(
    int Id,
    Guid EventId,
    string AttendeeName,
    string AttendeeEmail,
    DateTime CreatedAt);
