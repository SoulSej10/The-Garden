using Garden.Core.Interfaces;
using Garden.Engine.Events;
using Garden.Engine.Generation;
using Garden.Engine.Random;
using Garden.Engine.Scheduling;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.Engine.Time;
using Garden.Infrastructure.Persistence;
using Garden.Infrastructure.Services;
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

        services.AddHostedService<WorldPersistenceService>();
        services.AddHostedService<BackupService>();

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
        services.AddSingleton<CitizenSystem>();
        services.AddSingleton<AgingSystem>();
        services.AddSingleton<SpawnSystem>();
        services.AddSingleton<PopulationManager>();
        services.AddSingleton<SettlementManager>();
        services.AddSingleton<ConstructionSystem>();
        services.AddSingleton<AgricultureSystem>();
        services.AddSingleton<EconomySystem>();

        services.AddSingleton<HistoricalArchive>();
        services.AddSingleton<SignificanceEvaluator>();
        services.AddSingleton<TimelineService>();
        services.AddSingleton<MemoryService>();
        services.AddSingleton<StoryEngine>();
        services.AddSingleton<HistoryManager>();
        services.AddSingleton<HistorySystem>();

        services.AddSingleton<LeadershipService>();
        services.AddSingleton<GovernanceService>();
        services.AddSingleton<KingdomService>();
        services.AddSingleton<DiplomacyService>();
        services.AddSingleton<MigrationService>();
        services.AddSingleton<TradeRouteService>();
        services.AddSingleton<TechnologyService>();
        services.AddSingleton<CultureService>();
        services.AddSingleton<ReligionService>();
        services.AddSingleton<CivilizationSystem>();

        services.AddSingleton<SaveLoadService>();
        services.AddSingleton<NarrationService>();

        return services;
    }
}
