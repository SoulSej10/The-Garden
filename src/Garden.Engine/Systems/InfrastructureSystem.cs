using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.World.Collections;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-014 (specification/RFC/RFC-014-infrastructure-route-quality.md):
/// first increment of TG-660_Infrastructure.md - grows/decays an already-
/// existing TradeRoute's InfrastructureQuality based on sustained use or
/// neglect, rather than rewriting Building/ConstructionSystem into a
/// network model (ADR-003 - TG-660's "network, not isolated structures"
/// framing is satisfied by extending TradeRoute, the connection that
/// already exists between settlements).
///
/// Monthly cadence (IntervalTicks = 24 * 30), matching DecomposerSystem's/
/// FaunaSystem's granularity for slow-moving, usage-driven state.
/// </summary>
public class InfrastructureSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "InfrastructureSystem";
    public long IntervalTicks => 24 * 30;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-014: invented rates/thresholds (TG-660 gives no numbers).
    private const double QualityGainPerTrip = 2.0;
    private const double QualityDecayPerMonthWhenInactive = 5.0;
    private const double RoadWorthyThreshold = 50.0;
    private const double FootpathThreshold = 10.0;

    private readonly Dictionary<GameEntityId, int> _previousTripCount = new();
    // Hysteresis: a route becomes "road" at RoadWorthyThreshold (50) and
    // only reverts to "not road" if it falls all the way to
    // FootpathThreshold (10), not merely below 50 again - the same
    // "avoid flapping right at one threshold" reasoning behind having two
    // separate invented thresholds rather than one.
    private readonly Dictionary<GameEntityId, bool> _isRoad = new();

    public InfrastructureSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var route in _worldState.TradeRoutes)
        {
            var previousTripCount = _previousTripCount.GetValueOrDefault(route.Id, route.TripCount);
            var tripsGained = route.TripCount - previousTripCount;

            if (route.IsActive && tripsGained > 0)
            {
                route.InfrastructureQuality = Math.Min(100.0, route.InfrastructureQuality + tripsGained * QualityGainPerTrip);
            }
            else if (!route.IsActive)
            {
                route.InfrastructureQuality = Math.Max(0.0, route.InfrastructureQuality - QualityDecayPerMonthWhenInactive);
            }

            var wasRoad = _isRoad.GetValueOrDefault(route.Id);

            if (!wasRoad && route.InfrastructureQuality >= RoadWorthyThreshold)
            {
                _eventBus.Publish(new RoadConstructedEvent
                {
                    Tick = tick,
                    RouteId = route.Id,
                    FromSettlementId = route.FromSettlementId,
                    FromSettlementName = route.FromSettlementName,
                    ToSettlementId = route.ToSettlementId,
                    ToSettlementName = route.ToSettlementName
                });
                _isRoad[route.Id] = true;
            }
            else if (wasRoad && route.InfrastructureQuality < FootpathThreshold)
            {
                _eventBus.Publish(new InfrastructureFailureEvent
                {
                    Tick = tick,
                    RouteId = route.Id,
                    FromSettlementId = route.FromSettlementId,
                    FromSettlementName = route.FromSettlementName,
                    ToSettlementId = route.ToSettlementId,
                    ToSettlementName = route.ToSettlementName
                });
                _isRoad[route.Id] = false;
            }

            _previousTripCount[route.Id] = route.TripCount;
        }

        _nextExecutionTick = tick + IntervalTicks;
    }
}
