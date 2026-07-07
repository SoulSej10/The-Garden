using Garden.Engine.Time;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class SimulationHostedService : BackgroundService
{
    private readonly SimulationCoordinator _coordinator;
    private readonly SimulationClock _clock;
    private readonly ILogger<SimulationHostedService> _logger;

    public SimulationHostedService(
        SimulationCoordinator coordinator,
        SimulationClock clock,
        ILogger<SimulationHostedService> logger)
    {
        _coordinator = coordinator;
        _clock = clock;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Simulation engine started");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_clock.IsRunning)
            {
                var delay = _clock.GetTickDelayMs();
                await Task.Delay(delay, stoppingToken);
                await _coordinator.ExecuteTickAsync();
            }
            else
            {
                await Task.Delay(100, stoppingToken);
            }
        }

        _logger.LogInformation("Simulation engine stopped");
    }
}
