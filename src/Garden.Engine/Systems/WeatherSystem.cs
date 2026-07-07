using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.Core.World;
using Garden.Engine.Events;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

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
        var weather = _worldState.Weather;
        var currentSeason = _worldState.CurrentTime.Season;
        var tick = _worldState.CurrentTime.Tick;

        if (weather.RemainingDuration > 0)
        {
            weather.RemainingDuration--;
            _nextExecutionTick = tick + IntervalTicks;
            return;
        }

        var previousWeather = weather.CurrentWeather;
        weather.CurrentWeather = DetermineWeather(currentSeason);
        weather.RemainingDuration = new System.Random((int)tick).Next(3, 12);
        weather.Intensity = new System.Random((int)tick).NextDouble() * 0.5 + 0.5;
        weather.TemperatureModifier = GetTemperatureModifier(weather.CurrentWeather);
        weather.WindStrength = GetWindStrength(weather.CurrentWeather);

        if (weather.CurrentWeather != previousWeather)
        {
            _logger.LogInformation("Weather changed from {Previous} to {Current}",
                previousWeather, weather.CurrentWeather);
        }

        ApplyWeatherEffects(weather, currentSeason);
        _nextExecutionTick = tick + IntervalTicks;
    }

    private static WeatherState DetermineWeather(Season season)
    {
        var rng = new System.Random();
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

    private static double GetTemperatureModifier(WeatherState weather)
    {
        return weather switch
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
    }

    private static double GetWindStrength(WeatherState weather)
    {
        return weather switch
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
    }

    private void ApplyWeatherEffects(WeatherStateData weather, Season season)
    {
        var tempMod = weather.TemperatureModifier + (season switch
        {
            Season.Spring => 0,
            Season.Summer => 5,
            Season.Autumn => -2,
            Season.Winter => -7,
            _ => 0
        });

        weather.HumidityModifier = weather.CurrentWeather switch
        {
            WeatherState.Rain => 0.3,
            WeatherState.HeavyRain => 0.5,
            WeatherState.Storm => 0.6,
            WeatherState.Fog => 0.2,
            WeatherState.Snow => 0.1,
            _ => -0.1
        };

        if (weather.CurrentWeather is WeatherState.Rain or WeatherState.HeavyRain)
        {
            foreach (var tile in _worldState.Map.GetAllTiles())
            {
                tile.Moisture = Math.Min(1.0, tile.Moisture + 0.01 * weather.Intensity);
            }
        }
    }
}
