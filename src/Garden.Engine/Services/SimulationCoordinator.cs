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
    public bool IsRunning => _clock.IsRunning;
    public double TargetSpeed => _clock.SpeedMultiplier;

    public SimulationDiagnostics GetDiagnostics()
    {
        return new SimulationDiagnostics
        {
            TickRate = _clock.SpeedMultiplier,
            TickDurationMs = 0,
            UptimeMs = (long)(DateTime.UtcNow - _lastStartTime).TotalMilliseconds,
            RegisteredSystems = _scheduler.AllSystems.Count()
        };
    }

    private DateTime _lastStartTime = DateTime.UtcNow;

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

    public void Start()
    {
        _clock.Start();
        _lastStartTime = DateTime.UtcNow;
    }

    public void Pause()
    {
        _clock.Pause();
    }

    public void SetSpeed(double multiplier)
    {
        _clock.SetSpeed(multiplier);
    }

    private record RuleContext(
        SimulationTime CurrentTime,
        long Tick,
        IEventBus EventBus,
        IMutationCollector Mutations) : IRuleContext;
}

public class SimulationDiagnostics
{
    public double TickRate { get; init; }
    public double TickDurationMs { get; init; }
    public long UptimeMs { get; init; }
    public int RegisteredSystems { get; init; }
}
