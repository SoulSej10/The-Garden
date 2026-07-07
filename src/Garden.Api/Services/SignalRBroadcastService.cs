using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Engine.Services;
using Garden.World.Collections;

namespace Garden.Api.Services;

public class SignalRBroadcastService : BackgroundService
{
    private readonly BroadcastService _broadcast;
    private readonly IEventBus _eventBus;
    private readonly WorldState _worldState;
    private readonly SimulationCoordinator _coordinator;
    private readonly ILogger<SignalRBroadcastService> _logger;

    public SignalRBroadcastService(
        BroadcastService broadcast,
        IEventBus eventBus,
        WorldState worldState,
        SimulationCoordinator coordinator,
        ILogger<SignalRBroadcastService> logger)
    {
        _broadcast = broadcast;
        _eventBus = eventBus;
        _worldState = worldState;
        _coordinator = coordinator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventBus.Subscribe<CitizenSpawnedEvent>(e =>
        {
            _ = _broadcast.CitizenBorn(e.CitizenName);
        });

        _eventBus.Subscribe<CitizenDiedEvent>(e =>
        {
            _ = _broadcast.CitizenDied(e.CitizenName, e.AgeAtDeath, e.CauseOfDeath);
        });

        _eventBus.Subscribe<SettlementFoundedEvent>(e =>
        {
            _ = _broadcast.SettlementFounded(e.SettlementName, e.TileX, e.TileY);
            _ = _broadcast.SignificantEvent(
                $"Settlement Founded: {e.SettlementName}",
                $"{e.FounderName} founded {e.SettlementName}.",
                "Settlement", "High");
        });

        _eventBus.Subscribe<BuildingCompletedEvent>(e =>
        {
            _ = _broadcast.BuildingCompleted(e.SettlementName, e.BuildingType);
        });

        _eventBus.Subscribe<FarmHarvestedEvent>(e =>
        {
            _ = _broadcast.SignificantEvent(
                $"Harvest at {e.SettlementName}",
                $"{e.CropType} harvest yielded {e.Yield:F1} units.",
                "Harvest", "Normal");
        });

        _ = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var time = _worldState.CurrentTime;
                    var alive = _worldState.Citizens.Count(c => c.IsAlive);
                    await _broadcast.SimulationTick(time.Tick, !_coordinator.IsRunning, (int)_coordinator.TargetSpeed);
                    await _broadcast.PopulationChanged(
                        _worldState.Citizens.Count, alive, 0, 0);
                    var status = _coordinator.IsRunning ? "Running" : "Paused";
                    await _broadcast.SimulationStatusChanged(status);
                }
                catch
                {
                }
                await Task.Delay(1000, stoppingToken);
            }
        }, stoppingToken);

        _logger.LogInformation("SignalR broadcast service started");
        await Task.CompletedTask;
    }
}
