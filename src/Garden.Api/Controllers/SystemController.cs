using System.Security.Cryptography;
using System.Text;
using Garden.Engine.Generation;
using Garden.Engine.Services;
using Garden.Infrastructure.Persistence;
using Garden.Infrastructure.Services;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SystemController : ControllerBase
{
    private readonly WorldState _worldState;
    private readonly SimulationCoordinator _coordinator;
    private readonly SaveLoadService _saveLoadService;
    private readonly BackupService _backupService;
    private readonly GardenDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly SpawnSystem _spawnSystem;
    private readonly WorldInitializer _worldInitializer;
    private readonly HistoryManager _historyManager;
    private readonly PopulationManager _populationManager;
    private readonly ILogger<SystemController> _logger;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public SystemController(
        WorldState worldState,
        SimulationCoordinator coordinator,
        SaveLoadService saveLoadService,
        BackupService backupService,
        GardenDbContext db,
        IConfiguration configuration,
        SpawnSystem spawnSystem,
        WorldInitializer worldInitializer,
        HistoryManager historyManager,
        PopulationManager populationManager,
        ILogger<SystemController> logger)
    {
        _worldState = worldState;
        _coordinator = coordinator;
        _saveLoadService = saveLoadService;
        _backupService = backupService;
        _db = db;
        _configuration = configuration;
        _spawnSystem = spawnSystem;
        _worldInitializer = worldInitializer;
        _historyManager = historyManager;
        _populationManager = populationManager;
        _logger = logger;
    }

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        var databaseConnected = await _db.Database.CanConnectAsync();

        return Ok(new
        {
            Status = "Healthy",
            Uptime = (DateTime.UtcNow - _startTime).TotalSeconds,
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            SimulationRunning = _coordinator.IsRunning,
            DatabaseConnected = databaseConnected
        });
    }

    [HttpGet("statistics")]
    public IActionResult GetStatistics()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var time = _worldState.CurrentTime;
        var alive = _worldState.Citizens.Count(c => c.IsAlive);

        return Ok(new
        {
            Simulation = new
            {
                Tick = time.Tick,
                Year = time.Year,
                Day = time.Day,
                Season = time.Season.ToString(),
                IsRunning = _coordinator.IsRunning,
                Speed = _coordinator.TargetSpeed,
                TickDurationMs = _coordinator.GetDiagnostics().TickDurationMs,
                UptimeMs = _coordinator.GetDiagnostics().UptimeMs
            },
            World = new
            {
                TotalCitizens = _worldState.Citizens.Count,
                AliveCitizens = alive,
                DeadCitizens = _worldState.Citizens.Count - alive,
                TotalSettlements = _worldState.Settlements.Count,
                ActiveKingdoms = _worldState.Kingdoms.Count(k => k.IsActive),
                TotalBuildings = _worldState.Settlements.Sum(s => s.CompletedBuildings),
                ActiveTradeRoutes = _worldState.TradeRoutes.Count(r => r.IsActive),
                TechnologiesDiscovered = _worldState.SettlementTechnologies.Count(t => t.IsDiscovered),
                Religions = _worldState.Religions.Count,
                HistoryRecords = _historyManager.Archive.Count
            },
            Performance = new
            {
                WorkingSetMB = Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 1),
                PrivateMemoryMB = Math.Round(process.PrivateMemorySize64 / 1024.0 / 1024.0, 1),
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                CpuTime = process.TotalProcessorTime.TotalSeconds
            }
        });
    }

    [HttpGet("saves")]
    public IActionResult ListSaves()
    {
        var saves = _saveLoadService.ListSaves().Select(s => new
        {
            s.Name, s.SizeBytes, LastModified = s.LastModified.ToString("o")
        }).ToList();
        return Ok(new { Saves = saves, Count = saves.Count });
    }

    // RFC-017: flat lineage list - the Observatory builds the branch tree
    // client-side from each entry's parentSaveId, per TG-OBS-007's Timeline
    // Branching section.
    [HttpGet("timeline")]
    public IActionResult GetTimeline()
    {
        var timeline = _saveLoadService.GetTimeline().Select(e => new
        {
            e.Id, e.ParentSaveId, e.Name, e.Tick, SavedAt = e.SavedAt.ToString("o")
        }).ToList();
        return Ok(timeline);
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] SaveRequest request)
    {
        var name = string.IsNullOrWhiteSpace(request.Name)
            ? $"world_{DateTime.UtcNow:yyyyMMdd-HHmmss}"
            : request.Name;
        var success = await _saveLoadService.SaveAsync(name);
        if (!success) return StatusCode(500, new { Error = "Failed to save world" });
        return Ok(new { Name = name, SavedAt = DateTime.UtcNow });
    }

    [HttpPost("load")]
    public async Task<IActionResult> Load([FromBody] LoadRequest request)
    {
        var success = await _saveLoadService.LoadAsync(request.Name);
        if (!success) return NotFound(new { Error = $"Save '{request.Name}' not found" });
        return Ok(new { Name = request.Name, LoadedAt = DateTime.UtcNow });
    }

    [HttpDelete("saves/{name}")]
    public IActionResult DeleteSave(string name)
    {
        var success = _saveLoadService.DeleteSave(name);
        if (!success) return NotFound(new { Error = $"Save '{name}' not found" });
        return Ok(new { Name = name, Deleted = true });
    }

    [HttpGet("backups")]
    public IActionResult ListBackups()
    {
        var backups = _backupService.ListBackups().Select(b => new
        {
            b.Name, b.Type, b.CitizenCount, b.SettlementCount,
            CreatedAt = b.CreatedAt.ToString("o")
        }).ToList();
        return Ok(new { Backups = backups, Count = backups.Count });
    }

    /// <summary>
    /// Dev-only: wipes the entire world (database + in-memory state +
    /// history) and spawns a fresh population of 50 citizens on a newly
    /// generated map. Gated by a password whose SHA-256 hash is configured
    /// via Admin:ResetPasswordHash - never compares or logs the plaintext.
    /// Not part of the normal Observatory (observe-only) surface; this
    /// exists to recover from a fully-dead saved world without a redeploy.
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetWorld([FromBody] ResetWorldRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { Error = "Password is required." });

        var configuredHash = _configuration["Admin:ResetPasswordHash"];
        if (string.IsNullOrWhiteSpace(configuredHash))
            return StatusCode(500, new { Error = "World reset is not configured on this server." });

        var submittedHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Password))).ToLowerInvariant();
        var expectedBytes = Encoding.UTF8.GetBytes(configuredHash.Trim().ToLowerInvariant());
        var actualBytes = Encoding.UTF8.GetBytes(submittedHash);

        var isMatch = expectedBytes.Length == actualBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);

        if (!isMatch)
        {
            _logger.LogWarning("Rejected world reset attempt with an incorrect password");
            return Unauthorized(new { Error = "Incorrect password." });
        }

        try
        {
            _coordinator.Pause();

            // Both ExecuteDeleteAsync and load+RemoveRange fail with a
            // foreign key violation once a settlement has accumulated child
            // rows (CulturalTrait, Building, Building_Items,
            // Settlements_Items) or a citizen has memories (CitizenMemory) -
            // any world that's been running for a while will have these.
            // EF's owned-entity cascade tracking doesn't reliably reach
            // every one of these tables, so delete children before parents
            // explicitly, in dependency order.
            await _db.Database.ExecuteSqlRawAsync(@"
                DELETE FROM ""CitizenMemory"";
                DELETE FROM ""Building_Items"";
                DELETE FROM ""Building"";
                DELETE FROM ""Settlements_Items"";
                DELETE FROM ""CulturalTrait"";
                DELETE FROM ""Citizens"";
                DELETE FROM ""Settlements"";
            ");

            _worldState.Citizens.Clear();
            _worldState.Settlements.Clear();
            _worldState.Kingdoms.Clear();
            _worldState.DiplomaticRelations.Clear();
            _worldState.TradeRoutes.Clear();
            _worldState.SettlementTechnologies.Clear();
            _worldState.Religions.Clear();
            _worldState.EnvironmentEvents.Clear();

            _historyManager.Archive.Clear();
            _populationManager.Reset();
            _saveLoadService.ResetLineage();

            var seed = Random.Shared.Next();
            _worldInitializer.Reinitialize(width: 100, height: 100, seed: seed);
            _spawnSystem.SpawnInitialPopulation(count: 50);

            _coordinator.Clock.Reset();
            _coordinator.Start();

            _logger.LogWarning("World reset complete - fresh population spawned (seed {Seed})", seed);

            return Ok(new
            {
                Message = "World reset complete. A fresh population of 50 citizens has been spawned.",
                Seed = seed,
                CitizenCount = _worldState.Citizens.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "World reset failed");
            return StatusCode(500, new { Error = "Reset failed. The world may be in a partially-reset state - check server logs." });
        }
    }
}

public record SaveRequest(string? Name);
public record LoadRequest(string Name);
public record ResetWorldRequest(string Password);
