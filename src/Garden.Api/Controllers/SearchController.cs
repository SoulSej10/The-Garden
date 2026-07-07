using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SearchController : ControllerBase
{
    private readonly WorldState _worldState;

    public SearchController(WorldState worldState)
    {
        _worldState = worldState;
    }

    [HttpGet]
    public IActionResult Search([FromQuery] string q, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { Query = q, Citizens = Array.Empty<object>(), Settlements = Array.Empty<object>(), Total = 0 });

        var lower = q.ToLowerInvariant();

        var citizenResults = _worldState.Citizens
            .Where(c => c.IsAlive && (
                c.FirstName.ToLowerInvariant().Contains(lower) ||
                c.LastName.ToLowerInvariant().Contains(lower) ||
                $"{c.FirstName} {c.LastName}".ToLowerInvariant().Contains(lower)))
            .Take(limit)
            .Select(c => new
            {
                Type = "citizen",
                Id = c.Id.Value,
                Label = $"{c.FirstName} {c.LastName}",
                SubLabel = $"Age {c.Age} - {c.CurrentActivity}",
                Location = new { c.TileX, c.TileY }
            })
            .ToList();

        var settlementResults = _worldState.Settlements
            .Where(s => s.Name.ToLowerInvariant().Contains(lower))
            .Take(limit)
            .Select(s => new
            {
                Type = "settlement",
                Id = s.Id.Value,
                Label = s.Name,
                SubLabel = $"Population {s.Population} - {s.CompletedBuildings} buildings",
                Location = new { s.TileX, s.TileY }
            })
            .ToList();

        var combined = citizenResults.Concat(settlementResults).Take(limit).ToList();

        return Ok(new
        {
            Query = q,
            Citizens = citizenResults,
            Settlements = settlementResults,
            Total = combined.Count,
            Results = combined
        });
    }
}
