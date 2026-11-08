using TaskTracker.Application.Common;
using TaskTracker.Application.DTOs.Events;

namespace TaskTracker.Application.Interfaces;

public interface IEventService
{
    Task<PagedResult<EventDto>> GetPagedAsync(
        EventQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<EventDetailDto?> GetByIdAsync(
        string idOrGuid,
        CancellationToken cancellationToken = default);

    Task<EventDetailDto> CreateAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken = default);

    Task<EventDetailDto> UpdateAsync(
        string idOrGuid,
        UpdateEventRequest request,
        CancellationToken cancellationToken = default);

    Task<RegistrationDto> RegisterAttendeeAsync(
        string idOrGuid,
        CreateRegistrationRequest request,
        CancellationToken cancellationToken = default);
}
