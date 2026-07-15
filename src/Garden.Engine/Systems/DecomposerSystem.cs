using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.Core.World;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-011 (specification/RFC/RFC-011-decomposers-soil-health.md): first
/// increment of TG-220_Decomposers_Microbiology.md - a settlement-level
/// SoilHealth fed by the organic matter that already-existing
/// CitizenDied/ForestDeclined events produce, depleted by already-existing
/// FarmHarvested events, and fed back into AgricultureSystem's yield - the
/// first RFC in this series to write into an earlier system's formula
/// rather than staying strictly read-only, since TG-220 explicitly names
/// Agriculture as directly influenced by decomposition.
///
/// Monthly cadence (IntervalTicks = 24 * 30), matching
/// PopulationEcologySystem's granularity for slow-moving ecological state.
/// </summary>
public class DecomposerSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "DecomposerSystem";
    public long IntervalTicks => 24 * 30;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-011: invented rates/thresholds (TG-220 gives no numbers).
    private const double OrganicMatterPerDeath = 10.0;
    private const double OrganicMatterPerForestAreaLost = 5.0;
    // Growth rebalancing finding (100-year live test): raising
    // AgricultureSystem's yield multiplier (2.0 -> 3.5, to fix an
    // insufficient reproduction surplus) proportionally raised depletion
    // too, since depletion here is yield-scaled - completely swamping the
    // natural-regeneration fix added below. Worked through the full loop's
    // actual numbers this time instead of iterating by trial and error:
    // at a typical ~15-seed planting, 0.9 average seasonal modifier, and
    // ~30 harvests/month, the old 0.05 depleted roughly 14x faster than
    // BaselineSoilRegenPerMonth could ever offset, for ANY forest cover.
    // Lowered so a zero-forest settlement settles near a modest but real
    // ~40 equilibrium (not zero), and forest-rich settlements comfortably
    // reach full health rather than everything converging on near-zero.
    private const double SoilDepletionPerHarvestYield = 0.004;
    private const double DecompositionFraction = 0.3;
    private const double SoilHealthGainPerMatterDecomposed = 0.1;
    private const double NutrientPulseThreshold = 3.0;
    private const double BacklogAccumulationThreshold = 50.0;

    // Growth rebalancing finding: SoilHealth's only income was the
    // organic-matter-pending pipeline below, fed exclusively by
    // CitizenDied and ForestDeclined events - both rare, one-off events.
    // FarmHarvested (this system's own subscription) depletes SoilHealth
    // on every single harvest (daily, via AgricultureSystem), which
    // structurally outpaces that income - SoilHealth is a one-way ratchet
    // to near-zero within the first year or two of any settlement that
    // farms at all, and since AgricultureSystem's yield is itself
    // multiplied by SoilHealth/100, the farm then produces almost nothing
    // forever. Worse, fixing disease elsewhere in this project *reduced*
    // deaths, which starved this the only income source further. Real
    // soil doesn't need something to die every month to stay fertile -
    // fallow recovery, nitrogen fixation, and leaf litter from nearby
    // forest cover replenish it continuously. This adds that as a direct,
    // renewable monthly income independent of the death-triggered pipeline,
    // scaled by the settlement's actual forest cover so a well-balanced
    // forest (see EcologySystem's carrying-capacity fix) sustains
    // agriculture instead of only ever being depleted by it.
    private const double BaselineSoilRegenPerMonth = 2.5;
    private const double SoilRegenPerForestTile = 0.08;

    private readonly Dictionary<GameEntityId, double> _organicMatterPending = new();
    private readonly Dictionary<GameEntityId, double> _previousSoilHealth = new();
    private readonly Dictionary<GameEntityId, bool> _backlogAccumulating = new();

    public DecomposerSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;
        _eventBus = eventBus;

        eventBus.Subscribe<CitizenDiedEvent>(OnCitizenDied);
        eventBus.Subscribe<ForestDeclinedEvent>(OnForestDeclined);
        eventBus.Subscribe<FarmHarvestedEvent>(OnFarmHarvested);
    }

    private void OnCitizenDied(CitizenDiedEvent e)
    {
        var citizen = _worldState.Citizens.FirstOrDefault(c => c.Id == e.CitizenId);
        if (citizen?.HomeSettlementId == null) return;

        AddOrganicMatter(citizen.HomeSettlementId.Value, OrganicMatterPerDeath);
    }

    private void OnForestDeclined(ForestDeclinedEvent e)
    {
        var settlement = _worldState.Settlements.FirstOrDefault(s => s.IsWithinTerritory(e.TileX, e.TileY));
        if (settlement == null) return;

        AddOrganicMatter(settlement.Id, OrganicMatterPerForestAreaLost * e.AreaLost);
    }

    private void OnFarmHarvested(FarmHarvestedEvent e)
    {
        var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == e.SettlementId);
        if (settlement == null) return;

        settlement.SoilHealth = Math.Max(0, settlement.SoilHealth - e.Yield * SoilDepletionPerHarvestYield);
    }

    private void AddOrganicMatter(GameEntityId settlementId, double amount)
    {
        _organicMatterPending[settlementId] = _organicMatterPending.GetValueOrDefault(settlementId) + amount;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var settlement in _worldState.Settlements)
        {
            // Captured before this evaluation's own decomposition changes
            // SoilHealth, or the "previous" baseline would always equal the
            // post-decomposition value on a settlement's first-ever
            // evaluation, making a real rise undetectable.
            var previousSoilHealth = _previousSoilHealth.GetValueOrDefault(settlement.Id, settlement.SoilHealth);

            var pending = _organicMatterPending.GetValueOrDefault(settlement.Id);
            if (pending > 0)
            {
                var decomposed = pending * DecompositionFraction;
                _organicMatterPending[settlement.Id] = pending - decomposed;
                settlement.SoilHealth = Math.Min(100.0, settlement.SoilHealth + decomposed * SoilHealthGainPerMatterDecomposed);
            }

            var naturalRegen = BaselineSoilRegenPerMonth + CountForestTiles(settlement) * SoilRegenPerForestTile;
            settlement.SoilHealth = Math.Min(100.0, settlement.SoilHealth + naturalRegen);

            if (settlement.SoilHealth - previousSoilHealth >= NutrientPulseThreshold)
            {
                _eventBus.Publish(new NutrientPulseOccurredEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name,
                    SoilHealth = settlement.SoilHealth
                });
            }
            _previousSoilHealth[settlement.Id] = settlement.SoilHealth;

            var remainingPending = _organicMatterPending.GetValueOrDefault(settlement.Id);
            var isAccumulating = remainingPending >= BacklogAccumulationThreshold;
            var wasAccumulating = _backlogAccumulating.GetValueOrDefault(settlement.Id);

            if (isAccumulating && !wasAccumulating)
            {
                _eventBus.Publish(new OrganicMatterAccumulatedEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name
                });
            }
            else if (!isAccumulating && wasAccumulating)
            {
                _eventBus.Publish(new WasteFullyDecomposedEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name
                });
            }
            _backlogAccumulating[settlement.Id] = isAccumulating;
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private int CountForestTiles(Settlement settlement)
    {
        var count = 0;
        for (var x = settlement.TileX - settlement.TerritoryRadius; x <= settlement.TileX + settlement.TerritoryRadius; x++)
        {
            for (var y = settlement.TileY - settlement.TerritoryRadius; y <= settlement.TileY + settlement.TerritoryRadius; y++)
            {
                if (x < 0 || x >= _worldState.Map.Width || y < 0 || y >= _worldState.Map.Height) continue;
                if (_worldState.Map.GetTile(x, y).Terrain == TerrainType.Forest) count++;
            }
        }
        return count;
    }
}
