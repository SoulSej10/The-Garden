using Garden.Core.Interfaces;
using Garden.Engine.Services;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class DiagnosticsController : ControllerBase
{
    private readonly SimulationCoordinator _coordinator;
    private readonly ISimulationScheduler _scheduler;
    private readonly WorldState _worldState;

    public DiagnosticsController(
        SimulationCoordinator coordinator,
        ISimulationScheduler scheduler,
        WorldState worldState)
    {
        _coordinator = coordinator;
        _scheduler = scheduler;
        _worldState = worldState;
    }

    [HttpGet]
    public IActionResult GetDiagnostics()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        var diagnostics = _coordinator.GetDiagnostics();

        return Ok(new
        {
            Simulation = new
            {
                diagnostics.TickDurationMs,
                diagnostics.TickRate,
                diagnostics.UptimeMs,
                IsRunning = _coordinator.IsRunning,
                TargetSpeed = _coordinator.TargetSpeed,
                CurrentTick = _worldState.CurrentTime.Tick,
                RegisteredSystems = _scheduler.AllSystems.Count()
            },
            Memory = new
            {
                WorkingSetMB = System.Math.Round(process.WorkingSet64 / 1024.0 / 1024.0, 1),
                PrivateMemoryMB = System.Math.Round(process.PrivateMemorySize64 / 1024.0 / 1024.0, 1),
                PagedMemoryMB = System.Math.Round(process.PagedMemorySize64 / 1024.0 / 1024.0, 1)
            },
            World = new
            {
                TotalCitizens = _worldState.Citizens.Count,
                AliveCitizens = _worldState.Citizens.Count(c => c.IsAlive),
                DeadCitizens = _worldState.Citizens.Count(c => !c.IsAlive),
                TotalSettlements = _worldState.Settlements.Count,
                MapWidth = _worldState.Map.Width,
                MapHeight = _worldState.Map.Height
            },
            Process = new
            {
                process.ProcessName,
                Threads = process.Threads.Count,
                HandleCount = process.HandleCount,
                StartTime = process.StartTime
            }
        });
    }
}
