using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.DTOs.Events;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Exceptions;
using TaskTracker.Infrastructure.Persistence;
using TaskTracker.Infrastructure.Services;

namespace TaskTracker.UnitTests;

public sealed class EventServiceGeocodingIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly CountingTestGeocodingService _geocodingService;
    private readonly EventService _service;

    public EventServiceGeocodingIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _geocodingService = new CountingTestGeocodingService();
        var eventRepository = new TaskTracker.Infrastructure.Persistence.Repositories.EventRepository(_dbContext);
        _service = new EventService(_dbContext, eventRepository, _geocodingService);
    }

    [Fact]
    public async Task CreateAsync_ValidAddress_SetsCoordinatesFromService()
    {
        // Arrange
        var request = new CreateEventRequest(
            "ASP.NET Core Community Day",
            "Тестування геокодування",
            "Kyiv",
            DateTime.UtcNow.AddDays(10),
            120,
            1);

        // Act
        var created = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(50.4501, created.Latitude);
        Assert.Equal(30.5234, created.Longitude);
        Assert.Equal(1, _geocodingService.CallCount);
    }

    [Fact]
    public async Task CreateAsync_InvalidCategory_ThrowsNotFound_AndDoesNotCallGeocoding()
    {
        // Arrange
        var request = new CreateEventRequest(
            "Invalid Category Event",
            "Category check before geocoding",
            "Kyiv",
            DateTime.UtcNow.AddDays(10),
            50,
            999); // Unknown category

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.CreateAsync(request));

        // External API must NOT be called
        Assert.Equal(0, _geocodingService.CallCount);
    }

    [Fact]
    public async Task UpdateAsync_SameNormalizedAddress_PreservesCoordinatesWithoutCallingGeocoding()
    {
        // Arrange: Create event with Kyiv
        var created = await _service.CreateAsync(new CreateEventRequest(
            "Event", "Desc", "Kyiv", DateTime.UtcNow, 10, 1));

        Assert.Equal(1, _geocodingService.CallCount);

        // Act: Update title, location is same address with extra whitespace / different casing
        var updated = await _service.UpdateAsync(
            created.Id.ToString(),
            new UpdateEventRequest("Event Updated", "Desc", "  kyiv  ", DateTime.UtcNow, 10, 1));

        // Assert: Coordinates preserved, geocoding was NOT called again
        Assert.Equal(50.4501, updated.Latitude);
        Assert.Equal(30.5234, updated.Longitude);
        Assert.Equal(1, _geocodingService.CallCount); // Still 1 call!
    }

    [Fact]
    public async Task UpdateAsync_ChangedAddress_FetchesNewCoordinates_AndDoesNotRetainOldOnNotFound()
    {
        // Arrange: Create with Kyiv
        var created = await _service.CreateAsync(new CreateEventRequest(
            "Event", "Desc", "Kyiv", DateTime.UtcNow, 10, 1));

        // Act 1: Change to Kharkiv
        var updatedToKharkiv = await _service.UpdateAsync(
            created.Id.ToString(),
            new UpdateEventRequest("Event", "Desc", "Kharkiv", DateTime.UtcNow, 10, 1));

        Assert.Equal(49.9935, updatedToKharkiv.Latitude);
        Assert.Equal(36.2304, updatedToKharkiv.Longitude);

        // Act 2: Change to unknown address (returns null)
        var updatedToUnknown = await _service.UpdateAsync(
            created.Id.ToString(),
            new UpdateEventRequest("Event", "Desc", "Unknown Place", DateTime.UtcNow, 10, 1));

        // Assert: Old coordinates from Kharkiv are NOT retained; both are null
        Assert.Null(updatedToUnknown.Latitude);
        Assert.Null(updatedToUnknown.Longitude);
    }

    private sealed class CountingTestGeocodingService : IGeocodingService
    {
        public int CallCount { get; private set; }

        public Task<Coordinates?> GeocodeAsync(string address, CancellationToken cancellationToken = default)
        {
            CallCount++;
            var normalized = address.Trim().ToLowerInvariant();

            if (normalized.Contains("kyiv"))
            {
                return Task.FromResult<Coordinates?>(new Coordinates(50.4501, 30.5234));
            }

            if (normalized.Contains("kharkiv"))
            {
                return Task.FromResult<Coordinates?>(new Coordinates(49.9935, 36.2304));
            }

            return Task.FromResult<Coordinates?>(null);
        }
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
