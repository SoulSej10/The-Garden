using System.Threading.RateLimiting;
using Garden.Api;
using Garden.Api.Hubs;
using Garden.Api.Services;
using Garden.Engine.Generation;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.Infrastructure.Configuration;
using Garden.Infrastructure.Persistence;
using Garden.World.Collections;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// RFC-018: TG-DEV-009's "API versioning" - a single global route prefix
// applied via convention rather than editing every controller.
builder.Services.AddControllers(options =>
    options.Conventions.Add(new RoutePrefixConvention("v1")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

// RFC-018: TG-DEV-009's "rate limiting" - one global fixed-window policy,
// partitioned per client IP so one heavy Observatory session can't starve
// every other client. 300/minute is generous enough for the Observatory's
// own polling cadence (the busiest page polls every 5s across ~6 queries).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddGardenInfrastructure(builder.Configuration);
builder.Services.AddGardenEngine();
builder.Services.AddSingleton<BroadcastService>();
builder.Services.AddHostedService<SignalRBroadcastService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GardenDbContext>();
    try
    {
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database migration failed; continuing without persistence");
    }

    var initializer = scope.ServiceProvider.GetRequiredService<WorldInitializer>();
    var coordinator = scope.ServiceProvider.GetRequiredService<SimulationCoordinator>();
    var scheduler = scope.ServiceProvider.GetRequiredService<Garden.Core.Interfaces.ISimulationScheduler>();

    scheduler.Register(scope.ServiceProvider.GetRequiredService<WeatherSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<SeasonSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<HydrologySystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<ResourceSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<EcologySystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<CitizenSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<EmotionSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<RelationshipSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<CommunicationSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<LanguageSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<EducationSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<LawSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<TerritorySystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<PopulationEcologySystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<DiseaseSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<EvolutionSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<DecomposerSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<FaunaSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<WarfareSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<InfrastructureSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<AgingSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<ReproductionSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<ConstructionSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<AgricultureSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<EconomySystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<HistorySystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<LegendSystem>());
    scheduler.Register(scope.ServiceProvider.GetRequiredService<CivilizationSystem>());

    initializer.Initialize(width: 100, height: 100, seed: 42);

    var worldState = scope.ServiceProvider.GetRequiredService<WorldState>();
    var progLog = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // A world only counts as "resumable" if it still has someone alive in it.
    // Previously this checked db.Citizens.AnyAsync(), which is true even when
    // every saved citizen is dead - since CitizenSystem only ever acts on
    // living citizens, reloading an all-dead snapshot produced a "zombie
    // world": the clock kept ticking forever (matching reports of the
    // simulation sitting at Year 3+ with population stuck at 0) while no AI,
    // reproduction, or history ever ran again, because there was no one left
    // to run it on. Every subsequent restart reloaded the same corpses.
    var hasViablePopulation = await db.Citizens.AnyAsync(c => c.IsAlive);

    if (hasViablePopulation)
    {
        var existingCitizens = await db.Citizens.ToListAsync();
        worldState.Citizens.Clear();
        worldState.Citizens.AddRange(existingCitizens);

        var existingSettlements = await db.Settlements.ToListAsync();
        worldState.Settlements.Clear();
        worldState.Settlements.AddRange(existingSettlements);

        progLog.LogInformation(
            "Loaded {CitizenCount} citizens and {SettlementCount} settlements from database",
            existingCitizens.Count, existingSettlements.Count);
    }
    else
    {
        var staleCitizens = await db.Citizens.CountAsync();
        if (staleCitizens > 0)
        {
            progLog.LogWarning(
                "Discarding a fully-dead saved world ({Count} citizens, all deceased) and starting fresh",
                staleCitizens);

            // Same fix as SystemController.ResetWorld: a settlement/citizen
            // that's been running for a while has accumulated child rows
            // (CulturalTrait, Building, Building_Items, Settlements_Items,
            // CitizenMemory) that EF's owned-entity cascade tracking doesn't
            // reliably reach, so plain ExecuteDeleteAsync on Citizens/
            // Settlements 23503s on the first foreign key it hits - this
            // startup path crashed on every fully-dead world with any real
            // history, defeating the entire point of auto-recovery.
            await db.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""CitizenMemory"";
                DELETE FROM ""Building_Items"";
                DELETE FROM ""Building"";
                DELETE FROM ""Settlements_Items"";
                DELETE FROM ""CulturalTrait"";
                DELETE FROM ""Citizens"";
                DELETE FROM ""Settlements"";
            ");
        }

        var spawnSystem = scope.ServiceProvider.GetRequiredService<SpawnSystem>();
        spawnSystem.SpawnInitialPopulation(count: 50);
    }
}

app.UseCors();
app.UseRateLimiter();
app.MapControllers();
app.MapHub<SimulationHub>("/simulationHub");
app.MapHub<EnvironmentHub>("/environmentHub");
app.MapHub<CitizenHub>("/citizenHub");
app.MapHub<SettlementHub>("/settlementHub");
app.MapHub<HistoryHub>("/historyHub");

app.Run();
