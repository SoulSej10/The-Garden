using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Core.World;
using Garden.World.Collections;
using Garden.World.Entities;
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

        // Rebalancing audit finding 2: spread/conversion previously had no
        // upper bound and no density feedback at all - any eligible tile
        // rolled the same flat chance regardless of how much of the map was
        // already forested, so coverage only ever climbed. A global
        // eligible-land forest fraction (Plains/Grassland/Forest only - the
        // only terrain types this system touches) dampens both
        // probabilities as coverage approaches ForestCarryingCapacity,
        // giving forest cover a real equilibrium instead of a monotonic
        // ceiling of "every eligible tile eventually converts."
        const double ForestCarryingCapacity = 0.5;
        var allTiles = _worldState.Map.GetAllTiles().ToList();
        var eligibleTiles = allTiles.Count(t => t.Terrain is TerrainType.Plains or TerrainType.Grassland or TerrainType.Forest);
        var forestTiles = allTiles.Count(t => t.Terrain == TerrainType.Forest);
        var forestFraction = eligibleTiles > 0 ? forestTiles / (double)eligibleTiles : 0.0;
        var densityDamping = Math.Max(0.0, 1.0 - forestFraction / ForestCarryingCapacity);

        foreach (var tile in allTiles)
        {
            ProcessVegetationGrowth(tile, growthModifier, densityDamping, tick,
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

    // Rebalancing audit finding 2: harvesting used to only ever deplete a
    // tile's Trees resource deposit (which regenerates independently via
    // ResourceSystem) with zero effect on the tile's Terrain - a fully
    // logged-out forest tile stayed TerrainType.Forest forever and could
    // still spread to its neighbors. Sustained depletion now gives a real
    // chance of the tile reverting, so wood-gathering has an actual
    // ecological consequence instead of being cosmetic to the terrain layer.
    private const int SustainedDepletionWeeksForReversion = 4;
    private const double DepletedReversionChance = 0.1;

    private void ProcessVegetationGrowth(
        World.Entities.WorldTile tile, double growthModifier, double densityDamping, long tick,
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
            if (new System.Random((int)(tick + tile.X * 1000 + tile.Y)).NextDouble() < 0.01 * densityDamping)
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
            if (new System.Random((int)(tick + tile.X * 1000 + tile.Y)).NextDouble() < 0.005 * growthPotential * densityDamping)
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
            tile.HarvestDepletedWeeks = 0;
            declinedCount++;
            declinedLastX = tile.X;
            declinedLastY = tile.Y;
            return;
        }

        if (tile.Terrain == TerrainType.Forest)
        {
            var treeDeposit = tile.Resources.FirstOrDefault(r => r.Type == ResourceType.Trees);
            var depleted = treeDeposit != null && treeDeposit.MaxCapacity > 0
                && treeDeposit.Quantity / treeDeposit.MaxCapacity < 0.1;

            tile.HarvestDepletedWeeks = depleted ? tile.HarvestDepletedWeeks + 1 : 0;

            if (tile.HarvestDepletedWeeks >= SustainedDepletionWeeksForReversion
                && new System.Random((int)(tick + tile.X * 500 + tile.Y * 7)).NextDouble() < DepletedReversionChance)
            {
                tile.Terrain = TerrainType.Grassland;
                tile.HarvestDepletedWeeks = 0;
                declinedCount++;
                declinedLastX = tile.X;
                declinedLastY = tile.Y;
            }
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
