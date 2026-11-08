using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Models;

namespace TaskTracker.Infrastructure.Persistence.Repositories;

public sealed class EventRepository(ApplicationDbContext dbContext) : IEventRepository
{
    public async Task<(IReadOnlyList<EventListItem> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        int? categoryId,
        DateOnly? dateFrom,
        DateOnly? dateTo,
        string? search,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Events.AsNoTracking();

        if (categoryId.HasValue)
        {
            query = query.Where(e => e.CategoryId == categoryId.Value);
        }

        if (dateFrom.HasValue)
        {
            var fromUtc = dateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(e => e.Date >= fromUtc);
        }

        if (dateTo.HasValue)
        {
            var toUtc = dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(e => e.Date < toUtc);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = search.Trim();
            query = dbContext.Database.IsNpgsql()
                ? query.Where(e => EF.Functions.ILike(e.Title, $"%{pattern}%"))
                : query.Where(e => e.Title.Contains(pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        IOrderedQueryable<Event> orderedQuery = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("title", false) => query.OrderBy(e => e.Title).ThenBy(e => e.Id),
            ("title", true) => query.OrderByDescending(e => e.Title).ThenByDescending(e => e.Id),
            ("capacity", false) => query.OrderBy(e => e.Capacity).ThenBy(e => e.Id),
            ("capacity", true) => query.OrderByDescending(e => e.Capacity).ThenByDescending(e => e.Id),
            ("date", true) => query.OrderByDescending(e => e.Date).ThenByDescending(e => e.Id),
            _ => query.OrderBy(e => e.Date).ThenBy(e => e.Id)
        };

        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(eventItem => new EventListItem(
                eventItem,
                eventItem.Category!.Name,
                eventItem.Registrations.Count))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Event?> GetDetailsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Events
            .AsNoTracking()
            .Include(e => e.Category)
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .AnyAsync(c => c.Id == categoryId, cancellationToken);
    }

    public async Task AddAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        await dbContext.Events.AddAsync(eventItem, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Event eventItem, CancellationToken cancellationToken = default)
    {
        dbContext.Events.Update(eventItem);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRegistrationAsync(Registration registration, CancellationToken cancellationToken = default)
    {
        await dbContext.Registrations.AddAsync(registration, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
