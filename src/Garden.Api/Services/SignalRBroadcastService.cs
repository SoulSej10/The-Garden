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

        _eventBus.Subscribe<DroughtStartedEvent>(e =>
        {
            _ = _broadcast.SignificantEvent(
                $"Drought Near ({e.TileX}, {e.TileY})",
                "A prolonged dry spell has taken hold.",
                "Disaster", "High");
        });

        _eventBus.Subscribe<WildfireStartedEvent>(e =>
        {
            _ = _broadcast.SignificantEvent(
                $"Wildfire Near ({e.TileX}, {e.TileY})",
                $"Burned {e.TilesBurned} tile(s) amid drought conditions.",
                "Disaster", "High");
        });

        _eventBus.Subscribe<FloodStartedEvent>(e =>
        {
            _ = _broadcast.SignificantEvent(
                $"Flooding Near ({e.TileX}, {e.TileY})",
                e.NearestSettlementName is not null ? $"Rivers overtopped their banks near {e.NearestSettlementName}." : "Rivers overtopped their banks.",
                "Disaster", "High");
        });

        _eventBus.Subscribe<VolcanicEruptionEvent>(e =>
        {
            _ = _broadcast.SignificantEvent(
                e.FormedNewLand ? $"New Island Formed at ({e.TileX}, {e.TileY})" : $"Volcanic Eruption at ({e.TileX}, {e.TileY})",
                e.FormedNewLand ? "An eruption built new land above the waterline." : "An eruption reshaped the surrounding terrain.",
                "Disaster", "High");
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
