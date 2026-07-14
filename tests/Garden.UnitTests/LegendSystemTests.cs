using Garden.Core.Events;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 24: Legends & Myths - first increment per
/// specification/RFC/RFC-016-legends-myth-formation.md. A High-importance
/// HistoricalRecord, once old enough (Historical Distance), grows a
/// distorted Legend alongside (never overwriting) the original record.
/// </summary>
public class LegendSystemTests
{
    private static (WorldState world, HistoricalArchive archive, EventBus bus, LegendSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var archive = new HistoricalArchive();
        var bus = new EventBus();
        var system = new LegendSystem(world, archive, bus);
        return (world, archive, bus, system);
    }

    private static HistoricalRecord AddRecord(HistoricalArchive archive, long tick, string importance = "High", string category = HistoryCategories.Death)
    {
        var record = new HistoricalRecord
        {
            Tick = tick,
            Category = category,
            Title = "A Great Founder Has Passed",
            Description = "The founder of the settlement has died.",
            ParticipantNames = ["Founder Aldric"],
            Importance = importance
        };
        archive.Append(record);
        return record;
    }

    [Fact]
    public void NoLegendForms_WhileRecordIsStillRecent()
    {
        var (world, archive, _, system) = CreateHarness();
        AddRecord(archive, tick: 0);

        world.CurrentTime = SimulationTime.FromTick(SimulationTime.TicksPerYear);
        system.Execute();

        Assert.Empty(world.Legends);
    }

    [Fact]
    public void LegendForms_OnceRecordIsOldEnough()
    {
        var (world, archive, _, system) = CreateHarness();
        AddRecord(archive, tick: 0);

        world.CurrentTime = SimulationTime.FromTick(4 * SimulationTime.TicksPerYear);
        system.Execute();

        Assert.Single(world.Legends);
    }

    [Fact]
    public void LowImportanceRecord_NeverBecomesALegend()
    {
        var (world, archive, _, system) = CreateHarness();
        AddRecord(archive, tick: 0, importance: "Medium");

        world.CurrentTime = SimulationTime.FromTick(10 * SimulationTime.TicksPerYear);
        system.Execute();

        Assert.Empty(world.Legends);
    }

    [Fact]
    public void SameRecord_NeverProducesASecondLegend()
    {
        var (world, archive, _, system) = CreateHarness();
        AddRecord(archive, tick: 0);

        for (var year = 4; year <= 8; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * SimulationTime.TicksPerYear);
            system.Execute();
        }

        Assert.Single(world.Legends);
    }

    [Fact]
    public void PublishesLegendFormedEvent_OnFormation()
    {
        var (world, archive, bus, system) = CreateHarness();
        AddRecord(archive, tick: 0);

        var formed = false;
        bus.Subscribe<LegendFormedEvent>(_ => formed = true);

        world.CurrentTime = SimulationTime.FromTick(4 * SimulationTime.TicksPerYear);
        system.Execute();

        Assert.True(formed);
    }

    [Fact]
    public void LegendaryStatus_GrowsEachYear_CappedAt100()
    {
        var (world, archive, _, system) = CreateHarness();
        AddRecord(archive, tick: 0);

        world.CurrentTime = SimulationTime.FromTick(4 * SimulationTime.TicksPerYear);
        system.Execute();
        var initialStatus = world.Legends.Single().LegendaryStatus;

        for (var year = 5; year <= 40; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * SimulationTime.TicksPerYear);
            system.Execute();
        }

        Assert.True(world.Legends.Single().LegendaryStatus > initialStatus);
        Assert.True(world.Legends.Single().LegendaryStatus <= 100.0);
    }
}
