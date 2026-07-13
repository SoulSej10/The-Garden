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
            var overcrowded = population > 0 && settlement.CarryingCapacity > 0
                ? population / settlement.CarryingCapacity >= 1.0
                : population > 0 && settlement.CarryingCapacity <= 0;

            if (!overcrowded) continue;

            foreach (var citizen in members)
            {
                var alreadyInfected = _worldState.Infections.Any(i => i.IsActive && i.CitizenId == citizen.Id);
                if (alreadyInfected) continue;
                if (System.Random.Shared.NextDouble() >= DailyInfectionChance) continue;

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

            var recoveryChance = BaseDailyRecoveryChance * (citizen.Needs.Health / CitizenNeeds.MaxHealth);
            if (System.Random.Shared.NextDouble() < recoveryChance)
            {
                infection.IsActive = false;
                _eventBus.Publish(new DiseaseRecoveredEvent
                {
                    Tick = tick,
                    CitizenId = citizen.Id,
                    CitizenName = $"{citizen.FirstName} {citizen.LastName}"
                });
                continue;
            }

            infection.Severity = Math.Min(Infection.MaxSeverity, infection.Severity + DailySeverityGrowth);
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
