using TaskTracker.Application.DTOs;
using TaskTracker.Domain.Exceptions;

namespace TaskTracker.Application.Validation;

public sealed class CoverImageUploadValidator(int maxFileSizeMb)
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedImageExtensions =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = [".jpg", ".jpeg"],
            ["image/png"] = [".png"]
        };

    private readonly long _maxFileSizeBytes = maxFileSizeMb * 1024L * 1024L;

    public string ValidateAndGetExtension(CoverImageUpload? upload)
    {
        if (upload is null || upload.Length == 0)
        {
            throw new InvalidFileUploadException("Select a non-empty JPEG or PNG file.");
        }

        if (upload.Length > _maxFileSizeBytes)
        {
            throw new InvalidFileUploadException($"The cover image must not exceed {maxFileSizeMb} MB.");
        }

        if (!AllowedImageExtensions.TryGetValue(upload.ContentType, out var extensions))
        {
            throw new InvalidFileUploadException("Only JPEG and PNG cover images are supported.");
        }

        var extension = Path.GetExtension(upload.FileName);
        if (string.IsNullOrEmpty(extension) || !extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidFileUploadException("The file extension does not match the declared image type.");
        }

        return extension.ToLowerInvariant();
    }
}
