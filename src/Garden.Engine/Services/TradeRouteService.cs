using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class TradeRouteService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<TradeRouteService> _logger;

    private static readonly string[] TradeGoods = ["Food", "Wood", "Stone", "Planks", "Clay", "Tools"];

    public TradeRouteService(WorldState worldState, IEventBus eventBus, ILogger<TradeRouteService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void EvaluateTradeRoutes(long tick)
    {
        var settlements = _worldState.Settlements
            .Where(s => s.MemberIds.Count >= 2)
            .ToList();

        for (var i = 0; i < settlements.Count; i++)
        {
            for (var j = i + 1; j < settlements.Count; j++)
            {
                var a = settlements[i];
                var b = settlements[j];
                var dist = Math.Abs(a.TileX - b.TileX) + Math.Abs(a.TileY - b.TileY);
                if (dist > 25) continue;

                var existing = _worldState.TradeRoutes
                    .FirstOrDefault(r =>
                        (r.FromSettlementId == a.Id && r.ToSettlementId == b.Id) ||
                        (r.FromSettlementId == b.Id && r.ToSettlementId == a.Id));

                if (existing != null)
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
                    continue;
                }

                var good = FindTradeGood(a, b);
                if (good == null) continue;

                EstablishRoute(a, b, good, dist, tick);
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

    private void EstablishRoute(Settlement a, Settlement b, string good, double distance, long tick)
    {
        var route = new TradeRoute
        {
            FromSettlementId = a.Id,
            FromSettlementName = a.Name,
            ToSettlementId = b.Id,
            ToSettlementName = b.Name,
            PrimaryGood = good,
            Distance = distance,
            EstablishedTick = tick,
            LastTripTick = tick,
            EconomicValue = Math.Max(1, 10 - distance * 0.3)
        };

        _worldState.TradeRoutes.Add(route);
        ExecuteTrip(route, a, b, good, tick);

        var fromName = distance <= 15 ? a.Name : b.Name;
        var toName = distance <= 15 ? b.Name : a.Name;

        _eventBus.Publish(new TradeRouteEstablishedEvent
        {
            Tick = tick,
            RouteId = route.Id,
            FromSettlementId = route.FromSettlementId,
            FromSettlementName = fromName,
            ToSettlementId = route.ToSettlementId,
            ToSettlementName = toName,
            PrimaryGood = good,
            Distance = distance
        });

        _logger.LogInformation("Trade route established: {Good} between {A} and {B}",
            good, fromName, toName);
    }

    private static void ExecuteTrip(TradeRoute route, Settlement from, Settlement to, string good, long tick)
    {
        var available = from.Storage.GetQuantity(good);
        if (available < 5) return;

        var amount = Math.Min(10, available);
        from.Storage.Remove(good, amount);
        to.Storage.Add(good, amount);

        route.TotalVolumeTransported += amount;
        route.TripCount++;
        route.LastTripTick = tick;
    }
}
