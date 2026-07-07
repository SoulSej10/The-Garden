using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.Engine.Mutations;
using Garden.Engine.Time;
using Garden.World.Collections;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class SimulationCoordinator
{
    private readonly SimulationClock _clock;
    private readonly IEventBus _eventBus;
    private readonly ISimulationScheduler _scheduler;
    private readonly MutationCollector _mutationCollector;
    private readonly WorldState _worldState;
    private readonly ILogger<SimulationCoordinator> _logger;
    private readonly List<IRule> _rules = [];

    public IReadOnlyList<IRule> Rules => _rules.AsReadOnly();
    public SimulationClock Clock => _clock;

    public SimulationCoordinator(
        SimulationClock clock,
        IEventBus eventBus,
        ISimulationScheduler scheduler,
        WorldState worldState,
        ILogger<SimulationCoordinator> logger)
    {
        _clock = clock;
        _eventBus = eventBus;
        _scheduler = scheduler;
        _mutationCollector = new MutationCollector();
        _worldState = worldState;
        _logger = logger;
    }

    public void RegisterRule(IRule rule) => _rules.Add(rule);

    public async Task ExecuteTickAsync()
    {
        var tick = _clock.AdvanceTick();
        if (tick == 0) return;

        var startTime = DateTime.UtcNow;
        var time = SimulationTime.FromTick(tick);
        _worldState.CurrentTime = time;

        var context = new RuleContext(time, tick, _eventBus, _mutationCollector);

        foreach (var rule in _rules)
        {
            if (!rule.CanExecute(time)) continue;
            var result = rule.Execute(context);
            if (result.Executed)
            {
                _logger.LogDebug("Rule {Rule} executed: {Desc}", rule.Name, result.Description);
            }
        }

        var eligible = _scheduler.GetEligibleSystems(tick);
        foreach (var system in eligible)
        {
            var sysStart = DateTime.UtcNow;
            system.Execute();
            var sysDuration = (DateTime.UtcNow - sysStart).TotalMilliseconds;
            _logger.LogTrace("System {System} executed in {Duration:F1}ms", system.Name, sysDuration);
        }

        _eventBus.ClearPendingEvents();
        _mutationCollector.Clear();

        var elapsed = DateTime.UtcNow - startTime;
        if (elapsed.TotalMilliseconds > 50)
        {
            _logger.LogWarning("Tick {Tick} took {Duration:F1}ms (threshold: 50ms)", tick, elapsed.TotalMilliseconds);
        }

        await Task.CompletedTask;
    }

    private record RuleContext(
        SimulationTime CurrentTime,
        long Tick,
        IEventBus EventBus,
        IMutationCollector Mutations) : IRuleContext;
}
