using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.DTOs;
using TaskTracker.Application.Interfaces;

namespace TaskTracker.Infrastructure.ExternalServices;

public sealed partial class GeoapifyGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly GeocodingOptions _options;
    private readonly ILogger<GeoapifyGeocodingService> _logger;

    public GeoapifyGeocodingService(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<GeocodingOptions> options,
        ILogger<GeoapifyGeocodingService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Coordinates?> GeocodeAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return null;
        }

        var cacheKey = CreateCacheKey(address);
        if (TryGetCachedCoordinates(cacheKey, out var cachedCoordinates))
        {
            return cachedCoordinates;
        }

        var lookup = await RequestCoordinatesAsync(address, cancellationToken);
        if (lookup.ShouldCache)
        {
            CacheCoordinates(cacheKey, lookup.Coordinates);
        }

        return lookup.Coordinates;
    }

    private async Task<GeocodingLookup> RequestCoordinatesAsync(
        string address,
        CancellationToken cancellationToken)
    {
        try
        {
            var uri = BuildRequestUri(CollapseWhitespace(address));
            using var response = await _httpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return await ReadResponseAsync(response, cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Geoapify geocoding timed out.");
            return GeocodingLookup.TransientFailure;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Geoapify geocoding is unavailable.");
            return GeocodingLookup.TransientFailure;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Geoapify returned invalid JSON.");
            return GeocodingLookup.TransientFailure;
        }
    }

    private async Task<GeocodingLookup> ReadResponseAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning("Geoapify rate limit was exceeded.");
            return GeocodingLookup.TransientFailure;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Geoapify failed with HTTP status {StatusCode}.",
                (int)response.StatusCode);
            return GeocodingLookup.TransientFailure;
        }

        var payload = await response.Content.ReadFromJsonAsync<GeoapifyResponse>(
            cancellationToken: cancellationToken);

        var result = payload?.Results.FirstOrDefault();
        var coordinates = result is null
            ? null
            : new Coordinates(result.Latitude, result.Longitude);

        return GeocodingLookup.Cacheable(coordinates);
    }

    private string BuildRequestUri(string address)
    {
        return "v1/geocode/search" +
               $"?text={Uri.EscapeDataString(address)}" +
               "&format=json" +
               "&limit=1" +
               $"&lang={Uri.EscapeDataString(_options.Language)}" +
               $"&filter={Uri.EscapeDataString($"countrycode:{_options.CountryCode}")}" +
               $"&apiKey={Uri.EscapeDataString(_options.ApiKey)}";
    }

    private static string CollapseWhitespace(string text)
    {
        return WhitespaceRegex().Replace(text.Trim(), " ");
    }

    private static string CreateCacheKey(string address)
    {
        return $"geo:{CollapseWhitespace(address).ToLowerInvariant()}";
    }

    private sealed record CacheEntry(Coordinates? Coordinates);

    private bool TryGetCachedCoordinates(
        string cacheKey,
        out Coordinates? coordinates)
    {
        if (_cache.TryGetValue(cacheKey, out CacheEntry? cached))
        {
            coordinates = cached?.Coordinates;
            return true;
        }

        coordinates = null;
        return false;
    }

    private void CacheCoordinates(string cacheKey, Coordinates? coordinates)
    {
        _cache.Set(
            cacheKey,
            new CacheEntry(coordinates),
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                    coordinates is null
                        ? _options.NegativeCacheDurationMinutes
                        : _options.CacheDurationMinutes),
                Size = 1
            });
    }

    private sealed record GeocodingLookup(
        Coordinates? Coordinates,
        bool ShouldCache)
    {
        public static GeocodingLookup TransientFailure { get; } =
            new(null, false);

        public static GeocodingLookup Cacheable(Coordinates? coordinates) =>
            new(coordinates, true);
    }

    private sealed class GeoapifyResponse
    {
        [JsonPropertyName("results")]
        public IReadOnlyList<GeoapifyResult> Results { get; init; } = [];
    }

    private sealed class GeoapifyResult
    {
        [JsonPropertyName("lat")]
        public double Latitude { get; init; }

        [JsonPropertyName("lon")]
        public double Longitude { get; init; }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
