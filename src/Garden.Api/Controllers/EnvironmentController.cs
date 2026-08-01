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

    [HttpGet("weather/grid")]
    public IActionResult GetWeatherGrid()
    {
        if (!_worldState.IsInitialized || _worldState.WeatherCellsX == 0)
            return Ok(new { Status = "Not initialized", CellsX = 0, CellsY = 0, TileSize = 0, Cells = Array.Empty<object>() });

        var cellsX = _worldState.WeatherCellsX;
        var cellsY = _worldState.WeatherCellsY;
        var cells = new List<object>(cellsX * cellsY);
        for (var x = 0; x < cellsX; x++)
        for (var y = 0; y < cellsY; y++)
        {
            var cell = _worldState.WeatherCells[x, y];
            cells.Add(new
            {
                CellX = x,
                CellY = y,
                Condition = cell.CurrentWeather.ToString(),
                Intensity = Math.Round(cell.Intensity, 2),
                WindDirX = Math.Round(cell.WindDirX, 2),
                WindDirY = Math.Round(cell.WindDirY, 2),
                WindStrength = Math.Round(cell.WindStrength, 2)
            });
        }

        return Ok(new
        {
            CellsX = cellsX,
            CellsY = cellsY,
            TileSize = _worldState.WeatherCellTileSize,
            Cells = cells
        });
    }

    [HttpGet("climate")]
    public IActionResult GetClimate()
    {
        if (!_worldState.IsInitialized)
            return Ok(new { Status = "Not initialized" });

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

    [HttpGet("geology")]
    public IActionResult GetGeology()
    {
        if (!_worldState.IsInitialized)
            return Ok(new { Status = "Not initialized" });

        var tiles = _worldState.Map.GetAllTiles().ToList();
        var total = tiles.Count;
        if (total == 0) return Ok(new { Status = "Not initialized" });

        var terrainBreakdown = tiles.GroupBy(t => t.Terrain.ToString())
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => Math.Round(g.Count() * 100.0 / total, 2));

        var boundaryBreakdown = tiles.GroupBy(t => t.BoundaryType.ToString())
            .ToDictionary(g => g.Key, g => Math.Round(g.Count() * 100.0 / total, 2));

        var forestCount = tiles.Count(t => t.Terrain == Garden.Core.World.TerrainType.Forest);

        return Ok(new
        {
            Archetype = _worldState.Archetype.ToString(),
            TotalTiles = total,
            VolcanicTileCount = tiles.Count(t => t.IsVolcanic),
            AverageRelief = Math.Round(tiles.Average(t => t.Relief), 3),
            ForestCoverPercent = Math.Round(forestCount * 100.0 / total, 2),
            TerrainBreakdown = terrainBreakdown,
            BoundaryBreakdown = boundaryBreakdown
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
