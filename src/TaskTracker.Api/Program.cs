using Scalar.AspNetCore;
using TaskTracker.Api.Middleware;
using TaskTracker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Реєстрація сервісу Problem Details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Type = null;
});

// Реєстрація шару Infrastructure (DbContext та репозиторії)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Глобальний middleware обробки винятків та сторінки статус-кодів
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    
    // Перенаправлення з кореневого URL на інтерактивний інтерфейс документації
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
