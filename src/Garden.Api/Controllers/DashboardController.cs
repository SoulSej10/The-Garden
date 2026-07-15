using Garden.Core.Interfaces;
using Garden.Engine.Services;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DashboardController : ControllerBase
{
    private readonly WorldState _worldState;
    private readonly SimulationCoordinator _coordinator;
    private readonly ISimulationScheduler _scheduler;
    private readonly HistoricalArchive _archive;
    private readonly StoryEngine _storyEngine;
    private readonly IMemoryCache _cache;

    public DashboardController(
        WorldState worldState,
        SimulationCoordinator coordinator,
        ISimulationScheduler scheduler,
        HistoricalArchive archive,
        StoryEngine storyEngine,
        IMemoryCache cache)
    {
        _worldState = worldState;
        _coordinator = coordinator;
        _scheduler = scheduler;
        _archive = archive;
        _storyEngine = storyEngine;
        _cache = cache;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        const string cacheKey = "dashboard-summary";
        if (_cache.TryGetValue(cacheKey, out object? cached))
            return Ok(cached);

        var time = _worldState.CurrentTime;
        var citizens = _worldState.Citizens;
        var alive = citizens.Count(c => c.IsAlive);
        var settlements = _worldState.Settlements;
        var (births, deaths) = _archive.GetVitalStats();
        var latestStory = _storyEngine.Stories.OrderByDescending(s => s.Tick).FirstOrDefault();

        var result = new
        {
            Simulation = new
            {
                Status = _coordinator.IsRunning ? "Running" : "Paused",
                Tick = time.Tick,
                Speed = _coordinator.TargetSpeed,
                // Audit finding 06: SimulationTime.Month is a real, correctly
                // computed property that was never surfaced anywhere in the
                // API or frontend - the dashboard's "world age" read as
                // years/days only, with no way to tell where in the year
                // the simulation currently was.
                WorldAge = $"{time.Year}y {time.Month}m {time.Day}d"
            },
            Population = new
            {
                Total = alive,
                Alive = alive,
                Dead = citizens.Count - alive,
                Births = births,
                Deaths = deaths,
                AverageAge = alive > 0 ? System.Math.Round(citizens.Where(c => c.IsAlive).Average(c => c.Age), 1) : 0
            },
            Environment = new
            {
                Season = time.Season.ToString(),
                Temperature = System.Math.Round(_worldState.Map.GetAllTiles().Average(t => t.Temperature), 1),
                Weather = "Active"
            },
            Settlements = new
            {
                Total = settlements.Count,
                TotalBuildings = settlements.Sum(s => s.CompletedBuildings),
                TotalFood = System.Math.Round(settlements.Sum(s => s.Storage.GetQuantity("Food")), 0),
                TotalPopulation = settlements.Sum(s => s.Population)
            },
            History = new
            {
                TotalRecords = _archive.Count,
                LatestStory = latestStory != null ? new { latestStory.Title, latestStory.Summary, latestStory.Category } : null
            }
        };

        _cache.Set(cacheKey, result, TimeSpan.FromSeconds(2));
        return Ok(result);
    }

    [HttpGet("activity")]
    public IActionResult GetActivity([FromQuery] int limit = 50)
    {
        var recent = _archive.Records
            .OrderByDescending(r => r.Tick)
            .Take(limit)
            .Select(r => new
            {
                r.Tick, r.Year, r.Day, r.Season,
                r.EventType, r.Category, r.Title, r.Description,
                r.LocationName, r.ParticipantNames, r.Importance
            })
            .ToList();

        return Ok(new { Activities = recent, Total = _archive.Count });
    }

    [HttpGet("performance")]
    public IActionResult GetPerformance()
    {
        var diagnostics = _coordinator.GetDiagnostics();

        return Ok(new
        {
            TickDuration = diagnostics.TickDurationMs,
            TickRate = diagnostics.TickRate,
            MemoryMB = System.Math.Round(System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0, 1),
            EventQueueSize = 0,
            ConnectedClients = 0,
            SimulationTick = _worldState.CurrentTime.Tick,
            UptimeMs = diagnostics.UptimeMs
        });
    }
}
