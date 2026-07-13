using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 6 Days 26-28: pairwise settlement Language
/// Divergence per specification/RFC/RFC-003-language-divergence.md. No row
/// exists between settlements that have never had real contact; contact
/// (an active TradeRoute or a positive DiplomaticRelation) pulls Divergence
/// down, isolation pushes it up, and DialectFormedEvent fires exactly once
/// when the threshold is crossed.
/// </summary>
public class LanguageSystemTests
{
    private static (WorldState world, EventBus bus, LanguageSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new LanguageSystem(world, bus);
        return (world, bus, system);
    }

    private static Settlement AddSettlement(WorldState world, string name)
    {
        var settlement = new Settlement { Name = name };
        world.Settlements.Add(settlement);
        return settlement;
    }

    [Fact]
    public void NoDivergenceRow_ExistsByDefault_BetweenSettlementsWithNoContact()
    {
        var (world, _, system) = CreateHarness();
        AddSettlement(world, "A");
        AddSettlement(world, "B");

        system.Execute();

        Assert.Empty(world.LanguageDivergences);
    }

    [Fact]
    public void ActiveTradeRoute_CreatesRow_AndConverges()
    {
        var (world, _, system) = CreateHarness();
        var a = AddSettlement(world, "A");
        var b = AddSettlement(world, "B");
        world.TradeRoutes.Add(new TradeRoute
        {
            FromSettlementId = a.Id,
            ToSettlementId = b.Id,
            IsActive = true
        });

        // Start with some existing divergence to observe it decreasing.
        var row = system.GetOrCreate(a.Id, b.Id, tick: 0);
        row.Divergence = 20.0;

        system.Execute();

        Assert.True(row.Divergence < 20.0,
            $"Expected Divergence to decrease with active trade contact, but stayed at {row.Divergence}");
    }

    [Fact]
    public void Isolation_IncreasesDivergence_ForAnExistingPair()
    {
        var (world, _, system) = CreateHarness();
        var a = AddSettlement(world, "A");
        var b = AddSettlement(world, "B");
        var row = system.GetOrCreate(a.Id, b.Id, tick: 0);
        row.Divergence = 10.0;

        system.Execute();

        Assert.True(row.Divergence > 10.0,
            $"Expected Divergence to increase without any contact, but stayed at {row.Divergence}");
    }

    [Fact]
    public void DialectFormed_FiresExactlyOnce_WhenThresholdCrossed()
    {
        var (world, bus, system) = CreateHarness();
        var a = AddSettlement(world, "Alpha");
        var b = AddSettlement(world, "Beta");
        var row = system.GetOrCreate(a.Id, b.Id, tick: 0);

        var fireCount = 0;
        bus.Subscribe<DialectFormedEvent>(_ => fireCount++);

        // No contact at all - divergence grows every yearly evaluation
        // until it crosses the threshold, then must never fire again.
        for (var year = 1; year <= 30; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * SimulationTime.TicksPerYear);
            system.Execute();
        }

        Assert.Equal(1, fireCount);
        Assert.True(row.DialectFormed);
    }

    [Fact]
    public void PositiveDiplomaticRelation_CountsAsContact_EvenWithoutATradeRoute()
    {
        var (world, _, system) = CreateHarness();
        var a = AddSettlement(world, "A");
        var b = AddSettlement(world, "B");
        world.DiplomaticRelations.Add(new Garden.World.Entities.DiplomaticRelation
        {
            EntityAId = a.Id,
            EntityAIsSettlement = true,
            EntityBId = b.Id,
            EntityBIsSettlement = true,
            RelationScore = 80.0
        });

        var row = system.GetOrCreate(a.Id, b.Id, tick: 0);
        row.Divergence = 15.0;

        system.Execute();

        Assert.True(row.Divergence < 15.0,
            $"Expected a positive DiplomaticRelation to count as contact and converge, but stayed at {row.Divergence}");
    }

    [Fact]
    public void Divergence_IsStoredOnce_RegardlessOfWhichSettlementIsPassedFirst()
    {
        var (world, _, system) = CreateHarness();
        var a = AddSettlement(world, "A");
        var b = AddSettlement(world, "B");

        system.GetOrCreate(a.Id, b.Id, tick: 0);
        system.GetOrCreate(b.Id, a.Id, tick: 1);

        Assert.Single(world.LanguageDivergences);
    }
}
