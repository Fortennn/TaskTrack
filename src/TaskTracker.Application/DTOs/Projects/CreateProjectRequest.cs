using System.ComponentModel.DataAnnotations;

namespace TaskTracker.Application.DTOs.Projects;

public sealed record CreateProjectRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(1000)] string? Description = null);
