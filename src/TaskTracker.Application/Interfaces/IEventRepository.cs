using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Models;

namespace TaskTracker.Application.Interfaces;

public interface IEventRepository
{
    Task<(IReadOnlyList<EventListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? search,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken = default);

    Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Event?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default);
    Task AddAsync(Event eventItem, CancellationToken cancellationToken = default);
    Task UpdateAsync(Event eventItem, CancellationToken cancellationToken = default);
    Task AddRegistrationAsync(Registration registration, CancellationToken cancellationToken = default);
}
