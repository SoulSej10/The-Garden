using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.World.Collections;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class SeasonSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SeasonSystem> _logger;
    private Season _lastSeason;
    private long _nextExecutionTick;

    public string Name => "SeasonSystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    public SeasonSystem(WorldState worldState, IEventBus eventBus, ILogger<SeasonSystem> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
        _lastSeason = worldState.CurrentTime.Season;
    }

    public void Execute()
    {
        var currentSeason = _worldState.CurrentTime.Season;
        var tick = _worldState.CurrentTime.Tick;

        if (currentSeason != _lastSeason)
        {
            _logger.LogInformation("Season changed from {Previous} to {Current}", _lastSeason, currentSeason);

            _eventBus.Publish(new SeasonChangedEvent
            {
                Tick = tick,
                PreviousSeason = _lastSeason,
                NewSeason = currentSeason
            });

            ApplySeasonalEffects(currentSeason);
            _lastSeason = currentSeason;
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void ApplySeasonalEffects(Season season)
    {
        foreach (var tile in _worldState.Map.GetAllTiles())
        {
            if (tile.IsRiver || tile.IsLake) continue;

            var tempAdjustment = season switch
            {
                Season.Spring => 2.0,
                Season.Summer => 5.0,
                Season.Autumn => -2.0,
                Season.Winter => -7.0,
                _ => 0.0
            };

            tile.Temperature += tempAdjustment * 0.1;

            var moistureAdjustment = season switch
            {
                Season.Spring => 0.02,
                Season.Summer => -0.01,
                Season.Autumn => 0.01,
                Season.Winter => 0.0,
                _ => 0.0
            };

            tile.Moisture = Math.Clamp(tile.Moisture + moistureAdjustment, 0.0, 1.0);
        }
    }
}
