using Garden.Core.Interfaces;

namespace Garden.Engine.Mutations;

public record WorldMutation(string Description) : IWorldMutation;

public class MutationCollector : IMutationCollector
{
    private readonly List<IWorldMutation> _mutations = [];

    public void Add(IWorldMutation mutation) => _mutations.Add(mutation);
    public IReadOnlyList<IWorldMutation> GetPending() => _mutations.AsReadOnly();
    public void Clear() => _mutations.Clear();
}
