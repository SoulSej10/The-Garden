using Garden.Core.Interfaces;

namespace Garden.Engine.Scheduling;

public class SimulationScheduler : ISimulationScheduler
{
    private readonly List<IScheduledSystem> _systems = [];

    public void Register(IScheduledSystem system) => _systems.Add(system);
    public void Unregister(IScheduledSystem system) => _systems.Remove(system);

    public IReadOnlyList<IScheduledSystem> GetEligibleSystems(long tick)
    {
        return _systems
            .Where(s => tick >= s.NextExecutionTick)
            .ToList()
            .AsReadOnly();
    }

    public IEnumerable<IScheduledSystem> AllSystems => _systems.AsReadOnly();
}
