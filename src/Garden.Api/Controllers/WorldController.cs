using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class WorldController : ControllerBase
{
    private readonly WorldState _worldState;

    public WorldController(WorldState worldState)
    {
        _worldState = worldState;
    }

    [HttpGet]
    public IActionResult GetWorld()
    {
        if (!_worldState.IsInitialized)
            return Ok(new { Status = "Not initialized" });

        return Ok(new
        {
            Width = _worldState.Map.Width,
            Height = _worldState.Map.Height,
            TotalTiles = _worldState.Map.Width * _worldState.Map.Height,
            TileCounts = Enum.GetValues<Core.World.TerrainType>()
                .ToDictionary(t => t.ToString(), t =>
                    _worldState.Map.GetAllTiles().Count(tile => tile.Terrain == t)),
            ClimateZones = _worldState.ClimateZones.Select(c => new
            {
                c.Zone,
                c.BaseTemperature,
                c.AverageRainfall
            }),
            IsInitialized = _worldState.IsInitialized
        });
    }

    [HttpGet("map")]
    public IActionResult GetMap([FromQuery] int? regionX, [FromQuery] int? regionY,
        [FromQuery] int? width, [FromQuery] int? height)
    {
        if (!_worldState.IsInitialized)
            return Ok(new { Status = "Not initialized" });

        var map = _worldState.Map;
        var startX = Math.Max(0, regionX ?? 0);
        var startY = Math.Max(0, regionY ?? 0);
        var w = Math.Min(width ?? map.Width, map.Width - startX);
        var h = Math.Min(height ?? map.Height, map.Height - startY);

        var tiles = new List<object>();
        for (var x = startX; x < startX + w; x++)
        for (var y = startY; y < startY + h; y++)
        {
            var tile = map.GetTile(x, y);
            tiles.Add(new
            {
                tile.X,
                tile.Y,
                Terrain = tile.Terrain.ToString(),
                Biome = tile.Biome.ToString(),
                Elevation = Math.Round(tile.Elevation, 3),
                tile.IsRiver,
                tile.IsLake
            });
        }

        return Ok(new
        {
            OffsetX = startX,
            OffsetY = startY,
            Width = w,
            Height = h,
            TotalReturned = tiles.Count,
            Tiles = tiles
        });
    }

    [HttpGet("tiles/{x}/{y}")]
    public IActionResult GetTile(int x, int y)
    {
        if (!_worldState.IsInitialized)
            return Ok(new { Status = "Not initialized" });

        if (x < 0 || x >= _worldState.Map.Width || y < 0 || y >= _worldState.Map.Height)
            return NotFound(new { Error = $"Tile ({x},{y}) out of bounds" });

        var tile = _worldState.Map.GetTile(x, y);
        return Ok(new
        {
            tile.X,
            tile.Y,
            Terrain = tile.Terrain.ToString(),
            Biome = tile.Biome.ToString(),
            Climate = tile.Climate.ToString(),
            Elevation = Math.Round(tile.Elevation, 3),
            Moisture = Math.Round(tile.Moisture, 3),
            Temperature = Math.Round(tile.Temperature, 1),
            tile.IsRiver,
            tile.IsLake,
            Resources = tile.Resources.Select(r => new
            {
                Type = r.Type.ToString(),
                Quantity = Math.Round(r.Quantity, 1),
                MaxCapacity = r.MaxCapacity,
                RegenerationRate = r.RegenerationRate
            }),
            Occupancy = tile.Occupancy.IsOccupied ? new
            {
                tile.Occupancy.OccupiedBy,
                tile.Occupancy.StructureType
            } : null
        });
    }
}
