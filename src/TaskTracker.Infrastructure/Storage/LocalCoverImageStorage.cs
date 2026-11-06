using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Options;

namespace TaskTracker.Infrastructure.Storage;

public sealed class LocalCoverImageStorage : ICoverImageStorage
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    private readonly string _uploadDirectory;
    private readonly ILogger<LocalCoverImageStorage> _logger;

    public LocalCoverImageStorage(
        IOptions<FileStorageOptions> options,
        IHostEnvironment environment,
        ILogger<LocalCoverImageStorage> logger)
    {
        _logger = logger;
        var configuredPath = options.Value.UploadPath;
        _uploadDirectory = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : ResolveRelativePath(environment.ContentRootPath, configuredPath);
    }

    private static string ResolveRelativePath(string contentRootPath, string configuredPath)
    {
        var root = Path.GetFullPath(contentRootPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var resolved = Path.GetFullPath(Path.Combine(root, configuredPath));
        var rootPrefix = root + Path.DirectorySeparatorChar;

        if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "FileStorage:UploadPath must stay inside the application content root when it is relative.");
        }

        return resolved;
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        var normalizedExtension = extension.ToLowerInvariant();
        if (!ContentTypes.ContainsKey(normalizedExtension))
        {
            throw new ArgumentException("Unsupported cover image extension.", nameof(extension));
        }

        Directory.CreateDirectory(_uploadDirectory);
        var fileName = $"{Guid.NewGuid():N}{normalizedExtension}";
        var destination = Path.Combine(_uploadDirectory, fileName);

        await using var output = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);

        await content.CopyToAsync(output, cancellationToken);
        return fileName;
    }

    public Task<CoverImageFile?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        // Захист від path traversal: ім'я має бути простим іменем без сегментів каталогу
        if (!string.Equals(storagePath, Path.GetFileName(storagePath), StringComparison.Ordinal))
        {
            return Task.FromResult<CoverImageFile?>(null);
        }

        var fullPath = Path.Combine(_uploadDirectory, storagePath);
        if (!File.Exists(fullPath))
        {
            return Task.FromResult<CoverImageFile?>(null);
        }

        var extension = Path.GetExtension(storagePath);
        if (!ContentTypes.TryGetValue(extension, out var contentType))
        {
            return Task.FromResult<CoverImageFile?>(null);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult<CoverImageFile?>(new CoverImageFile(stream, contentType));
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return Task.CompletedTask;
        }

        var safeFileName = Path.GetFileName(storagePath);
        var fullPath = Path.Combine(_uploadDirectory, safeFileName);

        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete cover image file at {Path}", fullPath);
        }

        return Task.CompletedTask;
    }
}
