using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Common.Interfaces;

public interface ICoverImageStorage
{
    Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default);
    Task<CoverImageFile?> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
