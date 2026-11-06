namespace TaskTracker.Application.Options;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";
    public string ConnectionString { get; set; } = string.Empty;
}
