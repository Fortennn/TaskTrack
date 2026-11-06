namespace TaskTracker.Application.DTOs;

/// <summary>
/// Прикладна модель завантажуваного файлу, незалежна від деталей протоколу HTTP.
/// </summary>
public sealed record CoverImageUpload(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);
