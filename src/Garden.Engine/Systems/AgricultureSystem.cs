using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class AgricultureSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AgricultureSystem> _logger;
    private long _nextExecutionTick;

    public string Name => "AgricultureSystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    public AgricultureSystem(WorldState worldState, IEventBus eventBus, ILogger<AgricultureSystem> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var season = _worldState.CurrentTime.Season;

        foreach (var settlement in _worldState.Settlements)
        {
            var farms = settlement.Buildings
                .Where(b => b.BuildingType == BuildingTypes.Farm && b.Status == BuildingStatus.Completed)
                .ToList();

            foreach (var farm in farms)
            {
                ProcessFarm(farm, settlement, season, tick);
            }
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void ProcessFarm(Building farm, Settlement settlement, Core.Time.Season season, long tick)
    {
        var growthModifier = season switch
        {
            Core.Time.Season.Spring => 1.5,
            Core.Time.Season.Summer => 1.2,
            Core.Time.Season.Autumn => 0.8,
            Core.Time.Season.Winter => 0.1,
            _ => 1.0
        };

        var plantedCrops = farm.Storage.GetQuantity("Seeds");
        if (plantedCrops <= 0) return;

        var tile = _worldState.Map.GetTile(farm.TileX, farm.TileY);
        if (tile.Moisture < 0.2)
        {
            plantedCrops *= 0.5;
        }

        // RFC-011 (specification/RFC/RFC-011-decomposers-soil-health.md):
        // SoilHealth defaults to 100, making this a no-op until repeated
        // harvests actually deplete it - DecomposerSystem owns all updates
        // to this field.
        var yield = plantedCrops * growthModifier * 2.0 * (settlement.SoilHealth / 100.0);
        if (yield > 0.5)
        {
            settlement.Storage.Add("Food", yield);
            farm.Storage.Remove("Seeds", plantedCrops * 0.8);

            _eventBus.Publish(new FarmHarvestedEvent
            {
                Tick = tick,
                SettlementId = settlement.Id,
                SettlementName = settlement.Name,
                BuildingId = farm.Id,
                CropType = "Grain",
                Yield = Math.Round(yield, 1)
            });
        }
    }
}
