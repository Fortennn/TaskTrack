using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;
using TaskTracker.Api.Middleware;
using TaskTracker.Application;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.Options;
using TaskTracker.Application.Validation;
using TaskTracker.Infrastructure;
using TaskTracker.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

// 1. Реєстрація сервісу Problem Details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Type = null;
});

// 2. Налаштування сховища файлів (Strongly Typed Options) та валідація на старті
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

// 3. Реєстрація шарів Application та Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 4. Генератор OpenAPI (Swashbuckle)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskTracker API",
        Version = "v1",
        Description = "Платформа для управління проєктами, задачами та коментарями (Clean Architecture, ASP.NET Core .NET 10)"
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

// Глобальний middleware обробки винятків та сторінки статус-кодів
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();

// Swagger UI доступний у Development з оптимізованим відображенням зразків замість розкритих схем
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskTracker API v1");
        
        // Зручність відображення: одразу показувати компактний JSON-приклад (Example Value), а не схему з типами
        options.DefaultModelRendering(ModelRendering.Example);
        options.DefaultModelsExpandDepth(-1); // Згортає нижній блок Schemas, прибираючи візуальний шум
        options.DocExpansion(DocExpansion.List); // Список ендпоінтів розгорнуто компактно
        options.DisplayRequestDuration(); // Показує час виконання запиту
    });
    
    // Перенаправлення з кореневого URL на Swagger UI
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();

// Роздача статичних файлів з wwwroot
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

app.Run();
