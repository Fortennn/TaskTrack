using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskTracker.Application.Common.Interfaces;
using TaskTracker.Application.Interfaces;
using TaskTracker.Application.Options;
using TaskTracker.Infrastructure.Persistence;
using TaskTracker.Infrastructure.Persistence.Repositories;
using TaskTracker.Infrastructure.Services;

namespace TaskTracker.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider
                .GetRequiredService<IOptions<DatabaseOptions>>()
                .Value;
            var environment = serviceProvider.GetRequiredService<IHostEnvironment>();

            options.UseNpgsql(databaseOptions.ConnectionString);

            if (environment.IsDevelopment())
            {
                options.EnableDetailedErrors();
                options.LogTo(
                    Console.WriteLine,
                    [DbLoggerCategory.Database.Command.Name],
                    LogLevel.Information);
            }
        });

        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventService, EventService>();

        return services;
    }
}
