using Microsoft.OpenApi;
using TaskTracker.Api.Middleware;
using TaskTracker.Application;
using TaskTracker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Реєстрація сервісу Problem Details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Type = null;
});

// Реєстрація шарів Application та Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Реєстрація Swashbuckle OpenAPI генератора з метаданими та XML-документацією
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

// Swagger UI доступний виключно в середовищі розробки (Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskTracker API v1");
    });
    
    // Перенаправлення з кореневого URL на Swagger UI
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
