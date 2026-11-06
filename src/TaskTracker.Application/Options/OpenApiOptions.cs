namespace TaskTracker.Application.Options;

public sealed class OpenApiOptions
{
    public const string SectionName = "OpenApi";
    public string Title { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string? Description { get; init; }
}
