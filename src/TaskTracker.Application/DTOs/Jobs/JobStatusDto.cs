namespace TaskTracker.Application.DTOs.Jobs;

public sealed record JobStatusDto(
    Guid JobId,
    string Status);
