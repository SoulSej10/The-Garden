using Garden.Engine.Services;
using Garden.Infrastructure.Services;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SystemController : ControllerBase
{
    private readonly WorldState _worldState;
    private readonly SimulationCoordinator _coordinator;
    private readonly SaveLoadService _saveLoadService;
    private readonly BackupService _backupService;
    private static readonly DateTime _startTime = DateTime.UtcNow;

    public SystemController(
        WorldState worldState,
        SimulationCoordinator coordinator,
        SaveLoadService saveLoadService,
        BackupService backupService)
    {
        _worldState = worldState;
        _coordinator = coordinator;
        _saveLoadService = saveLoadService;
        _backupService = backupService;
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            Status = "Healthy",
            Uptime = (DateTime.UtcNow - _startTime).TotalSeconds,
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0",
            SimulationRunning = _coordinator.IsRunning,
            DatabaseConnected = false
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
                TechnologiesDiscovered = _worldState.Technologies.Count(t => t.IsDiscovered),
                Religions = _worldState.Religions.Count,
                HistoryRecords = 0
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
}

public record SaveRequest(string? Name);
public record LoadRequest(string Name);
