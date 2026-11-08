using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TaskTracker.Application.DTOs;
using TaskTracker.Infrastructure.ExternalServices;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace TaskTracker.UnitTests;

[Collection("WireMockTests")]
public sealed class GeoapifyGeocodingServiceWireMockTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 100 });

    private GeoapifyGeocodingService CreateService(TimeSpan? timeout = null)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(_server.Url! + "/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(2)
        };

        return new GeoapifyGeocodingService(
            client,
            _cache,
            Options.Create(new GeocodingOptions
            {
                ApiKey = "test-key",
                CacheDurationMinutes = 60,
                NegativeCacheDurationMinutes = 10
            }),
            NullLogger<GeoapifyGeocodingService>.Instance);
    }

    [Fact]
    public async Task GeocodeAsync_SuccessfulResponse_ReturnsCoordinates_AndUsesCache()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet()
                .WithParam("text", "Kyiv, Maidan")
                .WithParam("format", "json")
                .WithParam("limit", "1")
                .WithParam("lang", "uk")
                .WithParam("filter", "countrycode:ua")
                .WithParam("apiKey", "test-key"))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"results":[{"lat":50.4501,"lon":30.5234}]}"""));

        var service = CreateService();

        // Act - Call twice with varying whitespace and casing
        var first = await service.GeocodeAsync(" Kyiv, Maidan ");
        var second = await service.GeocodeAsync("kyiv, maidan");

        // Assert
        Assert.Equal(new Coordinates(50.4501, 30.5234), first);
        Assert.Equal(first, second);
        Assert.Single(_server.LogEntries); // 1 request received, second served from cache
    }

    [Fact]
    public async Task GeocodeAsync_EmptyResults_ReturnsNull_AndCachesNegativeResult()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"results":[]}"""));

        var service = CreateService();

        // Act
        var first = await service.GeocodeAsync("Unknown Street");
        var second = await service.GeocodeAsync("Unknown Street");

        // Assert
        Assert.Null(first);
        Assert.Null(second);
        Assert.Single(_server.LogEntries); // Negative cache prevents duplicate remote call
    }

    [Fact]
    public async Task GeocodeAsync_RateLimit429_ReturnsNull_AndDoesNotCache()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.TooManyRequests));

        var service = CreateService();

        // Act 1: 429
        var first = await service.GeocodeAsync("Lviv");
        Assert.Null(first);

        // Reset and prepare successful response
        _server.Reset();
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"results":[{"lat":49.8397,"lon":24.0297}]}"""));

        // Act 2: Should retry because 429 was NOT cached
        var second = await service.GeocodeAsync("Lviv");

        // Assert
        Assert.Equal(new Coordinates(49.8397, 24.0297), second);
    }

    [Fact]
    public async Task GeocodeAsync_ServerError500_ReturnsNull_AndDoesNotCache()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.InternalServerError));

        var service = CreateService();

        // Act 1
        var first = await service.GeocodeAsync("Odesa");
        Assert.Null(first);

        // Reset and succeed
        _server.Reset();
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"results":[{"lat":46.4825,"lon":30.7233}]}"""));

        var second = await service.GeocodeAsync("Odesa");

        Assert.Equal(new Coordinates(46.4825, 30.7233), second);
    }

    [Fact]
    public async Task GeocodeAsync_InvalidJson_ReturnsNull_AndDoesNotCache()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{ invalid: json ["));

        var service = CreateService();

        var first = await service.GeocodeAsync("Dnipro");
        Assert.Null(first);

        _server.Reset();
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"results":[{"lat":48.4647,"lon":35.0462}]}"""));

        var second = await service.GeocodeAsync("Dnipro");
        Assert.Equal(new Coordinates(48.4647, 35.0462), second);
    }

    [Fact]
    public async Task GeocodeAsync_Timeout_ReturnsNull_AndDoesNotCache()
    {
        // Arrange
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithDelay(TimeSpan.FromMilliseconds(500))
                .WithBody("""{"results":[{"lat":50.4501,"lon":30.5234}]}"""));

        // Timeout is shorter than server delay
        var service = CreateService(timeout: TimeSpan.FromMilliseconds(100));

        var timedOut = await service.GeocodeAsync("Kyiv");
        Assert.Null(timedOut);

        // Now with normal timeout
        _server.Reset();
        _server
            .Given(Request.Create()
                .WithPath("/v1/geocode/search")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"results":[{"lat":50.4501,"lon":30.5234}]}"""));

        var serviceNormal = CreateService(timeout: TimeSpan.FromSeconds(2));
        var second = await serviceNormal.GeocodeAsync("Kyiv");

        Assert.Equal(new Coordinates(50.4501, 30.5234), second);
    }

    [Fact]
    public async Task GeocodeAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var service = CreateService();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GeocodeAsync("Kyiv", cts.Token));
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
        _cache.Dispose();
    }
}
