namespace TaskTracker.Infrastructure.ExternalServices;

public sealed class GeocodingOptions
{
    public const string SectionName = "Geocoding";

    public string BaseUrl { get; set; } = "https://api.geoapify.com/";
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 5;
    public int CacheDurationMinutes { get; set; } = 60;
    public int NegativeCacheDurationMinutes { get; set; } = 10;
    public long CacheSize { get; set; } = 1_000;
    public string Language { get; set; } = "uk";
    public string CountryCode { get; set; } = "ua";
}
