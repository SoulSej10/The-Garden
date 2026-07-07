using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Engine.Events;
using Xunit;

namespace Garden.UnitTests;

public class EventBusTests
{
    [Fact]
    public void Publish_AddsPendingEvent()
    {
        var bus = new EventBus();
        bus.Publish(new TestEvent());
        Assert.Single(bus.GetPendingEvents());
    }

    [Fact]
    public void Subscribe_CallsHandler()
    {
        var bus = new EventBus();
        var called = false;
        bus.Subscribe<TestEvent>(_ => called = true);
        bus.Publish(new TestEvent());
        Assert.True(called);
    }

    [Fact]
    public void ClearPendingEvents_RemovesAll()
    {
        var bus = new EventBus();
        bus.Publish(new TestEvent());
        bus.ClearPendingEvents();
        Assert.Empty(bus.GetPendingEvents());
    }

    [Fact]
    public void Unsubscribe_RemovesHandler()
    {
        var bus = new EventBus();
        var count = 0;
        void Handler(TestEvent _) => count++;
        bus.Subscribe<TestEvent>(Handler);
        bus.Publish(new TestEvent());
        bus.Unsubscribe<TestEvent>(Handler);
        bus.Publish(new TestEvent());
        Assert.Equal(1, count);
    }

    private record TestEvent : DomainEvent;
}
