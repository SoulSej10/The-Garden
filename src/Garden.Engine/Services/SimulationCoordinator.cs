using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.Engine.Mutations;
using Garden.Engine.Time;
using Garden.World.Collections;

namespace Garden.Engine.Services;

public class SimulationCoordinator
{
    private readonly SimulationClock _clock;
    private readonly IEventBus _eventBus;
    private readonly ISimulationScheduler _scheduler;
    private readonly MutationCollector _mutationCollector;
    private readonly WorldState _worldState;
    private readonly List<IRule> _rules = [];

    public IReadOnlyList<IRule> Rules => _rules.AsReadOnly();
    public SimulationClock Clock => _clock;

    public SimulationCoordinator(
        SimulationClock clock,
        IEventBus eventBus,
        ISimulationScheduler scheduler,
        WorldState worldState)
    {
        _clock = clock;
        _eventBus = eventBus;
        _scheduler = scheduler;
        _mutationCollector = new MutationCollector();
        _worldState = worldState;
    }

    public void RegisterRule(IRule rule) => _rules.Add(rule);

    public async Task ExecuteTickAsync()
    {
        var tick = _clock.AdvanceTick();
        if (tick == 0) return;

        var time = SimulationTime.FromTick(tick);
        _worldState.CurrentTime = time;

        var context = new RuleContext(time, tick, _eventBus, _mutationCollector);

        foreach (var rule in _rules)
        {
            if (!rule.CanExecute(time)) continue;
            rule.Execute(context);
        }

        var eligible = _scheduler.GetEligibleSystems(tick);
        foreach (var system in eligible)
        {
            system.Execute();
        }

        _eventBus.ClearPendingEvents();
        _mutationCollector.Clear();

        await Task.CompletedTask;
    }

    private record RuleContext(
        SimulationTime CurrentTime,
        long Tick,
        IEventBus EventBus,
        IMutationCollector Mutations) : IRuleContext;
}
