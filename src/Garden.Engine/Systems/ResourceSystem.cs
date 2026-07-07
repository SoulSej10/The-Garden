using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class ResourceSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ResourceSystem> _logger;
    private long _nextExecutionTick;

    public string Name => "ResourceSystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    public ResourceSystem(WorldState worldState, IEventBus eventBus, ILogger<ResourceSystem> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var currentSeason = _worldState.CurrentTime.Season;

        var seasonModifier = currentSeason switch
        {
            Core.Time.Season.Spring => 1.5,
            Core.Time.Season.Summer => 1.2,
            Core.Time.Season.Autumn => 1.0,
            Core.Time.Season.Winter => 0.5,
            _ => 1.0
        };

        foreach (var tile in _worldState.Map.GetAllTiles())
        {
            foreach (var resource in tile.Resources)
            {
                if (resource.Quantity >= resource.MaxCapacity) continue;
                if (resource.RegenerationRate <= 0) continue;

                var regen = resource.RegenerationRate * seasonModifier;
                if (resource.Type == Core.World.ResourceType.Trees && tile.Moisture > 0.3)
                    regen *= tile.Moisture;
                if (resource.Type == Core.World.ResourceType.WildPlants && tile.Moisture > 0.2)
                    regen *= tile.Moisture;

                var oldQuantity = resource.Quantity;
                resource.Quantity = Math.Min(resource.MaxCapacity, resource.Quantity + regen);

                if (resource.Quantity - oldQuantity > 1)
                {
                    _eventBus.Publish(new ResourceRegeneratedEvent
                    {
                        Tick = tick,
                        TileX = tile.X,
                        TileY = tile.Y,
                        ResourceName = resource.Type.ToString(),
                        Amount = (int)(resource.Quantity - oldQuantity)
                    });
                }
            }
        }

        _nextExecutionTick = tick + IntervalTicks;
    }
}
