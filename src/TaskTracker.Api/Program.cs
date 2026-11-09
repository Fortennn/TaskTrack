using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;
using TaskTracker.Api.BackgroundJobs;
using TaskTracker.Api.Middleware;
using TaskTracker.Application;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;
using TaskTracker.Application.Validation;
using TaskTracker.Infrastructure;
using TaskTracker.Infrastructure.ExternalServices;
using TaskTracker.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// 1. Реєстрація сервісу Problem Details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Type = null;
});

// 2. Типізована конфігурація бази даних (DatabaseOptions) з перевіркою під час запуску
builder.Services
    .AddOptions<DatabaseOptions>()
    .Configure<IConfiguration>((options, configuration) =>
        options.ConnectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty)
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "ConnectionStrings:DefaultConnection is required. " +
        "Use .NET user secrets locally or an environment variable in deployed environments.")
    .ValidateOnStart();

// 3. Типізована конфігурація файлового сховища (FileStorageOptions)
builder.Services
    .AddOptions<FileStorageOptions>()
    .Bind(builder.Configuration.GetSection(FileStorageOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.UploadPath),
        "FileStorage:UploadPath is required.")
    .Validate(
        options => options.MaxFileSizeMb is > 0 and <= 100,
        "FileStorage:MaxFileSizeMb must be between 1 and 100.")
    .ValidateOnStart();

// 4. Типізована конфігурація OpenAPI (OpenApiOptions) — Самостійне завдання
builder.Services
    .AddOptions<OpenApiOptions>()
    .Bind(builder.Configuration.GetSection(OpenApiOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Title),
        "OpenApi:Title is required.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Version),
        "OpenApi:Version is required.")
    .ValidateOnStart();

// 5. Кеш у пам'яті (IMemoryCache) з обмеженням розміру
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = builder.Configuration.GetValue<long?>(
        $"{GeocodingOptions.SectionName}:CacheSize") ?? 1_000;
});

// 6. Типізована конфігурація геокодування (GeocodingOptions) — Практична робота 10
builder.Services
    .AddOptions<GeocodingOptions>()
    .Bind(builder.Configuration.GetSection(GeocodingOptions.SectionName))
    .Validate(
        options => Uri.TryCreate(
            options.BaseUrl,
            UriKind.Absolute,
            out var baseUrl) &&
            baseUrl.Scheme == Uri.UriSchemeHttps,
        "Geocoding:BaseUrl must be an absolute HTTPS URL.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ApiKey),
        "Geocoding:ApiKey is required.")
    .Validate(
        options => options.TimeoutSeconds is > 0 and <= 60,
        "Geocoding:TimeoutSeconds must be between 1 and 60.")
    .Validate(
        options => options.CacheDurationMinutes > 0,
        "Geocoding:CacheDurationMinutes must be greater than 0.")
    .Validate(
        options => options.NegativeCacheDurationMinutes > 0,
        "Geocoding:NegativeCacheDurationMinutes must be greater than 0.")
    .Validate(
        options => options.CacheSize > 0,
        "Geocoding:CacheSize must be greater than 0.")
    .ValidateOnStart();

// 7. Typed HttpClient для зовнішнього HTTP API (Geoapify) через IHttpClientFactory
builder.Services.AddHttpClient<IGeocodingService, GeoapifyGeocodingService>(
    (serviceProvider, client) =>
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<GeocodingOptions>>()
            .Value;

        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("TaskTracker/1.0");
    });

// Обмеження розміру multipart/form-data на рівні сервера
builder.Services.Configure<FormOptions>(options =>
{
    var maxFileSizeMb = builder.Configuration.GetValue<int?>(
        $"{FileStorageOptions.SectionName}:MaxFileSizeMb") ?? 5;
    options.MultipartBodyLengthLimit = maxFileSizeMb * 1024L * 1024L;
});

// Реєстрація адаптера файлового сховища та валідатора завантажень
builder.Services.AddSingleton<ICoverImageStorage, LocalCoverImageStorage>();
builder.Services.AddSingleton(serviceProvider =>
{
    var options = serviceProvider
        .GetRequiredService<IOptions<FileStorageOptions>>()
        .Value;
    return new CoverImageUploadValidator(options.MaxFileSizeMb);
});

// 8. Черга фонових задач, потокобезпечне сховище станів та фоновий воркер
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddSingleton<IJobStatusStore, InMemoryJobStatusStore>();
builder.Services.AddSingleton<LoggingRegistrationConfirmationSender>();
builder.Services.AddSingleton<ITaskNotificationSender>(sp => sp.GetRequiredService<LoggingRegistrationConfirmationSender>());
builder.Services.AddSingleton<IRegistrationConfirmationSender>(sp => sp.GetRequiredService<LoggingRegistrationConfirmationSender>());
builder.Services.AddHostedService<BackgroundTaskWorker>();

// 9. Реєстрація шарів Application та Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

// 10. Генератор OpenAPI (Swashbuckle) — конфігурація береться з типізованого OpenApiOptions
builder.Services.AddSwaggerGen(options =>
{
    var openApiOptions = builder.Configuration
        .GetSection(OpenApiOptions.SectionName)
        .Get<OpenApiOptions>() ?? new OpenApiOptions();

    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = openApiOptions.Title,
        Version = openApiOptions.Version,
        Description = openApiOptions.Description
    });

    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, "TaskTracker.Api.xml");
    if (File.Exists(apiXmlPath))
    {
        options.IncludeXmlComments(apiXmlPath);
    }

    var appXmlPath = Path.Combine(AppContext.BaseDirectory, "TaskTracker.Application.xml");
    if (File.Exists(appXmlPath))
    {
        options.IncludeXmlComments(appXmlPath);
    }
});

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("PerformanceSeed:Enabled"))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Performance data can be seeded only in the Development environment.");
    }

    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<TaskTracker.Infrastructure.Persistence.PerformanceDataSeeder>();
    var eventCount = builder.Configuration.GetValue(
        "PerformanceSeed:EventCount",
        100_000);
    var batchSize = builder.Configuration.GetValue(
        "PerformanceSeed:BatchSize",
        2_000);
    var reset = builder.Configuration.GetValue<bool>("PerformanceSeed:Reset");

    await seeder.SeedAsync(eventCount, batchSize, reset);
    return;
}

// Глобальний middleware обробки винятків та сторінки статус-кодів
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();

// Swagger UI доступний виключно в середовищі Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskTracker API v1");
        options.DefaultModelRendering(ModelRendering.Example);
        options.DefaultModelsExpandDepth(-1);
        options.DocExpansion(DocExpansion.List);
        options.DisplayRequestDuration();
    });
    
    // Маршрут /admin перенаправляє на Swagger UI
    app.MapGet("/admin", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Автоматична ініціалізація базових користувачів, якщо таблиця порожня
using (var seedScope = app.Services.CreateScope())
{
    var dbContext = seedScope.ServiceProvider.GetRequiredService<TaskTracker.Infrastructure.Persistence.ApplicationDbContext>();
    if (!dbContext.Users.Any())
    {
        dbContext.Users.AddRange(
            new TaskTracker.Domain.Entities.User("Адміністратор", "admin@tasktracker.local", TaskTracker.Domain.Enums.Role.Admin),
            new TaskTracker.Domain.Entities.User("Олександр Петренко", "olexandr@tasktracker.local", TaskTracker.Domain.Enums.Role.Member));
        await dbContext.SaveChangesAsync();
    }
}

app.Run();
