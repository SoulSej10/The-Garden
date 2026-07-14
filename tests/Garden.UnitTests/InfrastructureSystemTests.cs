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
/// DEVELOPMENT_PLAN.md Weeks 21-22: Infrastructure - Route Quality per
/// specification/RFC/RFC-014-infrastructure-route-quality.md. Grows/decays
/// an already-existing TradeRoute's InfrastructureQuality based on
/// sustained use or neglect, per ADR-003's decision to reuse TradeRoute
/// rather than rewrite Building/ConstructionSystem.
/// </summary>
public class InfrastructureSystemTests
{
    private static (WorldState world, EventBus bus, InfrastructureSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new InfrastructureSystem(world, bus);
        return (world, bus, system);
    }

    private static TradeRoute AddRoute(WorldState world, bool isActive = true, int tripCount = 0)
    {
        var route = new TradeRoute
        {
            FromSettlementId = GameEntityId.New(), FromSettlementName = "Rivermoot",
            ToSettlementId = GameEntityId.New(), ToSettlementName = "Upperridge",
            IsActive = isActive, TripCount = tripCount
        };
        world.TradeRoutes.Add(route);
        return route;
    }

    [Fact]
    public void QualityStaysZero_WhenNoNewTripsOccur()
    {
        var (world, _, system) = CreateHarness();
        var route = AddRoute(world);

        system.Execute();

        Assert.Equal(0, route.InfrastructureQuality);
    }

    [Fact]
    public void QualityGrows_AsActiveRouteAccumulatesTrips()
    {
        var (world, _, system) = CreateHarness();
        var route = AddRoute(world, tripCount: 0);

        system.Execute(); // baseline, 0 trips gained

        route.TripCount = 10; // 10 new trips since baseline
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        Assert.True(route.InfrastructureQuality > 0);
    }

    [Fact]
    public void QualityDecays_ForInactiveRoute()
    {
        var (world, _, system) = CreateHarness();
        var route = AddRoute(world, isActive: false);
        route.InfrastructureQuality = 30.0;

        system.Execute();

        Assert.True(route.InfrastructureQuality < 30.0);
    }

    [Fact]
    public void QualityDoesNotGoNegative()
    {
        var (world, _, system) = CreateHarness();
        var route = AddRoute(world, isActive: false);
        route.InfrastructureQuality = 1.0;

        system.Execute();

        Assert.Equal(0, route.InfrastructureQuality);
    }

    [Fact]
    public void PublishesRoadConstructed_WhenQualityCrossesRoadWorthyThreshold()
    {
        var (world, bus, system) = CreateHarness();
        var route = AddRoute(world, tripCount: 0);

        system.Execute(); // baseline, no trips gained yet

        var constructed = false;
        bus.Subscribe<RoadConstructedEvent>(_ => constructed = true);

        // Enough trips gained since baseline to cross the road-worthy threshold.
        route.TripCount = 30;
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        Assert.True(constructed);
    }

    [Fact]
    public void DoesNotRefireRoadConstructed_WhileStillRoadWorthy()
    {
        var (world, bus, system) = CreateHarness();
        var route = AddRoute(world, tripCount: 0);

        system.Execute(); // baseline

        var constructedCount = 0;
        bus.Subscribe<RoadConstructedEvent>(_ => constructedCount++);

        route.TripCount = 30;
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        route.TripCount = 40;
        world.CurrentTime = SimulationTime.FromTick(24 * 30 * 2);
        system.Execute();

        Assert.Equal(1, constructedCount);
    }

    [Fact]
    public void PublishesInfrastructureFailure_WhenARoadRevertsToFootpath()
    {
        var (world, bus, system) = CreateHarness();
        var route = AddRoute(world, tripCount: 0);

        system.Execute(); // baseline

        route.TripCount = 30;
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute(); // becomes road-worthy

        var failed = false;
        bus.Subscribe<InfrastructureFailureEvent>(_ => failed = true);

        route.IsActive = false;
        for (var i = 0; i < 11; i++)
        {
            world.CurrentTime = SimulationTime.FromTick((i + 2) * 24 * 30);
            system.Execute();
        }

        Assert.True(failed);
    }
}
