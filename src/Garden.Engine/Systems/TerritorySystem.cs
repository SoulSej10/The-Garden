using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-007 (specification/RFC/RFC-007-borders-territorial-influence.md):
/// first increment of TG-620_Borders_Territorial_Dynamics.md - a regional
/// influence field derived from existing Population/Legitimacy, replacing
/// the flat ever-growing Settlement.TerritoryRadius with something that can
/// also contract, plus detecting (not resolving) genuine territorial
/// disputes between settlements of comparable strength.
///
/// Yearly cadence (IntervalTicks = 336), matching the established
/// CivilizationSystem convention (the Week 6 Day 27 cadence-naming finding
/// applies here too, scheduled for its own fix in Week 12).
/// </summary>
public class TerritorySystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly SettlementManager _settlementManager;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "TerritorySystem";
    public long IntervalTicks => 336;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-007: invented thresholds/formula (TG-620 gives no numbers).
    private const double InfluenceChangeThreshold = 10.0;
    private const double DisputeInfluenceGapFraction = 0.20;

    private readonly Dictionary<GameEntityId, double> _previousInfluence = new();
    private readonly HashSet<(GameEntityId, GameEntityId)> _activeDisputes = [];

    /// <summary>Settlement pairs currently in an active border dispute, for Observatory surfacing.</summary>
    public IReadOnlyCollection<(GameEntityId SettlementAId, GameEntityId SettlementBId)> ActiveDisputes => _activeDisputes;

    public TerritorySystem(WorldState worldState, SettlementManager settlementManager, IEventBus eventBus)
    {
        _worldState = worldState;
        _settlementManager = settlementManager;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var settlement in _worldState.Settlements)
        {
            settlement.TerritorialInfluence = Math.Min(100.0,
                settlement.Population * 0.5 + settlement.Legitimacy * 0.3);

            var previous = _previousInfluence.GetValueOrDefault(settlement.Id, settlement.TerritorialInfluence);
            var change = settlement.TerritorialInfluence - previous;

            if (change > InfluenceChangeThreshold)
            {
                _settlementManager.ExpandTerritory(settlement);
            }
            else if (change < -InfluenceChangeThreshold && settlement.TerritoryRadius > 1)
            {
                settlement.TerritoryRadius--;
                _eventBus.Publish(new BorderContractedEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name,
                    NewTerritorySize = settlement.TerritoryRadius
                });
            }

            _previousInfluence[settlement.Id] = settlement.TerritorialInfluence;
        }

        DetectDisputes(tick);

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void DetectDisputes(long tick)
    {
        var settlements = _worldState.Settlements;
        var currentlyDisputing = new HashSet<(GameEntityId, GameEntityId)>();

        for (var i = 0; i < settlements.Count; i++)
        {
            for (var j = i + 1; j < settlements.Count; j++)
            {
                var a = settlements[i];
                var b = settlements[j];

                var dist = Math.Abs(a.TileX - b.TileX) + Math.Abs(a.TileY - b.TileY);
                var overlaps = dist <= a.TerritoryRadius + b.TerritoryRadius;
                if (!overlaps) continue;

                var maxInfluence = Math.Max(a.TerritorialInfluence, b.TerritorialInfluence);
                if (maxInfluence <= 0) continue;
                var gapFraction = Math.Abs(a.TerritorialInfluence - b.TerritorialInfluence) / maxInfluence;
                if (gapFraction > DisputeInfluenceGapFraction) continue;

                var (first, second) = a.Id.Value.CompareTo(b.Id.Value) <= 0 ? (a, b) : (b, a);
                var pairKey = (first.Id, second.Id);
                currentlyDisputing.Add(pairKey);

                if (_activeDisputes.Contains(pairKey)) continue;

                _eventBus.Publish(new BorderDisputeBeginsEvent
                {
                    Tick = tick,
                    SettlementAId = first.Id,
                    SettlementAName = first.Name,
                    SettlementBId = second.Id,
                    SettlementBName = second.Name
                });
            }
        }

        // A pair that no longer meets the dispute condition (overlap ended,
        // or one side's influence pulled clearly ahead) can trigger a fresh
        // BorderDisputeBeginsEvent if it starts disputing again later -
        // only a pair that's *still* disputing is suppressed from re-firing.
        _activeDisputes.Clear();
        foreach (var pair in currentlyDisputing) _activeDisputes.Add(pair);
    }
}
