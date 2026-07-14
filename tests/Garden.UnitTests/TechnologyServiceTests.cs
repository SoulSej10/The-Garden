using Garden.Core.Identifiers;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// Regression test for a bug found during Week 5 Day 22 live verification
/// (documented on TechnologyService.EvaluateTechnology): each Technology's
/// CurrentProgress used to accumulate at 10% of the settlement-level
/// progress value, with nothing else in the codebase treating the two as
/// different scales - making even the cheapest technology take ~385
/// simulated years to discover for a 26-member settlement. Asserts
/// discovery now happens within a bounded, realistic number of yearly
/// evaluations instead.
/// </summary>
public class TechnologyServiceTests
{
    private static (WorldState world, TechnologyService technology) CreateHarness()
    {
        var world = new WorldState();
        var eventBus = new EventBus();
        var technology = new TechnologyService(world, eventBus, NullLogger<TechnologyService>.Instance);
        return (world, technology);
    }

    private static Settlement AddSettlement(WorldState world, int memberCount)
    {
        var settlement = new Settlement { Name = "Testford" };
        for (var i = 0; i < memberCount; i++)
            settlement.MemberIds.Add(GameEntityId.New());
        world.Settlements.Add(settlement);
        return settlement;
    }

    [Fact]
    public void CheapestTechnology_Discovers_WithinRealisticYearlyEvaluations()
    {
        var (world, technology) = CreateHarness();
        AddSettlement(world, memberCount: 26);

        // The previous 0.1x scale-down needed ~385 yearly evaluations
        // (simulated years) to cross the cheapest threshold (Stone Tools,
        // 30.0). 60 years is a generous but still realistic upper bound for
        // a settlement's first technology.
        for (var year = 1; year <= 60; year++)
            technology.EvaluateTechnology(tick: year * SimulationTime.TicksPerYear);

        Assert.NotEmpty(technology.GetDiscoveredTechnologies());
    }

    [Fact]
    public void DiscoveredTechnology_RecordsDiscovererCitizen()
    {
        var (world, technology) = CreateHarness();
        var settlement = AddSettlement(world, memberCount: 26);
        var smartest = new Citizen { FirstName = "Smart", LastName = "One", IsAlive = true };
        smartest.Attributes.Intelligence = 10.0;
        settlement.MemberIds.Add(smartest.Id);
        world.Citizens.Add(smartest);

        for (var year = 1; year <= 60; year++)
            technology.EvaluateTechnology(tick: year * SimulationTime.TicksPerYear);

        var discovered = Assert.Single(technology.GetDiscoveredTechnologies().Take(1));
        Assert.Equal("Smart One", discovered.DiscoveredByCitizenName);
    }

    [Fact]
    public void TooSmallSettlement_NeverEvaluated()
    {
        var (world, technology) = CreateHarness();
        AddSettlement(world, memberCount: 1); // below the >= 2 member gate

        for (var year = 1; year <= 60; year++)
            technology.EvaluateTechnology(tick: year * SimulationTime.TicksPerYear);

        Assert.Empty(technology.GetDiscoveredTechnologies());
    }

    [Fact]
    public void OneSettlementDiscovering_DoesNotLockOutAnother()
    {
        // RFC-015 (ADR-004): the old shared Technology.CurrentProgress/
        // IsDiscovered pair meant the first settlement to cross a threshold
        // permanently locked every other settlement out of that technology.
        // Confirms independent discovery is now real: settlement A racing
        // ahead does not prevent settlement B from later discovering the
        // exact same technology on its own.
        var (world, technology) = CreateHarness();
        var fast = AddSettlement(world, memberCount: 26);
        var slow = AddSettlement(world, memberCount: 26);

        for (var year = 1; year <= 60; year++)
            technology.EvaluateTechnology(tick: year * SimulationTime.TicksPerYear);

        Assert.NotEmpty(technology.GetDiscoveredTechnologies(fast.Id));
        Assert.NotEmpty(technology.GetDiscoveredTechnologies(slow.Id));
    }

    [Fact]
    public void TechnologicalDivergenceEvent_FiresOncePairsDivergeEnough()
    {
        var world = new WorldState();
        var eventBus = new EventBus();
        var technology = new TechnologyService(world, eventBus, NullLogger<TechnologyService>.Instance);

        var advanced = AddSettlement(world, memberCount: 40);
        var stagnant = AddSettlement(world, memberCount: 2);

        var divergenceCount = 0;
        eventBus.Subscribe<Garden.Core.Events.TechnologicalDivergenceEvent>(_ => divergenceCount++);

        for (var year = 1; year <= 80; year++)
            technology.EvaluateTechnology(tick: year * SimulationTime.TicksPerYear);

        Assert.True(divergenceCount > 0);
    }
}
