using Garden.Core.Interfaces;
using Garden.Engine.Events;
using Garden.Engine.Generation;
using Garden.Engine.Random;
using Garden.Engine.Scheduling;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.Engine.Time;
using Garden.Infrastructure.Persistence;
using Garden.World.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Garden.Infrastructure.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddGardenInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<GardenDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        return services;
    }

    public static IServiceCollection AddGardenEngine(this IServiceCollection services)
    {
        services.AddSingleton<SimulationClock>();
        services.AddSingleton<WorldState>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<ISimulationScheduler, SimulationScheduler>();
        services.AddSingleton<SimulationCoordinator>();
        services.AddSingleton<WorldInitializer>();
        services.AddSingleton(_ => new SimulationRandom(Environment.TickCount));
        services.AddHostedService<SimulationHostedService>();

        services.AddSingleton<WeatherSystem>();
        services.AddSingleton<SeasonSystem>();
        services.AddSingleton<HydrologySystem>();
        services.AddSingleton<ResourceSystem>();
        services.AddSingleton<EcologySystem>();

        return services;
    }
}
