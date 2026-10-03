using Scalar.AspNetCore;
using TaskTracker.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Регистрация слоя Infrastructure (DbContext и репозитории)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    
    // Перенаправление с корневого URL на интерактивный интерфейс документации
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
