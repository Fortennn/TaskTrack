namespace TaskTracker.Application.DTOs.Projects;

public sealed record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    int TasksCount,
    DateTime CreatedAt);
