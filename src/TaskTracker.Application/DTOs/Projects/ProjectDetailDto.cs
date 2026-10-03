using TaskTracker.Application.DTOs.Tasks;

namespace TaskTracker.Application.DTOs.Projects;

public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    IReadOnlyList<TaskDto> Tasks);
