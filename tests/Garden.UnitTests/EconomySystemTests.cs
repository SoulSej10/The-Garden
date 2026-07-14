using Garden.Core.Events;
using Garden.Engine.Events;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// Week 26 leftover-consolidation sweep: EconomySystem.ProcessProduction has
/// tracked _totalGoodsCrafted since before this development cycle began, and
/// "GoodsCrafted" already sits in SignificanceEvaluator's always-Medium
/// whitelist, but GoodsCraftedEvent was never actually published anywhere -
/// HistorySystem's OnGoodsCrafted subscriber existed with nothing to ever
/// call it.
/// </summary>
public class EconomySystemTests
{
    private static (WorldState world, EventBus bus, EconomySystem system) CreateHarness()
    {
        var world = new WorldState();
        var bus = new EventBus();
        var system = new EconomySystem(world, bus, NullLogger<EconomySystem>.Instance);
        return (world, bus, system);
    }

    private static Settlement AddSettlementWithWorkshop(WorldState world, double wood = 10.0)
    {
        var settlement = new Settlement { Name = "Testford" };
        settlement.Buildings.Add(new Building { BuildingType = "Workshop", Status = BuildingStatus.Completed });
        settlement.Storage.Add("Wood", wood);
        world.Settlements.Add(settlement);
        return settlement;
    }

    [Fact]
    public void ProcessProduction_PublishesGoodsCraftedEvent_WhenWorkshopHasEnoughWood()
    {
        var (world, bus, system) = CreateHarness();
        AddSettlementWithWorkshop(world);

        GoodsCraftedEvent? captured = null;
        bus.Subscribe<GoodsCraftedEvent>(e => captured = e);

        system.Execute();

        Assert.NotNull(captured);
        Assert.Equal("Planks", captured!.Product);
        Assert.Equal(3, captured.Quantity);
    }

    [Fact]
    public void ProcessProduction_DoesNotPublish_WhenWoodIsInsufficient()
    {
        var (world, bus, system) = CreateHarness();
        AddSettlementWithWorkshop(world, wood: 2.0);

        var published = false;
        bus.Subscribe<GoodsCraftedEvent>(_ => published = true);

        system.Execute();

        Assert.False(published);
    }
}
