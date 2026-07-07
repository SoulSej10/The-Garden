using Garden.Core.Events;
using Garden.Core.Interfaces;

namespace Garden.Engine.Events;

public class EventBus : IEventBus
{
    private readonly List<IDomainEvent> _pendingEvents = [];
    private readonly Dictionary<Type, List<Delegate>> _handlers = [];

    public void Publish<T>(T @event) where T : IDomainEvent
    {
        _pendingEvents.Add(@event);
        if (_handlers.TryGetValue(typeof(T), out var handlers))
        {
            foreach (var handler in handlers.Cast<Action<T>>())
            {
                handler(@event);
            }
        }
    }

    public void Subscribe<T>(Action<T> handler) where T : IDomainEvent
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = [];
        _handlers[type].Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler) where T : IDomainEvent
    {
        var type = typeof(T);
        if (_handlers.TryGetValue(type, out var handlers))
            handlers.Remove(handler);
    }

    public IEnumerable<IDomainEvent> GetPendingEvents() => _pendingEvents.AsReadOnly();
    public void ClearPendingEvents() => _pendingEvents.Clear();
}
