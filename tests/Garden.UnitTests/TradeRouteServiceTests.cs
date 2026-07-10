using Garden.Core.Identifiers;
using Garden.Engine.Events;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// Investigation for the bug flagged during Week 6 Day 29 live verification
/// (task_b82147bd): EvaluateTradeRoutes() never created any route in a live
/// 8-settlement world, even with a settlement pair 23 tiles apart (under the
/// 25-tile cutoff) with one settlement holding 74 Food and the other 0
/// (satisfying FindTradeGood's `aQty >= 20 && bQty < 10` rule).
/// </summary>
public class TradeRouteServiceTests
{
    private static (WorldState world, TradeRouteService service) CreateHarness()
    {
        var world = new WorldState();
        var eventBus = new EventBus();
        var service = new TradeRouteService(world, eventBus, NullLogger<TradeRouteService>.Instance);
        return (world, service);
    }

    private static Settlement AddSettlement(WorldState world, string name, int tileX, int tileY, int memberCount, string good, double quantity)
    {
        var settlement = new Settlement { Name = name, TileX = tileX, TileY = tileY };
        for (var i = 0; i < memberCount; i++)
            settlement.MemberIds.Add(GameEntityId.New());
        settlement.Storage.Add(good, quantity);
        world.Settlements.Add(settlement);
        return settlement;
    }

    [Fact]
    public void CreatesRoute_ForTheExactLiveScenario_74Food_Vs_0Food_23TilesApart()
    {
        var (world, service) = CreateHarness();
        // Reproduces the exact live conditions: Upperridge (26 members,
        // 72,37, Food=74) and Newdale (15 members, 81,23, Food=0).
        var upperridge = AddSettlement(world, "Upperridge", 72, 37, 26, "Food", 74);
        AddSettlement(world, "Newdale", 81, 23, 15, "Food", 0);

        service.EvaluateTradeRoutes(tick: 7);

        Assert.Single(world.TradeRoutes);
    }

    [Fact]
    public void OnceAbandoned_APair_NeverGetsANewRoute_EvenWithFreshSurplus()
    {
        // Root cause confirmed: once a route exists for a pair (active or
        // not), EvaluateTradeRoutes' `existing != null` check short-circuits
        // before ever calling FindTradeGood again - so a pair that goes
        // quiet for 168+ ticks is permanently locked out of trading again,
        // even if a fresh surplus/scarcity appears later. This is what
        // actually happened live: an early route between two settlements
        // depleted, went inactive, and then Food=74-vs-0 conditions later
        // couldn't create a *new* evaluation for that same pair.
        var (world, service) = CreateHarness();
        var a = AddSettlement(world, "A", 0, 0, 5, "Wood", 20);
        var b = AddSettlement(world, "B", 10, 0, 5, "Wood", 0);

        service.EvaluateTradeRoutes(tick: 0);
        Assert.Single(world.TradeRoutes);
        var route = world.TradeRoutes.Single();

        // Deplete the source good with no replacement surplus yet, and let
        // the route go stale past the 168-tick inactivity window - this
        // evaluation should only abandon, since no good currently qualifies.
        a.Storage.Remove("Wood", a.Storage.GetQuantity("Wood"));
        service.EvaluateTradeRoutes(tick: 200); // > 168 past LastTripTick
        Assert.False(route.IsActive);

        // A fresh, unrelated surplus (Food) now appears - a perfectly good
        // trade opportunity on its own, for the very same settlement pair.
        a.Storage.Add("Food", 100);
        service.EvaluateTradeRoutes(tick: 207);

        Assert.True(route.IsActive,
            "Expected the pair to reactivate/re-evaluate once a fresh surplus (Food) appeared, but it stayed abandoned - confirms the pair is permanently skipped after its first route is abandoned.");
    }

    [Fact]
    public void GoodsFlow_FromWhicheverSettlementActuallyHasTheSurplus_NotAlwaysTheFirstArgument()
    {
        // Second bug found alongside the reactivation fix: FindTradeGood
        // matches either (aQty >= 20 && bQty < 10) or the reverse, but
        // trip execution used to always move goods a -> b regardless of
        // which one actually held the surplus. Here `b` (second settlement)
        // is the one with the surplus - goods must flow from b to a.
        var (world, service) = CreateHarness();
        var a = AddSettlement(world, "A", 0, 0, 5, "Wood", 0);
        var b = AddSettlement(world, "B", 10, 0, 5, "Wood", 50);

        service.EvaluateTradeRoutes(tick: 0);

        var route = Assert.Single(world.TradeRoutes);
        Assert.Equal(b.Id, route.FromSettlementId);
        Assert.Equal(a.Id, route.ToSettlementId);
        Assert.True(a.Storage.GetQuantity("Wood") > 0,
            "Expected Wood to flow into A (the settlement with none), but A still has none");
        Assert.True(b.Storage.GetQuantity("Wood") < 50,
            "Expected Wood to flow out of B (the settlement with the surplus), but B's stock is unchanged");
    }
}
