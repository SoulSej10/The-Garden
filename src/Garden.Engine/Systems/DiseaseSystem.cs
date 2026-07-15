using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-009 (specification/RFC/RFC-009-disease-health-overcrowding.md): first
/// increment of TG-260_Disease_Health.md - overcrowding-driven infection,
/// applied to Citizen (the one population that already exists), reusing
/// RFC-008's CarryingCapacity/Population signal directly rather than
/// inventing a separate density concept.
///
/// Daily cadence (IntervalTicks = 24), matching the granularity of
/// individual Needs/Health changes rather than the settlement-level
/// monthly/yearly cadences.
/// </summary>
public class DiseaseSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "DiseaseSystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-009: invented rates/thresholds (TG-260 gives no numbers).
    private const double DailyInfectionChance = 0.02;
    private const double DailySeverityGrowth = 8.0;
    private const double HealthDamagePerSeverityPoint = 0.3;
    private const double BaseDailyRecoveryChance = 0.15;
    private const double EpidemicInfectionRateThreshold = 0.2;

    // Rebalancing audit finding 4/5/8: DiseaseResistance builds gradually on
    // recovery (a population "learning" to survive recurring disease) and
    // is read as a straight dampener on both future infection chance and
    // severity growth - deliberately capped well short of full immunity
    // (0.6 max reduction) so disease never becomes a non-threat outright.
    private const double DiseaseResistanceGainOnRecovery = 8.0;
    private const double MaxDiseaseResistance = 60.0;

    // A completed Healer building measurably improves a settlement's odds
    // against an ongoing infection - the minimum viable "primitive
    // healthcare" lever the audit found completely missing.
    private const double HealerRecoveryBonus = 0.12;

    private readonly Dictionary<GameEntityId, bool> _isEpidemic = new();

    public DiseaseSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var citizensById = _worldState.Citizens.Where(c => c.IsAlive).ToDictionary(c => c.Id);

        DetectOnset(tick, citizensById);
        ProgressInfections(tick, citizensById);
        DetectEpidemics();

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void DetectOnset(long tick, Dictionary<GameEntityId, Citizen> citizensById)
    {
        foreach (var settlement in _worldState.Settlements)
        {
            var members = settlement.MemberIds
                .Where(citizensById.ContainsKey)
                .Select(id => citizensById[id])
                .ToList();

            var population = members.Count;
            if (population == 0) continue;

            // Rebalancing audit finding 4/5/8: this used to read
            // settlement.CarryingCapacity, which PopulationEcologySystem
            // computes as Min(HousingCapacity, Food / 3.0) - during any
            // food shortage (the common case elsewhere in this project),
            // the food term was the binding, near-zero denominator, making
            // population/CarryingCapacity astronomically above the
            // overcrowding threshold regardless of actual physical
            // crowding. "Overcrowded" was a disguised food-scarcity flag,
            // not a density signal. HousingCapacity alone is the real
            // physical-crowding signal this check is meant to represent.
            var housingCapacity = settlement.HousingCapacity;
            var overcrowded = housingCapacity > 0
                ? population / (double)housingCapacity >= 1.0
                : true;

            if (!overcrowded) continue;

            // Food stress still matters to disease risk (malnourished
            // people really are more vulnerable) but is now a separate,
            // bounded multiplier rather than the entire gate - starvation
            // can raise risk up to 2x, never substitute for it.
            var avgHunger = members.Average(c => c.Needs.Hunger);
            var starvationMultiplier = 1.0 + Math.Clamp(avgHunger - 50.0, 0, 50) / 50.0;

            foreach (var citizen in members)
            {
                var alreadyInfected = _worldState.Infections.Any(i => i.IsActive && i.CitizenId == citizen.Id);
                if (alreadyInfected) continue;

                var resistanceFactor = 1.0 - Math.Min(citizen.DiseaseResistance, MaxDiseaseResistance) / 100.0;
                var infectionChance = DailyInfectionChance * starvationMultiplier * resistanceFactor;
                if (System.Random.Shared.NextDouble() >= infectionChance) continue;

                _worldState.Infections.Add(new Infection
                {
                    CitizenId = citizen.Id,
                    StartedTick = tick,
                    Severity = DailySeverityGrowth
                });

                _eventBus.Publish(new OrganismInfectedEvent
                {
                    Tick = tick,
                    CitizenId = citizen.Id,
                    CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name
                });
            }
        }
    }

    private void ProgressInfections(long tick, Dictionary<GameEntityId, Citizen> citizensById)
    {
        foreach (var infection in _worldState.Infections.Where(i => i.IsActive).ToList())
        {
            if (!citizensById.TryGetValue(infection.CitizenId, out var citizen))
            {
                infection.IsActive = false;
                continue;
            }

            var settlement = citizen.HomeSettlementId != null
                ? _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId)
                : null;
            var hasHealer = settlement?.Buildings.Any(b =>
                b.BuildingType == BuildingTypes.Healer && b.Status == BuildingStatus.Completed) ?? false;

            var recoveryChance = BaseDailyRecoveryChance * (citizen.Needs.Health / CitizenNeeds.MaxHealth)
                + (hasHealer ? HealerRecoveryBonus : 0);
            if (System.Random.Shared.NextDouble() < recoveryChance)
            {
                infection.IsActive = false;
                citizen.DiseaseResistance = Math.Min(MaxDiseaseResistance,
                    citizen.DiseaseResistance + DiseaseResistanceGainOnRecovery);

                _eventBus.Publish(new DiseaseRecoveredEvent
                {
                    Tick = tick,
                    CitizenId = citizen.Id,
                    CitizenName = $"{citizen.FirstName} {citizen.LastName}"
                });
                continue;
            }

            // Prior survivors accumulate resistance (see recovery branch
            // above) - a citizen fighting off a second or third infection
            // gets measurably better odds each time, the "civilization
            // adapts" mechanic the audit found completely missing.
            var severityResistance = 1.0 - Math.Min(citizen.DiseaseResistance, MaxDiseaseResistance) / 100.0;
            infection.Severity = Math.Min(Infection.MaxSeverity,
                infection.Severity + DailySeverityGrowth * severityResistance);
            citizen.Needs.Health = Math.Max(0, citizen.Needs.Health - infection.Severity * HealthDamagePerSeverityPoint / 100.0);

            if (infection.Severity >= Infection.MaxSeverity)
            {
                infection.IsActive = false;
                citizen.IsAlive = false;
                citizen.DeathTick = tick;
                citizen.CauseOfDeath = "Disease";

                _eventBus.Publish(new CitizenDiedEvent
                {
                    Tick = tick,
                    CitizenId = citizen.Id,
                    CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                    CauseOfDeath = "Disease",
                    AgeAtDeath = citizen.Age
                });
            }
        }
    }

    private void DetectEpidemics()
    {
        var activeByCitizen = _worldState.Infections
            .Where(i => i.IsActive)
            .Select(i => i.CitizenId)
            .ToHashSet();

        foreach (var settlement in _worldState.Settlements)
        {
            var population = settlement.MemberIds.Count;
            if (population == 0) continue;

            var infectionRate = settlement.MemberIds.Count(activeByCitizen.Contains) / (double)population;
            var isEpidemic = infectionRate >= EpidemicInfectionRateThreshold;
            var wasEpidemic = _isEpidemic.GetValueOrDefault(settlement.Id);

            if (isEpidemic && !wasEpidemic)
            {
                _eventBus.Publish(new EpidemicStartedEvent
                {
                    Tick = _worldState.CurrentTime.Tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name,
                    InfectionRate = infectionRate
                });
            }
            else if (!isEpidemic && wasEpidemic)
            {
                _eventBus.Publish(new EpidemicContainedEvent
                {
                    Tick = _worldState.CurrentTime.Tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name
                });
            }

            _isEpidemic[settlement.Id] = isEpidemic;
        }
    }
}
