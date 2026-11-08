using TaskTracker.Application.DTOs.Events;

namespace TaskTracker.Application.Interfaces;

public interface IEventService
{
    Task<EventDetailDto> CreateAsync(CreateEventRequest request, CancellationToken cancellationToken = default);
    Task<EventDetailDto> UpdateAsync(string idOrGuid, UpdateEventRequest request, CancellationToken cancellationToken = default);
    Task<EventDetailDto?> GetByIdAsync(string idOrGuid, CancellationToken cancellationToken = default);
}
