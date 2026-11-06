namespace TaskTracker.Application.DTOs;

public sealed record RegistrationDto(
    int Id,
    int EventId,
    string AttendeeName,
    string AttendeeEmail,
    string Status,
    DateTime RegisteredAt);

public sealed record CreateRegistrationResponse(
    RegistrationDto Registration,
    Guid JobId);
