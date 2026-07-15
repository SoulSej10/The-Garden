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

        // Audit finding 05: this weekly pass can cross the spread/convert
        // threshold on many tiles in the same call, and each one used to
        // publish its own ForestExpandedEvent immediately - live sampling
        // showed ForestExpanded as 65% of the entire history archive,
        // drowning out births, deaths, and everything else. Accumulate the
        // whole week's changes and publish one summary event instead, the
        // same "aggregate, don't archive per-occurrence" treatment already
        // applied to FarmHarvested/TradeCompleted in SignificanceEvaluator.
        var expandedCount = 0;
        var declinedCount = 0;
        var expandedLastX = 0;
        var expandedLastY = 0;
        var declinedLastX = 0;
        var declinedLastY = 0;

        foreach (var tile in _worldState.Map.GetAllTiles())
        {
            ProcessVegetationGrowth(tile, growthModifier, tick,
                ref expandedCount, ref expandedLastX, ref expandedLastY,
                ref declinedCount, ref declinedLastX, ref declinedLastY);
        }

        if (expandedCount > 0)
        {
            _eventBus.Publish(new ForestExpandedEvent
            {
                Tick = tick,
                TileX = expandedLastX,
                TileY = expandedLastY,
                AreaExpanded = expandedCount
            });
        }

        if (declinedCount > 0)
        {
            _eventBus.Publish(new ForestDeclinedEvent
            {
                Tick = tick,
                TileX = declinedLastX,
                TileY = declinedLastY,
                AreaLost = declinedCount
            });
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void ProcessVegetationGrowth(
        World.Entities.WorldTile tile, double growthModifier, long tick,
        ref int expandedCount, ref int expandedLastX, ref int expandedLastY,
        ref int declinedCount, ref int declinedLastX, ref int declinedLastY)
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
                var spread = SpreadForest(tile, tick);
                if (spread != null)
                {
                    expandedCount++;
                    expandedLastX = spread.Value.X;
                    expandedLastY = spread.Value.Y;
                }
            }
        }

        if (tile.Terrain is TerrainType.Plains or TerrainType.Grassland && growthPotential > 0.6)
        {
            if (new System.Random((int)(tick + tile.X * 1000 + tile.Y)).NextDouble() < 0.005 * growthPotential)
            {
                tile.Terrain = TerrainType.Forest;
                expandedCount++;
                expandedLastX = tile.X;
                expandedLastY = tile.Y;
            }
        }

        if (tile.Terrain == TerrainType.Forest && tile.Moisture < 0.15 && tile.Temperature > 30)
        {
            tile.Terrain = TerrainType.Grassland;
            declinedCount++;
            declinedLastX = tile.X;
            declinedLastY = tile.Y;
        }
    }

    private (int X, int Y)? SpreadForest(World.Entities.WorldTile sourceTile, long tick)
    {
        var neighbors = _worldState.Map.GetNeighbors(sourceTile.X, sourceTile.Y)
            .Where(n => n.Terrain is TerrainType.Plains or TerrainType.Grassland && n.Moisture > 0.3)
            .ToList();

        if (neighbors.Count == 0) return null;

        var target = neighbors[new System.Random((int)tick).Next(neighbors.Count)];
        target.Terrain = TerrainType.Forest;

        return (target.X, target.Y);
    }
}
