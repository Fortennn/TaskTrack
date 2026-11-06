using TaskTracker.Application.Interfaces;

namespace TaskTracker.Api.BackgroundJobs;

public sealed class LoggingRegistrationConfirmationSender(
    ILogger<LoggingRegistrationConfirmationSender> logger)
    : IRegistrationConfirmationSender, ITaskNotificationSender
{
    public async Task SendAsync(
        string attendeeEmail,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Sending registration confirmation to {AttendeeEmail}...",
            attendeeEmail);

        // Керована імітація помилки для самостійного завдання
        if (attendeeEmail.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Simulated delivery failure triggered for {AttendeeEmail}.", attendeeEmail);
            throw new InvalidOperationException($"Simulated delivery failure for '{attendeeEmail}'.");
        }

        await Task.Delay(
            TimeSpan.FromSeconds(2),
            cancellationToken);

        logger.LogInformation(
            "Registration confirmation sent to {AttendeeEmail}.",
            attendeeEmail);
    }

    public async Task SendAsync(
        Guid taskId,
        string taskTitle,
        string? recipientEmail,
        CancellationToken cancellationToken)
    {
        var email = string.IsNullOrWhiteSpace(recipientEmail) ? "task-assignee@example.com" : recipientEmail;

        logger.LogInformation(
            "Sending task confirmation for task {TaskId} to {AttendeeEmail}...",
            taskId,
            email);

        // Керована імітація помилки для самостійного завдання
        if (email.Contains("fail", StringComparison.OrdinalIgnoreCase) ||
            taskTitle.Contains("[fail]", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Simulated task notification failure triggered for task {TaskId} ({TaskTitle}).", taskId, taskTitle);
            throw new InvalidOperationException($"Simulated task notification failure for '{taskTitle}'.");
        }

        await Task.Delay(
            TimeSpan.FromSeconds(2),
            cancellationToken);

        logger.LogInformation(
            "Task confirmation sent to {AttendeeEmail}.",
            email);
    }
}
