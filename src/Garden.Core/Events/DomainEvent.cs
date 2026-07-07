using Garden.Core.Identifiers;

namespace Garden.Core.Events;

public abstract record DomainEvent : IDomainEvent
{
    public GameEntityId EventId { get; init; } = GameEntityId.New();
    public long Tick { get; init; }
    public string EventType => GetType().Name;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
