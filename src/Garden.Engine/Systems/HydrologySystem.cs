using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class HydrologySystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly ILogger<HydrologySystem> _logger;
    private long _nextExecutionTick;

    public string Name => "HydrologySystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    public HydrologySystem(WorldState worldState, ILogger<HydrologySystem> logger)
    {
        _worldState = worldState;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        ApplyEvaporation();
        ProcessRiverFlow();
        UpdateGroundMoisture();

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void ApplyEvaporation()
    {
        var weather = _worldState.Weather;
        var evaporationBase = weather.HumidityModifier > 0 ? 0.001 : 0.005;

        foreach (var tile in _worldState.Map.GetAllTiles())
        {
            if (tile.IsLake)
            {
                tile.Moisture = Math.Max(0.8, tile.Moisture - evaporationBase);
            }
            else if (!tile.IsRiver && tile.Terrain is not Core.World.TerrainType.Ocean)
            {
                tile.Moisture = Math.Max(0.0, tile.Moisture - evaporationBase * 0.5);
            }
        }
    }

    private void ProcessRiverFlow()
    {
        foreach (var tile in _worldState.Map.GetAllTiles().Where(t => t.IsRiver))
        {
            var neighbors = _worldState.Map.GetNeighbors(tile.X, tile.Y).ToList();
            foreach (var neighbor in neighbors.Where(n => !n.IsRiver && n.Terrain is not Core.World.TerrainType.Ocean))
            {
                neighbor.Moisture = Math.Min(1.0, neighbor.Moisture + 0.005);
            }
        }
    }

    private void UpdateGroundMoisture()
    {
        var weather = _worldState.Weather;
        var rainfallBonus = weather.CurrentWeather switch
        {
            Core.World.WeatherState.Rain => 0.02,
            Core.World.WeatherState.HeavyRain => 0.04,
            Core.World.WeatherState.Storm => 0.05,
            _ => 0.0
        };

        if (rainfallBonus > 0)
        {
            foreach (var tile in _worldState.Map.GetAllTiles())
            {
                tile.Moisture = Math.Min(1.0, tile.Moisture + rainfallBonus * weather.Intensity);
            }
        }
    }
}
