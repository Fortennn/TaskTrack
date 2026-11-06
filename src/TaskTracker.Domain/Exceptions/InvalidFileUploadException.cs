namespace TaskTracker.Domain.Exceptions;

public sealed class InvalidFileUploadException(string message) : Exception(message);
