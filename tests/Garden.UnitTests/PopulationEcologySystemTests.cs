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
/// DEVELOPMENT_PLAN.md Week 14: Population Ecology - Carrying Capacity per
/// specification/RFC/RFC-008-population-ecology-carrying-capacity.md.
/// CarryingCapacity is derived from existing Food/housing inputs
/// (ADR-002); PopulationDeclineEvent/PopulationBoomEvent are detected
/// (not resolved) as a settlement's population crosses its capacity.
/// </summary>
public class PopulationEcologySystemTests
{
    private static (WorldState world, EventBus bus, PopulationEcologySystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new PopulationEcologySystem(world, bus);
        return (world, bus, system);
    }

    private static Settlement AddSettlement(WorldState world, int members, int houses, double food)
    {
        var settlement = new Settlement { Name = "Rivermoot", TileX = 0, TileY = 0 };

        for (var i = 0; i < houses; i++)
            settlement.Buildings.Add(new Building { BuildingType = BuildingTypes.House, Status = BuildingStatus.Completed });

        for (var i = 0; i < members; i++)
            settlement.MemberIds.Add(GameEntityId.New());

        settlement.Storage.Add("Food", food);
        world.Settlements.Add(settlement);
        return settlement;
    }

    [Fact]
    public void CarryingCapacity_IsTheLesserOfHousingAndFoodCapacity()
    {
        var (world, _, system) = CreateHarness();
        // One House's real max-occupants sum is asserted against directly
        // (via settlement.HousingCapacity), rather than hardcoding the
        // constant here, so this stays honest if that constant ever changes.
        var settlement = AddSettlement(world, members: 5, houses: 1, food: 300); // food capacity = 100

        system.Execute();

        var expectedFoodCapacity = 300 / 3.0;
        Assert.Equal(Math.Min(settlement.HousingCapacity, expectedFoodCapacity), settlement.CarryingCapacity, precision: 3);
    }

    [Fact]
    public void CarryingCapacity_IsZero_ForSettlementWithNoHousingOrFood()
    {
        var (world, _, system) = CreateHarness();
        var settlement = AddSettlement(world, members: 0, houses: 0, food: 0);

        system.Execute();

        Assert.Equal(0, settlement.CarryingCapacity);
    }

    [Fact]
    public void PublishesDecline_WhenPopulationCrossesAboveCapacity()
    {
        var (world, bus, system) = CreateHarness();
        // Housing (1 House) binds well below the food capacity (300/3.0=100).
        var settlement = AddSettlement(world, members: 1, houses: 1, food: 300);

        var declined = false;
        bus.Subscribe<PopulationDeclineEvent>(_ => declined = true);

        system.Execute(); // baseline: pressure comfortably under 1.0

        for (var i = 0; i < 20; i++) settlement.MemberIds.Add(GameEntityId.New());
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        Assert.True(declined);
    }

    [Fact]
    public void DeclineDoesNotRefire_WhileStillOverCapacity()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, members: 1, houses: 1, food: 300);

        var declineCount = 0;
        bus.Subscribe<PopulationDeclineEvent>(_ => declineCount++);

        system.Execute();
        for (var i = 0; i < 20; i++) settlement.MemberIds.Add(GameEntityId.New());
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();
        world.CurrentTime = SimulationTime.FromTick(24 * 60);
        system.Execute();

        Assert.Equal(1, declineCount);
    }

    [Fact]
    public void PublishesBoom_WhenPopulationGrows_WhileComfortablyUnderCapacity()
    {
        var (world, bus, system) = CreateHarness();
        // Ample housing (5 Houses) and food give a large, comfortable
        // carrying capacity relative to the starting population.
        var settlement = AddSettlement(world, members: 1, houses: 5, food: 3000);

        var boomed = false;
        bus.Subscribe<PopulationBoomEvent>(_ => boomed = true);

        system.Execute(); // baseline: already comfortable, but no growth yet this period

        settlement.MemberIds.Add(GameEntityId.New()); // real growth, still far under capacity
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        Assert.True(boomed);
    }

    [Fact]
    public void DoesNotPublishBoom_WhenCapacityRises_ButPopulationDoesNotGrow()
    {
        // Guards the specific false-positive RFC-008 calls out: a
        // settlement whose capacity rises (e.g. a new farm's food surplus)
        // without any real population growth should not be misreported as
        // a "boom" - population, not capacity, must actually increase.
        var (world, bus, system) = CreateHarness();
        // Food (15/3.0=5) binds below housing (5 Houses = 20) initially.
        var settlement = AddSettlement(world, members: 5, houses: 5, food: 15);

        var boomed = false;
        bus.Subscribe<PopulationBoomEvent>(_ => boomed = true);

        system.Execute(); // baseline: pressure = 5/5 = 1.0

        settlement.Storage.Add("Food", 3000); // food capacity balloons past housing's 20
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        Assert.False(boomed);
    }
}
