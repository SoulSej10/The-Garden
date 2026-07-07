using Garden.Core.Identifiers;

namespace Garden.Core.Events;

public interface IDomainEvent
{
    GameEntityId EventId { get; }
    long Tick { get; }
    string EventType { get; }
    DateTime CreatedAt { get; }
}
