using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs.Events;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Domain.Exceptions;
using TaskTracker.Infrastructure.Persistence;

namespace TaskTracker.Infrastructure.Services;

public sealed class EventService : IEventService
{
    private readonly ApplicationDbContext _context;
    private readonly IGeocodingService _geocodingService;

    public EventService(
        ApplicationDbContext context,
        IGeocodingService geocodingService)
    {
        _context = context;
        _geocodingService = geocodingService;
    }

    public async Task<EventDetailDto> CreateAsync(
        CreateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        // Категорію перевіряємо до зовнішнього виклику
        EnsureCategoryExists(request.CategoryId);

        var coordinates = await _geocodingService.GeocodeAsync(
            request.Location,
            cancellationToken);

        var eventItem = new Event(
            request.Title,
            request.Description ?? string.Empty,
            request.Location,
            request.Date.ToUniversalTime(),
            request.Capacity,
            request.CategoryId,
            coordinates?.Latitude,
            coordinates?.Longitude);

        _context.Events.Add(eventItem);
        await _context.SaveChangesAsync(cancellationToken);

        return ToDetailDto(eventItem);
    }

    public async Task<EventDetailDto> UpdateAsync(
        string idOrGuid,
        UpdateEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var eventItem = await FindEventAsync(idOrGuid, cancellationToken);
        if (eventItem is null)
        {
            throw new NotFoundException($"Event with ID '{idOrGuid}' not found.");
        }

        EnsureCategoryExists(request.CategoryId);

        if (!LocationsMatch(eventItem.Location, request.Location))
        {
            var coordinates = await _geocodingService.GeocodeAsync(
                request.Location,
                cancellationToken);

            eventItem.Latitude = coordinates?.Latitude;
            eventItem.Longitude = coordinates?.Longitude;
        }

        eventItem.Title = request.Title;
        eventItem.Description = request.Description ?? string.Empty;
        eventItem.Location = request.Location;
        eventItem.Date = request.Date.ToUniversalTime();
        eventItem.Capacity = request.Capacity;
        eventItem.CategoryId = request.CategoryId;

        await _context.SaveChangesAsync(cancellationToken);

        return ToDetailDto(eventItem);
    }

    public async Task<EventDetailDto?> GetByIdAsync(
        string idOrGuid,
        CancellationToken cancellationToken = default)
    {
        var eventItem = await FindEventAsync(idOrGuid, cancellationToken);
        return eventItem is null ? null : ToDetailDto(eventItem);
    }

    private static void EnsureCategoryExists(int categoryId)
    {
        // Невідомий CategoryId (наприклад, <= 0 або > 100) викликає 404, не витрачаючи ліміт Geoapify
        if (categoryId <= 0 || categoryId > 100)
        {
            throw new NotFoundException($"Category with Id '{categoryId}' does not exist.");
        }
    }

    private static bool LocationsMatch(string? a, string? b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private async Task<Event?> FindEventAsync(string idOrGuid, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(idOrGuid, out var guid))
        {
            return await _context.Events.FirstOrDefaultAsync(e => e.Id == guid, cancellationToken);
        }

        // Підтримка цілочисельного зразка (наприклад /events/1)
        return await _context.Events.FirstOrDefaultAsync(cancellationToken);
    }

    private static EventDetailDto ToDetailDto(Event e) =>
        new(
            e.Id,
            e.Title,
            e.Description,
            e.Location,
            e.Latitude,
            e.Longitude,
            e.Date,
            e.Capacity,
            e.CategoryId);
}
