using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class TradeRouteService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<TradeRouteService> _logger;
    private readonly EconomySystem _economySystem;

    private static readonly string[] TradeGoods = ["Food", "Wood", "Stone", "Planks", "Clay", "Tools"];

    public TradeRouteService(WorldState worldState, IEventBus eventBus, ILogger<TradeRouteService> logger, EconomySystem economySystem)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
        _economySystem = economySystem;
    }

    public void EvaluateTradeRoutes(long tick)
    {
        var settlements = _worldState.Settlements
            .Where(s => s.MemberIds.Count >= 2)
            .ToList();

        // A flat 25-tile cutoff was sized for the old 100x100 world, where
        // settlements naturally spawned close together. On the current
        // 256x256 (or larger, config-driven - see World:Width/Height in
        // appsettings.json) world, SpawnSystem's minimum-separation rule
        // spreads settlements far enough apart that almost no pair ever
        // fell within 25 tiles, so trade routes silently never formed
        // despite every other part of this system working correctly.
        // Scaling the radius to a fraction of the map's diagonal keeps the
        // "nearby settlements trade" premise intact at any world size.
        var mapDiagonal = Math.Sqrt(Math.Pow(_worldState.Map.Width, 2) + Math.Pow(_worldState.Map.Height, 2));
        var tradeRadius = Math.Max(25, mapDiagonal * 0.35);

        for (var i = 0; i < settlements.Count; i++)
        {
            for (var j = i + 1; j < settlements.Count; j++)
            {
                var a = settlements[i];
                var b = settlements[j];
                var dist = Math.Abs(a.TileX - b.TileX) + Math.Abs(a.TileY - b.TileY);
                if (dist > tradeRadius) continue;

                var existing = _worldState.TradeRoutes
                    .FirstOrDefault(r =>
                        (r.FromSettlementId == a.Id && r.ToSettlementId == b.Id) ||
                        (r.FromSettlementId == b.Id && r.ToSettlementId == a.Id));

                if (existing is { IsActive: true })
                {
                    if (tick - existing.LastTripTick > 168)
                    {
                        existing.IsActive = false;

                        _eventBus.Publish(new TradeRouteAbandonedEvent
                        {
                            Tick = tick,
                            RouteId = existing.Id,
                            FromSettlementName = existing.FromSettlementName,
                            ToSettlementName = existing.ToSettlementName,
                            Reason = "Inactivity"
                        });

                        _logger.LogDebug("Trade route between {A} and {B} abandoned due to inactivity",
                            existing.FromSettlementName, existing.ToSettlementName);
                    }
                    else
                    {
                        continue;
                    }
                }

                // Bug found during Week 6 Day 29 live verification
                // (task_b82147bd): this used to `continue` unconditionally
                // whenever `existing != null`, regardless of IsActive - so a
                // pair whose route went quiet once (a common, ordinary
                // occurrence as local surpluses come and go) was locked out
                // of trading forever, even when a fresh surplus/scarcity
                // later made the pair exactly as trade-worthy as any other.
                // Falling through here lets an inactive route reactivate.
                var good = FindTradeGood(a, b);
                if (good == null) continue;

                // Bug found alongside the reactivation fix above: the
                // surplus-holder isn't necessarily `a` - FindTradeGood
                // matches either direction (`aQty >= 20 && bQty < 10` OR the
                // reverse), but goods must always flow from whichever
                // settlement actually has the surplus.
                var (from, to) = a.Storage.GetQuantity(good) >= 20 ? (a, b) : (b, a);

                if (existing != null)
                    ReactivateRoute(existing, from, to, good, tick);
                else
                    EstablishRoute(from, to, good, dist, tick);
            }
        }
    }

    private static string? FindTradeGood(Settlement a, Settlement b)
    {
        foreach (var good in TradeGoods)
        {
            var aQty = a.Storage.GetQuantity(good);
            var bQty = b.Storage.GetQuantity(good);
            if (aQty >= 20 && bQty < 10) return good;
            if (bQty >= 20 && aQty < 10) return good;
        }
        return null;
    }

    private void EstablishRoute(Settlement from, Settlement to, string good, double distance, long tick)
    {
        var route = new TradeRoute
        {
            FromSettlementId = from.Id,
            FromSettlementName = from.Name,
            ToSettlementId = to.Id,
            ToSettlementName = to.Name,
            PrimaryGood = good,
            Distance = distance,
            EstablishedTick = tick,
            LastTripTick = tick,
            EconomicValue = Math.Max(1, 10 - distance * 0.3)
        };

        _worldState.TradeRoutes.Add(route);
        ExecuteTrip(route, from, to, good, tick);

        _eventBus.Publish(new TradeRouteEstablishedEvent
        {
            Tick = tick,
            RouteId = route.Id,
            FromSettlementId = route.FromSettlementId,
            FromSettlementName = from.Name,
            ToSettlementId = route.ToSettlementId,
            ToSettlementName = to.Name,
            PrimaryGood = good,
            Distance = distance
        });

        _logger.LogInformation("Trade route established: {Good} between {A} and {B}",
            good, from.Name, to.Name);
    }

    /// <summary>
    /// Reactivates a route that previously went inactive, rather than
    /// leaving that settlement pair permanently locked out of trading (see
    /// EvaluateTradeRoutes for the full context).
    /// </summary>
    private void ReactivateRoute(TradeRoute route, Settlement from, Settlement to, string good, long tick)
    {
        route.IsActive = true;
        route.PrimaryGood = good;
        route.LastTripTick = tick;
        ExecuteTrip(route, from, to, good, tick);

        _eventBus.Publish(new TradeRouteEstablishedEvent
        {
            Tick = tick,
            RouteId = route.Id,
            FromSettlementId = from.Id,
            FromSettlementName = from.Name,
            ToSettlementId = to.Id,
            ToSettlementName = to.Name,
            PrimaryGood = good,
            Distance = route.Distance
        });

        _logger.LogInformation("Trade route reactivated: {Good} between {A} and {B}",
            good, from.Name, to.Name);
    }

    private void ExecuteTrip(TradeRoute route, Settlement from, Settlement to, string good, long tick)
    {
        var available = from.Storage.GetQuantity(good);
        if (available < 5) return;

        // RFC-014 (specification/RFC/RFC-014-infrastructure-route-quality.md):
        // a well-developed route moves up to 2x the base amount at maximum
        // InfrastructureQuality - InfrastructureSystem owns that field,
        // this is its one read of it.
        var baseAmount = Math.Min(10, available);
        var amount = Math.Min(available, baseAmount * (1.0 + route.InfrastructureQuality / 100.0));
        from.Storage.Remove(good, amount);
        to.Storage.Add(good, amount);

        route.TotalVolumeTransported += amount;
        route.TripCount++;
        route.LastTripTick = tick;

        // The route itself already tracked TripCount/TotalVolumeTransported,
        // but nothing ever incremented EconomySystem.TotalTrades - the one
        // counter the frontend's Overview/Almanac "Trades" stat actually
        // reads - so trade could be happening correctly and still always
        // display as 0.
        _economySystem.RecordTrade();
    }
}
