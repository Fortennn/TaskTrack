namespace TaskTracker.Application.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";
    public string UploadPath { get; init; } = "wwwroot/uploads";
    public int MaxFileSizeMb { get; init; } = 5;
    public long MaxFileSizeBytes => MaxFileSizeMb * 1024L * 1024L;
}
