using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Core.World;
using Garden.World.Collections;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

/// <summary>
/// Four natural-disaster triggers layered on top of the spatial weather
/// grid (WeatherSystem) and the geological generator's hydrology/volcanism
/// data (WorldGenerator). Each resolves within the tick it's detected on
/// rather than simulating multi-day spread/recession, fire physics, or
/// vortex paths - a deliberate simplification (see the plan's "explicitly
/// deferred" list for hurricanes/tornadoes/monsoons, which need real path
/// simulation this does not attempt). Drought is the one multi-day-tracked
/// exception (via WeatherCell.DryStreak) since Started/Ended is meaningless
/// as an instant event.
/// </summary>
public class NaturalEventsSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<NaturalEventsSystem> _logger;
    private long _nextExecutionTick;
    private List<(int X, int Y)>? _volcanicTiles;

    public string Name => "NaturalEventsSystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    public NaturalEventsSystem(WorldState worldState, IEventBus eventBus, ILogger<NaturalEventsSystem> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        if (_worldState.WeatherCellsX > 0)
        {
            CheckDroughtAndWildfire(tick);
            CheckFlood(tick);
        }
        CheckVolcanicEruption(tick);

        _nextExecutionTick = tick + IntervalTicks;
    }

    private static readonly System.Random Rng = new();

    private void CheckDroughtAndWildfire(long tick)
    {
        var map = _worldState.Map;
        var tileSize = _worldState.WeatherCellTileSize;

        for (var cx = 0; cx < _worldState.WeatherCellsX; cx++)
        for (var cy = 0; cy < _worldState.WeatherCellsY; cy++)
        {
            var cell = _worldState.WeatherCells[cx, cy];
            var isDry = cell.CurrentWeather is Garden.Core.World.WeatherState.Clear or Garden.Core.World.WeatherState.Cloudy;
            cell.DryStreak = isDry ? cell.DryStreak + 1 : 0;

            if (cell.DryStreak >= 5 && !cell.IsDroughtActive)
            {
                cell.IsDroughtActive = true;
                var (cxTile, cyTile) = CellCenter(cx, cy, tileSize);
                _eventBus.Publish(new DroughtStartedEvent { TileX = cxTile, TileY = cyTile, Tick = tick, Severity = "High", SeverityLevel = cell.DryStreak });
            }
            else if (cell.DryStreak == 0 && cell.IsDroughtActive)
            {
                cell.IsDroughtActive = false;
                var (cxTile, cyTile) = CellCenter(cx, cy, tileSize);
                _eventBus.Publish(new DroughtEndedEvent { TileX = cxTile, TileY = cyTile, Tick = tick, Severity = "Normal" });
            }

            if (!cell.IsDroughtActive) continue;

            var startX = cx * tileSize;
            var endX = Math.Min(map.Width, startX + tileSize);
            var startY = cy * tileSize;
            var endY = Math.Min(map.Height, startY + tileSize);

            // One ignition roll per cell per check, not per-tile - keeps
            // wildfires rare and localized instead of a flammable-tile lottery.
            if (Rng.NextDouble() > 0.12) continue;

            var candidates = new List<(int X, int Y)>();
            for (var x = startX; x < endX; x++)
            for (var y = startY; y < endY; y++)
            {
                var t = map.GetTile(x, y);
                if (t.Terrain is TerrainType.Forest or TerrainType.Grassland) candidates.Add((x, y));
            }
            if (candidates.Count == 0) continue;

            var origin = candidates[Rng.Next(candidates.Count)];
            var burned = BurnFrom(map, origin.X, origin.Y);
            if (burned == 0) continue;

            _logger.LogInformation("Wildfire near ({X},{Y}) burned {Count} tiles", origin.X, origin.Y, burned);
            _eventBus.Publish(new WildfireStartedEvent { TileX = origin.X, TileY = origin.Y, Tick = tick, Severity = "High", TilesBurned = burned });
        }
    }

    private static (int X, int Y) CellCenter(int cx, int cy, int tileSize) => (cx * tileSize + tileSize / 2, cy * tileSize + tileSize / 2);

    private int BurnFrom(WorldMap map, int originX, int originY)
    {
        var burned = 0;
        var frontier = new Queue<(int X, int Y, int Depth)>();
        var visited = new HashSet<(int, int)> { (originX, originY) };
        frontier.Enqueue((originX, originY, 0));
        const int maxRadius = 4;

        while (frontier.Count > 0)
        {
            var (x, y, depth) = frontier.Dequeue();
            var tile = map.GetTile(x, y);
            if (tile.Terrain is TerrainType.Forest or TerrainType.Grassland)
            {
                tile.Terrain = TerrainType.Plains;
                tile.Moisture = Math.Max(0, tile.Moisture - 0.3);
                burned++;
            }
            if (depth >= maxRadius) continue;

            foreach (var n in map.GetNeighbors(x, y))
            {
                var key = (n.X, n.Y);
                if (visited.Contains(key)) continue;
                if (n.Terrain is not (TerrainType.Forest or TerrainType.Grassland)) continue;
                // Spread probability decays with distance from the ignition point.
                if (Rng.NextDouble() > 0.55 - depth * 0.1) continue;
                visited.Add(key);
                frontier.Enqueue((n.X, n.Y, depth + 1));
            }
        }

        return burned;
    }

    private void CheckFlood(long tick)
    {
        var map = _worldState.Map;
        var tileSize = _worldState.WeatherCellTileSize;

        for (var cx = 0; cx < _worldState.WeatherCellsX; cx++)
        for (var cy = 0; cy < _worldState.WeatherCellsY; cy++)
        {
            var cell = _worldState.WeatherCells[cx, cy];
            if (cell.CurrentWeather is not (Garden.Core.World.WeatherState.HeavyRain or Garden.Core.World.WeatherState.Storm)) continue;
            if (Rng.NextDouble() > 0.15) continue;

            var startX = cx * tileSize;
            var endX = Math.Min(map.Width, startX + tileSize);
            var startY = cy * tileSize;
            var endY = Math.Min(map.Height, startY + tileSize);

            var majorRivers = new List<(int X, int Y)>();
            for (var x = startX; x < endX; x++)
            for (var y = startY; y < endY; y++)
            {
                var t = map.GetTile(x, y);
                if (t.IsRiver && t.FlowAccumulation > 0.5) majorRivers.Add((x, y));
            }
            if (majorRivers.Count == 0) continue;

            var (rx, ry) = majorRivers[Rng.Next(majorRivers.Count)];
            var floodedTiles = 0;
            foreach (var n in map.GetNeighbors(rx, ry, range: 2))
            {
                if (n.IsRiver || n.IsLake || n.Terrain == TerrainType.Ocean) continue;
                if (n.Relief > 0.35) continue; // steep ground doesn't pool
                n.Moisture = 1.0;
                floodedTiles++;
            }
            if (floodedTiles == 0) continue;

            var nearestSettlement = _worldState.Settlements
                .OrderBy(s => Math.Abs(s.TileX - rx) + Math.Abs(s.TileY - ry))
                .FirstOrDefault(s => Math.Abs(s.TileX - rx) + Math.Abs(s.TileY - ry) < 15);

            _logger.LogInformation("Flood near ({X},{Y}) affected {Count} tiles", rx, ry, floodedTiles);
            _eventBus.Publish(new FloodStartedEvent
            {
                TileX = rx, TileY = ry, Tick = tick, Severity = "High",
                NearestSettlementName = nearestSettlement?.Name
            });
        }
    }

    private void CheckVolcanicEruption(long tick)
    {
        var map = _worldState.Map;
        _volcanicTiles ??= map.GetAllTiles().Where(t => t.IsVolcanic).Select(t => (t.X, t.Y)).ToList();
        if (_volcanicTiles.Count == 0) return;

        foreach (var (x, y) in _volcanicTiles)
        {
            // ~once per few in-game decades per volcanic tile at daily cadence -
            // volcanism should read as a rare, memorable event, not ambient noise.
            if (Rng.NextDouble() > 1.0 / 3650) continue;

            var tile = map.GetTile(x, y);
            var wasOcean = tile.Terrain == TerrainType.Ocean;
            tile.Elevation = Math.Clamp(tile.Elevation + 0.08, 0, 1);
            foreach (var n in map.GetNeighbors(x, y))
                n.Elevation = Math.Clamp(n.Elevation + 0.03, 0, 1);

            var formedNewLand = wasOcean && tile.Elevation >= 0.46;
            if (formedNewLand) tile.Terrain = TerrainType.VolcanicIsland;

            _logger.LogInformation("Volcanic eruption at ({X},{Y}), formedNewLand={FormedNewLand}", x, y, formedNewLand);
            _eventBus.Publish(new VolcanicEruptionEvent { TileX = x, TileY = y, Tick = tick, Severity = "High", FormedNewLand = formedNewLand });
        }
    }
}
