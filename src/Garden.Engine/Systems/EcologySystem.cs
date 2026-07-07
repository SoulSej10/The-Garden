using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Core.World;
using Garden.World.Collections;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class EcologySystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<EcologySystem> _logger;
    private long _nextExecutionTick;

    public string Name => "EcologySystem";
    public long IntervalTicks => 24 * 7;
    public long NextExecutionTick => _nextExecutionTick;

    public EcologySystem(WorldState worldState, IEventBus eventBus, ILogger<EcologySystem> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var currentSeason = _worldState.CurrentTime.Season;

        var growthModifier = currentSeason switch
        {
            Core.Time.Season.Spring => 2.0,
            Core.Time.Season.Summer => 1.5,
            Core.Time.Season.Autumn => 0.8,
            Core.Time.Season.Winter => 0.1,
            _ => 1.0
        };

        foreach (var tile in _worldState.Map.GetAllTiles())
        {
            ProcessVegetationGrowth(tile, growthModifier, tick);
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void ProcessVegetationGrowth(World.Entities.WorldTile tile, double growthModifier, long tick)
    {
        if (tile.Terrain is TerrainType.Ocean or TerrainType.River or TerrainType.Lake or TerrainType.Swamp or TerrainType.Mountains)
            return;

        if (tile.Moisture < 0.2 || tile.Temperature < 0)
            return;

        var growthPotential = tile.Moisture * (tile.Temperature / 40.0) * growthModifier;

        if (tile.Terrain == TerrainType.Forest && growthPotential > 0.5)
        {
            if (new System.Random((int)(tick + tile.X * 1000 + tile.Y)).NextDouble() < 0.01)
            {
                SpreadForest(tile, tick);
            }
        }

        if (tile.Terrain is TerrainType.Plains or TerrainType.Grassland && growthPotential > 0.6)
        {
            if (new System.Random((int)(tick + tile.X * 1000 + tile.Y)).NextDouble() < 0.005 * growthPotential)
            {
                tile.Terrain = TerrainType.Forest;
                _eventBus.Publish(new ForestExpandedEvent
                {
                    Tick = tick,
                    TileX = tile.X,
                    TileY = tile.Y,
                    AreaExpanded = 1
                });
            }
        }

        if (tile.Terrain == TerrainType.Forest && tile.Moisture < 0.15 && tile.Temperature > 30)
        {
            tile.Terrain = TerrainType.Grassland;
            _eventBus.Publish(new ForestDeclinedEvent
            {
                Tick = tick,
                TileX = tile.X,
                TileY = tile.Y,
                AreaLost = 1
            });
        }
    }

    private void SpreadForest(World.Entities.WorldTile sourceTile, long tick)
    {
        var neighbors = _worldState.Map.GetNeighbors(sourceTile.X, sourceTile.Y)
            .Where(n => n.Terrain is TerrainType.Plains or TerrainType.Grassland && n.Moisture > 0.3)
            .ToList();

        if (neighbors.Count == 0) return;

        var target = neighbors[new System.Random((int)tick).Next(neighbors.Count)];
        target.Terrain = TerrainType.Forest;

        _eventBus.Publish(new ForestExpandedEvent
        {
            Tick = tick,
            TileX = target.X,
            TileY = target.Y,
            AreaExpanded = 1
        });
    }
}
