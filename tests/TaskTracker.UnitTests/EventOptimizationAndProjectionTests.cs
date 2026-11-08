using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.DTOs.Events;
using TaskTracker.Application.Interfaces;
using TaskTracker.Domain.Entities;
using TaskTracker.Infrastructure.Persistence;
using TaskTracker.Infrastructure.Persistence.Repositories;
using TaskTracker.Infrastructure.Services;
using Xunit;

namespace TaskTracker.UnitTests;

public sealed class EventOptimizationAndProjectionTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly EventRepository _repository;
    private readonly EventService _service;

    public EventOptimizationAndProjectionTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _repository = new EventRepository(_dbContext);
        _service = new EventService(_dbContext, _repository, new TestGeocodingService());
    }

    private sealed class TestGeocodingService : IGeocodingService
    {
        public Task<Coordinates?> GeocodeAsync(string address, CancellationToken cancellationToken = default) =>
            Task.FromResult<Coordinates?>(new Coordinates(50.45, 30.52));
    }

    [Fact]
    public void EventQueryParameters_PageSizeGreaterThan100_ValidationFails()
    {
        var parameters = new EventQueryParameters
        {
            PageSize = 101
        };

        var validationContext = new ValidationContext(parameters);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(parameters, validationContext, validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(EventQueryParameters.PageSize)));
    }

    [Fact]
    public void EventQueryParameters_PageSizeWithinBounds_ValidationSucceeds()
    {
        var parameters = new EventQueryParameters
        {
            PageSize = 20,
            Page = 1
        };

        var validationContext = new ValidationContext(parameters);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(parameters, validationContext, validationResults, validateAllProperties: true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public async Task GetPagedAsync_WithRegistrations_ProjectsCountWithoutLoadingFullRegistrationEntities()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Tech Conferences" };
        _dbContext.Categories.Add(category);

        var eventItem = new Event(
            "Projection workshop",
            "Checking RegistrationCount in the event list.",
            "Kyiv, Khreshchatyk 1",
            new DateTime(2035, 9, 1, 10, 0, 0, DateTimeKind.Utc),
            30,
            category.Id);

        _dbContext.Events.Add(eventItem);
        await _dbContext.SaveChangesAsync();

        await _service.RegisterAttendeeAsync(
            eventItem.Id.ToString(),
            new CreateRegistrationRequest("Olena", "olena@example.com"));

        await _service.RegisterAttendeeAsync(
            eventItem.Id.ToString(),
            new CreateRegistrationRequest("Taras", "taras@example.com"));

        // Act
        var result = await _service.GetPagedAsync(new EventQueryParameters
        {
            Search = "Projection workshop",
            Page = 1,
            PageSize = 20
        });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal("Projection workshop", item.Title);
        Assert.Equal("Tech Conferences", item.CategoryName);
        Assert.Equal(2, item.RegistrationCount);
    }

    [Fact]
    public async Task GetPagedAsync_DateRange_FiltersCorrectEvents()
    {
        // Arrange
        var category = new Category { Id = 2, Name = "Community" };
        _dbContext.Categories.Add(category);

        var eventDateMatch = new Event(
            "Event on June 15",
            "Matching date event",
            "Kyiv",
            new DateTime(2035, 6, 15, 14, 0, 0, DateTimeKind.Utc),
            50,
            category.Id);

        var eventDateOther = new Event(
            "Event on June 17",
            "Other date event",
            "Lviv",
            new DateTime(2035, 6, 17, 10, 0, 0, DateTimeKind.Utc),
            50,
            category.Id);

        _dbContext.Events.AddRange(eventDateMatch, eventDateOther);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetPagedAsync(new EventQueryParameters
        {
            DateFrom = new DateOnly(2035, 6, 15),
            DateTo = new DateOnly(2035, 6, 15),
            SortBy = "date",
            Order = "asc"
        });

        // Assert
        Assert.NotNull(result);
        var item = Assert.Single(result.Items);
        Assert.Equal("Event on June 15", item.Title);
    }
}
