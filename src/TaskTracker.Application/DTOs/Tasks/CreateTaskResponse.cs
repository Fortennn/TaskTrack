namespace TaskTracker.Application.DTOs.Tasks;

public sealed record CreateTaskResponse(
    TaskDetailDto Task,
    Guid JobId);
