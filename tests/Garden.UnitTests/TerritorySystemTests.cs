using Garden.Core.Events;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 11: Territorial Influence per
/// specification/RFC/RFC-007-borders-territorial-influence.md. A regional
/// influence field derived from Population/Legitimacy drives territory
/// expansion/contraction, and pairwise disputes are detected (not resolved)
/// between overlapping settlements of comparable influence.
/// </summary>
public class TerritorySystemTests
{
    private static (WorldState world, EventBus bus, TerritorySystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var settlementManager = new SettlementManager(world, bus, NullLogger<SettlementManager>.Instance);
        var system = new TerritorySystem(world, settlementManager, bus);
        return (world, bus, system);
    }

    private static Settlement AddSettlement(WorldState world, string name, int x, int y,
        int population, double legitimacy, int territoryRadius = 5)
    {
        var settlement = new Settlement
        {
            Name = name, TileX = x, TileY = y,
            Population = population, Legitimacy = legitimacy, TerritoryRadius = territoryRadius
        };
        world.Settlements.Add(settlement);
        return settlement;
    }

    [Fact]
    public void Influence_IsComputedFromPopulationAndLegitimacy()
    {
        var (world, _, system) = CreateHarness();
        var settlement = AddSettlement(world, "A", 0, 0, population: 20, legitimacy: 50.0);

        system.Execute();

        Assert.Equal(20 * 0.5 + 50.0 * 0.3, settlement.TerritorialInfluence, precision: 5);
    }

    [Fact]
    public void Expands_WhenInfluenceRisesSignificantly()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, "A", 0, 0, population: 5, legitimacy: 0.0);

        var expanded = false;
        bus.Subscribe<SettlementExpandedEvent>(_ => expanded = true);

        system.Execute(); // baseline: influence = 2.5
        settlement.Population = 60; // influence jumps to 30 - a rise > 10
        world.CurrentTime = SimulationTime.FromTick(SimulationTime.TicksPerYear);
        var radiusBefore = settlement.TerritoryRadius;
        system.Execute();

        Assert.True(settlement.TerritoryRadius > radiusBefore);
        Assert.True(expanded);
    }

    [Fact]
    public void Contracts_WhenInfluenceFallsSignificantly()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, "A", 0, 0, population: 60, legitimacy: 100.0, territoryRadius: 5);

        var contracted = false;
        bus.Subscribe<BorderContractedEvent>(_ => contracted = true);

        system.Execute(); // baseline: influence = 60*0.5 + 100*0.3 = 60
        settlement.Population = 5; // influence falls to 2.5+30=32.5, a drop > 10
        settlement.Legitimacy = 0.0;
        world.CurrentTime = SimulationTime.FromTick(SimulationTime.TicksPerYear);
        system.Execute();

        Assert.Equal(4, settlement.TerritoryRadius);
        Assert.True(contracted);
    }

    [Fact]
    public void DoesNotContract_BelowRadiusOne()
    {
        var (world, _, system) = CreateHarness();
        var settlement = AddSettlement(world, "A", 0, 0, population: 60, legitimacy: 100.0, territoryRadius: 1);

        system.Execute();
        settlement.Population = 0;
        settlement.Legitimacy = 0.0;
        world.CurrentTime = SimulationTime.FromTick(SimulationTime.TicksPerYear);
        system.Execute();

        Assert.Equal(1, settlement.TerritoryRadius);
    }

    [Fact]
    public void DetectsDispute_WhenOverlapping_AndInfluenceComparable()
    {
        var (world, bus, system) = CreateHarness();
        AddSettlement(world, "A", 0, 0, population: 20, legitimacy: 50.0, territoryRadius: 10);
        AddSettlement(world, "B", 15, 0, population: 20, legitimacy: 50.0, territoryRadius: 10);

        var disputeCount = 0;
        bus.Subscribe<BorderDisputeBeginsEvent>(_ => disputeCount++);

        system.Execute();

        Assert.Equal(1, disputeCount);
    }

    [Fact]
    public void DoesNotDetectDispute_WhenTerritoriesDoNotOverlap()
    {
        var (world, bus, system) = CreateHarness();
        AddSettlement(world, "A", 0, 0, population: 20, legitimacy: 50.0, territoryRadius: 3);
        AddSettlement(world, "B", 50, 0, population: 20, legitimacy: 50.0, territoryRadius: 3);

        var disputed = false;
        bus.Subscribe<BorderDisputeBeginsEvent>(_ => disputed = true);

        system.Execute();

        Assert.False(disputed);
    }

    [Fact]
    public void DoesNotDetectDispute_WhenInfluenceGapTooLarge()
    {
        var (world, bus, system) = CreateHarness();
        AddSettlement(world, "A", 0, 0, population: 100, legitimacy: 100.0, territoryRadius: 10);
        AddSettlement(world, "B", 15, 0, population: 2, legitimacy: 0.0, territoryRadius: 10);

        var disputed = false;
        bus.Subscribe<BorderDisputeBeginsEvent>(_ => disputed = true);

        system.Execute();

        Assert.False(disputed);
    }

    [Fact]
    public void Dispute_DoesNotRefire_WhileStillOverlapping()
    {
        var (world, bus, system) = CreateHarness();
        AddSettlement(world, "A", 0, 0, population: 20, legitimacy: 50.0, territoryRadius: 10);
        AddSettlement(world, "B", 15, 0, population: 20, legitimacy: 50.0, territoryRadius: 10);

        var disputeCount = 0;
        bus.Subscribe<BorderDisputeBeginsEvent>(_ => disputeCount++);

        system.Execute();
        world.CurrentTime = SimulationTime.FromTick(SimulationTime.TicksPerYear);
        system.Execute();
        world.CurrentTime = SimulationTime.FromTick(672);
        system.Execute();

        Assert.Equal(1, disputeCount);
    }
}
