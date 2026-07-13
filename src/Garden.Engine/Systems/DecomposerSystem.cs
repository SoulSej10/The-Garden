using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
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
    private const double SoilDepletionPerHarvestYield = 0.05;
    private const double DecompositionFraction = 0.3;
    private const double SoilHealthGainPerMatterDecomposed = 0.1;
    private const double NutrientPulseThreshold = 3.0;
    private const double BacklogAccumulationThreshold = 50.0;

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
}
