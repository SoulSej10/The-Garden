namespace Garden.Core.Interfaces;

public interface ISimulationScheduler
{
    void Register(IScheduledSystem system);
    void Unregister(IScheduledSystem system);
    IReadOnlyList<IScheduledSystem> GetEligibleSystems(long tick);
    IEnumerable<IScheduledSystem> AllSystems { get; }
}

public interface IScheduledSystem
{
    string Name { get; }
    long IntervalTicks { get; }
    long NextExecutionTick { get; }
    void Execute();
}
