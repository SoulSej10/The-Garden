using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.Core.World;
using Garden.Engine.Events;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

/// <summary>
/// Spatial weather: a coarse grid of independently-rolling cells instead of
/// one global scalar (see WeatherCell). Each cell biases toward inheriting
/// a stormy condition from its upwind neighbor, so storm fronts visibly
/// travel across the map over several ticks rather than the whole world
/// changing state in lockstep. WorldState.Weather is kept as a
/// backward-compatible dominant-condition aggregate for the handful of
/// existing consumers (EnvironmentController, HydrologySystem) that only
/// ever needed a single global reading.
/// </summary>
public class WeatherSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<WeatherSystem> _logger;
    private long _nextExecutionTick;

    public string Name => "WeatherSystem";
    public long IntervalTicks => 1;
    public long NextExecutionTick => _nextExecutionTick;

    public WeatherSystem(WorldState worldState, IEventBus eventBus, ILogger<WeatherSystem> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var season = _worldState.CurrentTime.Season;

        EnsureGrid();
        if (_worldState.WeatherCellsX == 0)
        {
            _nextExecutionTick = tick + IntervalTicks;
            return;
        }

        var cellsX = _worldState.WeatherCellsX;
        var cellsY = _worldState.WeatherCellsY;
        var cells = _worldState.WeatherCells;

        for (var cx = 0; cx < cellsX; cx++)
        for (var cy = 0; cy < cellsY; cy++)
        {
            var cell = cells[cx, cy];
            if (cell.RemainingDuration > 0)
            {
                cell.RemainingDuration--;
                continue;
            }

            // Deterministic-but-decorrelated per-cell roll: same tick, different
            // cell -> different roll, unlike the old single `new Random(tick)`.
            var rng = new System.Random(unchecked((int)(tick * 1000003 + cx * 977 + cy * 31)));

            WeatherState? inherited = null;
            var upwindX = cx - (int)Math.Sign(cell.WindDirX);
            var upwindY = cy - (int)Math.Sign(cell.WindDirY);
            if (upwindX >= 0 && upwindX < cellsX && upwindY >= 0 && upwindY < cellsY)
            {
                var upwind = cells[upwindX, upwindY];
                if (IsStormy(upwind.CurrentWeather) && rng.NextDouble() < 0.45)
                    inherited = upwind.CurrentWeather;
            }

            var previous = cell.CurrentWeather;
            cell.CurrentWeather = inherited ?? DetermineWeather(season, rng);
            cell.RemainingDuration = rng.Next(3, 12);
            cell.Intensity = rng.NextDouble() * 0.5 + 0.5;
            cell.TemperatureModifier = GetTemperatureModifier(cell.CurrentWeather);
            cell.WindStrength = GetWindStrength(cell.CurrentWeather);
            cell.HumidityModifier = GetHumidityModifier(cell.CurrentWeather);
            // A gentle, whole-grid prevailing drift (fronts move west-to-east)
            // with a little per-cell jitter so cells aren't perfectly aligned.
            cell.WindDirX = 1;
            cell.WindDirY = rng.NextDouble() * 0.4 - 0.2;

            if (cell.CurrentWeather != previous)
            {
                _logger.LogDebug("Weather cell ({Cx},{Cy}) changed {Previous} -> {Current}", cx, cy, previous, cell.CurrentWeather);
            }
        }

        ApplyWeatherEffects();
        UpdateGlobalAggregate();
        _nextExecutionTick = tick + IntervalTicks;
    }

    private static bool IsStormy(WeatherState state) => state is WeatherState.Rain or WeatherState.HeavyRain or WeatherState.Storm;

    private void EnsureGrid()
    {
        if (_worldState.WeatherCellsX > 0) return;
        var map = _worldState.Map;
        if (map.Width == 0 || map.Height == 0) return;

        var tileSize = _worldState.WeatherCellTileSize;
        var cellsX = Math.Max(1, (int)Math.Ceiling(map.Width / (double)tileSize));
        var cellsY = Math.Max(1, (int)Math.Ceiling(map.Height / (double)tileSize));

        var cells = new WeatherCell[cellsX, cellsY];
        for (var x = 0; x < cellsX; x++)
        for (var y = 0; y < cellsY; y++)
            cells[x, y] = new WeatherCell();

        _worldState.WeatherCellsX = cellsX;
        _worldState.WeatherCellsY = cellsY;
        _worldState.WeatherCells = cells;
    }

    private static WeatherState DetermineWeather(Season season, System.Random rng)
    {
        return season switch
        {
            Season.Spring => rng.NextDouble() switch
            {
                < 0.35 => WeatherState.Clear,
                < 0.55 => WeatherState.Cloudy,
                < 0.75 => WeatherState.Rain,
                < 0.88 => WeatherState.HeavyRain,
                < 0.94 => WeatherState.Fog,
                _ => WeatherState.Storm
            },
            Season.Summer => rng.NextDouble() switch
            {
                < 0.45 => WeatherState.Clear,
                < 0.65 => WeatherState.Cloudy,
                < 0.78 => WeatherState.Rain,
                < 0.88 => WeatherState.HeavyRain,
                < 0.94 => WeatherState.Fog,
                _ => WeatherState.Storm
            },
            Season.Autumn => rng.NextDouble() switch
            {
                < 0.25 => WeatherState.Clear,
                < 0.45 => WeatherState.Cloudy,
                < 0.60 => WeatherState.Rain,
                < 0.75 => WeatherState.HeavyRain,
                < 0.85 => WeatherState.Fog,
                < 0.93 => WeatherState.Storm,
                _ => WeatherState.Snow
            },
            Season.Winter => rng.NextDouble() switch
            {
                < 0.20 => WeatherState.Clear,
                < 0.35 => WeatherState.Cloudy,
                < 0.55 => WeatherState.Snow,
                < 0.72 => WeatherState.Rain,
                < 0.85 => WeatherState.HeavyRain,
                < 0.93 => WeatherState.Fog,
                _ => WeatherState.Storm
            },
            _ => WeatherState.Clear
        };
    }

    private static double GetTemperatureModifier(WeatherState weather) => weather switch
    {
        WeatherState.Clear => 2.0,
        WeatherState.Cloudy => 0.0,
        WeatherState.Rain => -2.0,
        WeatherState.HeavyRain => -4.0,
        WeatherState.Storm => -5.0,
        WeatherState.Fog => -1.0,
        WeatherState.Snow => -8.0,
        _ => 0.0
    };

    private static double GetWindStrength(WeatherState weather) => weather switch
    {
        WeatherState.Clear => 0.0,
        WeatherState.Cloudy => 0.2,
        WeatherState.Rain => 0.3,
        WeatherState.HeavyRain => 0.5,
        WeatherState.Storm => 0.8,
        WeatherState.Fog => 0.1,
        WeatherState.Snow => 0.4,
        _ => 0.0
    };

    private static double GetHumidityModifier(WeatherState weather) => weather switch
    {
        WeatherState.Rain => 0.3,
        WeatherState.HeavyRain => 0.5,
        WeatherState.Storm => 0.6,
        WeatherState.Fog => 0.2,
        WeatherState.Snow => 0.1,
        _ => -0.1
    };

    /// <summary>
    /// Bounded to each rain-bearing cell's own tile region, not a full-grid
    /// pass - actually cheaper overall than the old single-scalar version,
    /// which touched every tile on the map whenever it rained ANYWHERE.
    /// </summary>
    private void ApplyWeatherEffects()
    {
        var map = _worldState.Map;
        var tileSize = _worldState.WeatherCellTileSize;
        var cells = _worldState.WeatherCells;

        for (var cx = 0; cx < _worldState.WeatherCellsX; cx++)
        for (var cy = 0; cy < _worldState.WeatherCellsY; cy++)
        {
            var cell = cells[cx, cy];
            if (cell.CurrentWeather is not (WeatherState.Rain or WeatherState.HeavyRain)) continue;

            var startX = cx * tileSize;
            var endX = Math.Min(map.Width, startX + tileSize);
            var startY = cy * tileSize;
            var endY = Math.Min(map.Height, startY + tileSize);

            for (var x = startX; x < endX; x++)
            for (var y = startY; y < endY; y++)
            {
                var tile = map.GetTile(x, y);
                tile.Moisture = Math.Min(1.0, tile.Moisture + 0.01 * cell.Intensity);
            }
        }
    }

    private void UpdateGlobalAggregate()
    {
        var cells = _worldState.WeatherCells;
        var cellsX = _worldState.WeatherCellsX;
        var cellsY = _worldState.WeatherCellsY;
        if (cellsX == 0 || cellsY == 0) return;

        var counts = new Dictionary<WeatherState, int>();
        double sumTemp = 0, sumWind = 0, sumHumidity = 0;
        var n = 0;

        for (var x = 0; x < cellsX; x++)
        for (var y = 0; y < cellsY; y++)
        {
            var cell = cells[x, y];
            counts[cell.CurrentWeather] = counts.GetValueOrDefault(cell.CurrentWeather) + 1;
            sumTemp += cell.TemperatureModifier;
            sumWind += cell.WindStrength;
            sumHumidity += cell.HumidityModifier;
            n++;
        }

        if (n == 0) return;
        var dominant = counts.OrderByDescending(kv => kv.Value).First().Key;
        var dominantCells = new List<WeatherCell>();
        for (var x = 0; x < cellsX; x++)
        for (var y = 0; y < cellsY; y++)
            if (cells[x, y].CurrentWeather == dominant) dominantCells.Add(cells[x, y]);

        var agg = _worldState.Weather;
        agg.CurrentWeather = dominant;
        agg.Intensity = dominantCells.Count > 0 ? dominantCells.Average(c => c.Intensity) : 0;
        agg.RemainingDuration = dominantCells.Count > 0 ? dominantCells.Max(c => c.RemainingDuration) : 0;
        agg.TemperatureModifier = sumTemp / n;
        agg.WindStrength = sumWind / n;
        agg.HumidityModifier = sumHumidity / n;
    }
}
