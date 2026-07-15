using Garden.Core.Events;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// ADR-002 (specification/ADR/ADR-002-agriculture-system-crop-growth-disposition.md,
/// Week 13 Day 63): AgricultureSystem existed with zero dedicated unit tests
/// prior to this - only ever exercised indirectly through
/// SurvivalSimulationTests's multi-year integration runs. Direct coverage
/// added as a contained gap-fill, not a design change - ADR-002 explicitly
/// chose to keep the existing formula, not alter it.
/// </summary>
public class AgricultureSystemTests
{
    private static (WorldState world, EventBus bus, AgricultureSystem system) CreateHarness(long tick = 0)
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(tick) };
        world.Map.Initialize(1, 1);
        world.Map.SetTile(0, 0, new WorldTile { X = 0, Y = 0, Moisture = 0.6 });
        var bus = new EventBus();
        var system = new AgricultureSystem(world, bus, NullLogger<AgricultureSystem>.Instance);
        return (world, bus, system);
    }

    private static Settlement AddSettlementWithFarm(WorldState world, double seeds, double moisture = 0.6)
    {
        world.Map.GetTile(0, 0).Moisture = moisture;

        var farm = new Building
        {
            BuildingType = BuildingTypes.Farm,
            Status = BuildingStatus.Completed,
            TileX = 0,
            TileY = 0
        };
        farm.Storage.Add("Seeds", seeds);

        var settlement = new Settlement { Name = "Rivermoot", TileX = 0, TileY = 0 };
        settlement.Buildings.Add(farm);
        world.Settlements.Add(settlement);
        return settlement;
    }

    [Fact]
    public void ProcessFarm_DoesNothing_WhenNoSeedsPlanted()
    {
        var (world, _, system) = CreateHarness();
        AddSettlementWithFarm(world, seeds: 0);

        system.Execute();

        Assert.Equal(0, world.Settlements[0].Storage.GetQuantity("Food"));
    }

    [Theory]
    [InlineData(0, 1.5)]   // Spring
    [InlineData(3, 1.2)]   // Summer (month 4)
    [InlineData(6, 0.8)]   // Autumn (month 7)
    [InlineData(9, 0.1)]   // Winter (month 10)
    public void ProcessFarm_ScalesYield_BySeasonModifier(int monthOffset, double expectedModifier)
    {
        var tick = monthOffset * 24L * 30;
        var (world, _, system) = CreateHarness(tick);
        AddSettlementWithFarm(world, seeds: 10, moisture: 0.6);

        system.Execute();

        var expectedYield = 10 * expectedModifier * 3.5;
        Assert.Equal(expectedYield, world.Settlements[0].Storage.GetQuantity("Food"), precision: 3);
    }

    [Fact]
    public void ProcessFarm_HalvesEffectiveSeeds_WhenMoistureIsLow()
    {
        var (world, _, system) = CreateHarness(); // tick 0 = Spring, modifier 1.5
        AddSettlementWithFarm(world, seeds: 10, moisture: 0.1);

        system.Execute();

        // 10 seeds * 0.5 (low moisture) * 1.5 (Spring) * 3.5 = 26.25
        Assert.Equal(26.25, world.Settlements[0].Storage.GetQuantity("Food"), precision: 3);
    }

    [Fact]
    public void ProcessFarm_ConsumesEightyPercentOfPlantedSeeds()
    {
        var (world, _, system) = CreateHarness();
        AddSettlementWithFarm(world, seeds: 10, moisture: 0.6);

        system.Execute();

        Assert.Equal(2.0, world.Settlements[0].Buildings[0].Storage.GetQuantity("Seeds"), precision: 3);
    }

    [Fact]
    public void ProcessFarm_PublishesFarmHarvestedEvent_WithRealYield()
    {
        var (world, bus, system) = CreateHarness();
        AddSettlementWithFarm(world, seeds: 10, moisture: 0.6);

        FarmHarvestedEvent? published = null;
        bus.Subscribe<FarmHarvestedEvent>(e => published = e);

        system.Execute();

        Assert.NotNull(published);
        Assert.Equal("Rivermoot", published!.SettlementName);
        Assert.Equal(52.5, published.Yield, precision: 3); // 10 * 1.5 * 3.5
    }

    [Fact]
    public void ProcessFarm_IgnoresIncompleteOrOtherBuildings()
    {
        var (world, _, system) = CreateHarness();
        var settlement = new Settlement { Name = "Oldfield", TileX = 0, TileY = 0 };
        settlement.Buildings.Add(new Building
        {
            BuildingType = BuildingTypes.Farm,
            Status = BuildingStatus.UnderConstruction,
            TileX = 0,
            TileY = 0
        });
        settlement.Buildings.Add(new Building
        {
            BuildingType = BuildingTypes.Well,
            Status = BuildingStatus.Completed,
            TileX = 0,
            TileY = 0
        });
        world.Settlements.Add(settlement);

        system.Execute();

        Assert.Equal(0, settlement.Storage.GetQuantity("Food"));
    }
}
