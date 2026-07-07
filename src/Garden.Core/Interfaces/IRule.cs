using Garden.Core.Time;

namespace Garden.Core.Interfaces;

public interface IRule
{
    string Name { get; }
    bool CanExecute(SimulationTime time);
    RuleResult Execute(IRuleContext context);
}

public interface IRuleContext
{
    SimulationTime CurrentTime { get; }
    long Tick { get; }
    IEventBus EventBus { get; }
    IMutationCollector Mutations { get; }
}

public interface IMutationCollector
{
    void Add(IWorldMutation mutation);
    IReadOnlyList<IWorldMutation> GetPending();
    void Clear();
}

public record RuleResult(bool Executed, string? Description = null)
{
    public static RuleResult Skipped => new(false);
    public static RuleResult Completed(string? description = null) => new(true, description);
}

public interface IWorldMutation
{
    string Description { get; }
}
