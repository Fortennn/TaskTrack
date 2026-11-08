using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence;

public sealed class PerformanceDataSeeder(
    ApplicationDbContext dbContext,
    ILogger<PerformanceDataSeeder> logger)
{
    private const string CategoryPrefix = "Performance data ";
    private const int CategoryCount = 10;
    private static readonly DateTimeOffset FirstEventDate =
        new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public async Task SeedAsync(
        int eventCount,
        int batchSize,
        bool reset,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(eventCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var existingCategoryIds = await dbContext.Categories
            .Where(category => category.Name.StartsWith(CategoryPrefix))
            .Select(category => category.Id)
            .ToListAsync(cancellationToken);

        if (existingCategoryIds.Count > 0 && !reset)
        {
            logger.LogInformation(
                "Performance data already exists. Run with PerformanceSeed:Reset=true to replace it.");
            return;
        }

        if (existingCategoryIds.Count > 0)
        {
            await dbContext.Events
                .Where(eventItem => existingCategoryIds.Contains(eventItem.CategoryId))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.Categories
                .Where(category => existingCategoryIds.Contains(category.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        var categories = Enumerable.Range(1, CategoryCount)
            .Select(number => new Category
            {
                Name = $"{CategoryPrefix}{number:D2}"
            })
            .ToList();

        dbContext.Categories.AddRange(categories);
        await dbContext.SaveChangesAsync(cancellationToken);

        var categoryIds = categories.Select(category => category.Id).ToArray();
        dbContext.ChangeTracker.Clear();

        for (var offset = 0; offset < eventCount; offset += batchSize)
        {
            var currentBatchSize = Math.Min(batchSize, eventCount - offset);
            var events = Enumerable.Range(offset, currentBatchSize)
                .Select(index => CreateEvent(index, categoryIds))
                .ToList();

            dbContext.Events.AddRange(events);
            await dbContext.SaveChangesAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            logger.LogInformation(
                "Seeded {SeededCount} of {EventCount} performance events.",
                offset + currentBatchSize,
                eventCount);
        }
    }

    private static Event CreateEvent(int index, IReadOnlyList<int> categoryIds)
    {
        return new Event
        {
            Title = $"Performance event {index + 1:D6}",
            Description = "Generated data for measuring EventHub database queries.",
            Location = $"Test venue {(index % 100) + 1:D3}",
            Date = FirstEventDate.AddMinutes(index * 53L).UtcDateTime,
            Capacity = 50 + index % 451,
            CategoryId = categoryIds[index % categoryIds.Count],
            CreatedAt = FirstEventDate.AddMinutes(index).UtcDateTime
        };
    }
}
