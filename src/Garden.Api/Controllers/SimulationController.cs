using Garden.Engine.Services;
using Garden.Engine.Time;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SimulationController : ControllerBase
{
    private readonly SimulationCoordinator _coordinator;
    private readonly SimulationClock _clock;
    private readonly WorldState _worldState;

    public SimulationController(
        SimulationCoordinator coordinator,
        SimulationClock clock,
        WorldState worldState)
    {
        _coordinator = coordinator;
        _clock = clock;
        _worldState = worldState;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            IsRunning = _clock.IsRunning,
            TotalTicks = _clock.TotalTicks,
            Speed = _clock.SpeedMultiplier,
            Time = _clock.CurrentTime.ToString()
        });
    }

    [HttpGet("time")]
    public IActionResult GetTime()
    {
        return Ok(new
        {
            Tick = _clock.TotalTicks,
            Time = _clock.CurrentTime.ToString(),
            _worldState.CurrentTime
        });
    }

    [HttpPost("start")]
    public IActionResult Start()
    {
        _clock.Start();
        return Ok(new { Status = "Started" });
    }

    [HttpPost("pause")]
    public IActionResult Pause()
    {
        _clock.Pause();
        return Ok(new { Status = "Paused" });
    }

    [HttpPost("resume")]
    public IActionResult Resume()
    {
        _clock.Start();
        return Ok(new { Status = "Resumed" });
    }

    [HttpPost("step")]
    public async Task<IActionResult> Step()
    {
        _clock.Start();
        await _coordinator.ExecuteTickAsync();
        _clock.Pause();
        return Ok(new { Tick = _clock.TotalTicks, Time = _clock.CurrentTime.ToString() });
    }

    [HttpPost("speed")]
    public IActionResult SetSpeed([FromBody] SpeedRequest request)
    {
        _clock.SetSpeed(request.Speed);
        return Ok(new { Speed = request.Speed });
    }
}

public record SpeedRequest(double Speed);
