using Garden.Core.Events;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EnvironmentController : ControllerBase
{
    private readonly WorldState _worldState;

    public EnvironmentController(WorldState worldState)
    {
        _worldState = worldState;
    }

    [HttpGet("weather")]
    public IActionResult GetWeather()
    {
        if (!_worldState.IsInitialized)
            return Ok(new { Status = "Not initialized" });

        var weather = _worldState.Weather;
        return Ok(new
        {
            Condition = weather.CurrentWeather.ToString(),
            weather.Intensity,
            RemainingDuration = weather.RemainingDuration,
            TemperatureModifier = Math.Round(weather.TemperatureModifier, 1),
            weather.WindStrength,
            HumidityModifier = Math.Round(weather.HumidityModifier, 2)
        });
    }

    [HttpGet("climate")]
    public IActionResult GetClimate()
    {
        if (!_worldState.IsInitialized)
            return Ok(new { Status = "Not initialized" });

        var zones = _worldState.ClimateZones;
        if (zones.Count == 0)
        {
            var zoneData = _worldState.Map.GetAllTiles()
                .GroupBy(t => t.Climate)
                .Select(g => new
                {
                    Zone = g.Key.ToString(),
                    TileCount = g.Count(),
                    AvgTemperature = Math.Round(g.Average(t => t.Temperature), 1),
                    AvgMoisture = Math.Round(g.Average(t => t.Moisture), 3),
                    AvgElevation = Math.Round(g.Average(t => t.Elevation), 3)
                })
                .ToList();

            return Ok(new { Zones = zoneData, Derived = true });
        }

        return Ok(new
        {
            Zones = zones.Select(z => new
            {
                z.Zone,
                z.BaseTemperature,
                z.AverageRainfall,
                z.VegetationPotential
            })
        });
    }

    [HttpGet("resources")]
    public IActionResult GetResources()
    {
        if (!_worldState.IsInitialized)
            return Ok(new { Status = "Not initialized" });

        var allResources = _worldState.Map.GetAllTiles()
            .SelectMany(t => t.Resources.Select(r => new { t.X, t.Y, r.Type, r.Quantity, r.MaxCapacity }))
            .ToList();

        var summary = allResources
            .GroupBy(r => r.Type.ToString())
            .ToDictionary(g => g.Key, g => new
            {
                Total = Math.Round(g.Sum(r => r.Quantity), 1),
                TotalCapacity = Math.Round(g.Sum(r => r.MaxCapacity), 1),
                Deposits = g.Count()
            });

        return Ok(new
        {
            Summary = summary,
            TotalResources = allResources.Count
        });
    }

    [HttpGet("events")]
    public IActionResult GetEvents([FromQuery] int limit = 50)
    {
        if (!_worldState.IsInitialized)
            return Ok(new { Status = "Not initialized" });

        var events = _worldState.EnvironmentEvents
            .OrderByDescending(e => e.Tick)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(e => new
            {
                e.Tick,
                e.EventType,
                e.Severity,
                X = e is EnvironmentalEvent env ? env.TileX : (int?)null,
                Y = e is EnvironmentalEvent env2 ? env2.TileY : (int?)null,
                Time = _worldState.CurrentTime.ToString()
            })
            .ToList();

        return Ok(new
        {
            Total = _worldState.EnvironmentEvents.Count,
            Events = events
        });
    }
}
