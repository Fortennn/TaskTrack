namespace TaskTracker.Application.Interfaces;

public interface ITaskNotificationSender
{
    Task SendAsync(
        Guid taskId,
        string taskTitle,
        string? recipientEmail,
        CancellationToken cancellationToken);
}
