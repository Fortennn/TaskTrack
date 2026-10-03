namespace TaskTracker.Application.DTOs.Projects;

public sealed record CreateProjectRequest(
    string Name,
    string? Description);
