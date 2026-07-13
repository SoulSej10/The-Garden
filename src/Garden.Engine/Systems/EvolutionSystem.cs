using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-010 (specification/RFC/RFC-010-evolution-adaptive-drift.md): first
/// increment of TG-250_Evolution_Adaptation.md - observes the
/// population-level attribute drift that ReproductionSystem's
/// inheritance-with-variance and CitizenSystem's differential survival
/// already produce, rather than adding a second selection mechanic.
/// "Individuals do not evolve. Populations evolve over many generations."
///
/// Yearly cadence (IntervalTicks = SimulationTime.TicksPerYear), matching
/// TG-250's requirement that meaningful adaptation spans multiple
/// generations, not days.
/// </summary>
public class EvolutionSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "EvolutionSystem";
    public long IntervalTicks => SimulationTime.TicksPerYear;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-010: invented thresholds (TG-250 gives no numbers).
    private const double ShiftThreshold = 0.5;
    private const int StagnantYearsBeforeEvent = 3;

    private readonly Dictionary<(GameEntityId, string), double> _previousAverages = new();
    private readonly Dictionary<GameEntityId, int> _stagnantYears = new();
    private readonly Dictionary<GameEntityId, bool> _stagnationReported = new();

    public EvolutionSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var settlement in _worldState.Settlements)
        {
            var members = settlement.MemberIds
                .Select(id => _worldState.Citizens.FirstOrDefault(c => c.Id == id))
                .Where(c => c != null && c.IsAlive)
                .Select(c => c!)
                .ToList();

            if (members.Count == 0) continue;

            var hasBaseline = _previousAverages.ContainsKey((settlement.Id, "Strength"));

            var averages = new Dictionary<string, double>
            {
                ["Strength"] = members.Average(c => c.Attributes.Strength),
                ["Endurance"] = members.Average(c => c.Attributes.Endurance),
                ["Intelligence"] = members.Average(c => c.Attributes.Intelligence),
                ["Dexterity"] = members.Average(c => c.Attributes.Dexterity),
                ["Perception"] = members.Average(c => c.Attributes.Perception)
            };

            var anyShift = false;

            foreach (var (attributeName, average) in averages)
            {
                var key = (settlement.Id, attributeName);
                if (_previousAverages.TryGetValue(key, out var previous))
                {
                    var delta = average - previous;
                    if (Math.Abs(delta) >= ShiftThreshold)
                    {
                        anyShift = true;
                        _eventBus.Publish(new AdaptiveShiftObservedEvent
                        {
                            Tick = tick,
                            SettlementId = settlement.Id,
                            SettlementName = settlement.Name,
                            AttributeName = attributeName,
                            Delta = delta
                        });
                    }
                }

                _previousAverages[key] = average;
            }

            // The very first evaluation for a settlement has no baseline to
            // compare against - it establishes one, silently, the same
            // "no event fires on the first Execute()" convention
            // TerritorySystem/PopulationEcologySystem both already use.
            if (!hasBaseline) continue;

            if (anyShift)
            {
                _stagnantYears[settlement.Id] = 0;
                _stagnationReported[settlement.Id] = false;
            }
            else
            {
                var years = _stagnantYears.GetValueOrDefault(settlement.Id) + 1;
                _stagnantYears[settlement.Id] = years;

                if (years >= StagnantYearsBeforeEvent && !_stagnationReported.GetValueOrDefault(settlement.Id))
                {
                    _eventBus.Publish(new EvolutionaryStagnationEvent
                    {
                        Tick = tick,
                        SettlementId = settlement.Id,
                        SettlementName = settlement.Name
                    });
                    _stagnationReported[settlement.Id] = true;
                }
            }
        }

        _nextExecutionTick = tick + IntervalTicks;
    }
}
