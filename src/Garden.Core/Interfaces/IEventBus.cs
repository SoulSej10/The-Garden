using Garden.Core.Events;

namespace Garden.Core.Interfaces;

public interface IEventBus
{
    void Publish<T>(T @event) where T : IDomainEvent;
    void Subscribe<T>(Action<T> handler) where T : IDomainEvent;
    void Unsubscribe<T>(Action<T> handler) where T : IDomainEvent;
    IEnumerable<IDomainEvent> GetPendingEvents();
    void ClearPendingEvents();
}
