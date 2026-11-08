using TaskTracker.Application.DTOs;

namespace TaskTracker.Application.Interfaces;

public interface IGeocodingService
{
    Task<Coordinates?> GeocodeAsync(
        string address,
        CancellationToken cancellationToken = default);
}
