namespace TaskTracker.Application.DTOs;

/// <summary>
/// Представлення зчитаного файлу обкладинки з потоком та MIME-типом.
/// </summary>
public sealed record CoverImageFile(Stream Content, string ContentType);
