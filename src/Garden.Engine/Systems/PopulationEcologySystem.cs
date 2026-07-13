using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-008 (specification/RFC/RFC-008-population-ecology-carrying-capacity.md):
/// first increment of TG-240_Population_Ecology.md - applies TG-240's
/// carrying-capacity concept to the one population that already exists in
/// this codebase (Settlement.MemberIds), rather than inventing a wildlife
/// population system. Detects (does not add a second consequence to)
/// crossings into/out of sustainable capacity.
///
/// Monthly cadence (IntervalTicks = 24 * 30) - population dynamics are
/// observable on a monthly timescale, unlike the civilization-milestone
/// systems' yearly cadence (SimulationTime.TicksPerYear, Week 12 Day 58).
/// </summary>
public class PopulationEcologySystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "PopulationEcologySystem";
    public long IntervalTicks => 24 * 30;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-008: invented thresholds (TG-240 gives no numbers). FoodPerCapita
    // reuses ReproductionSystem's existing reproduction-safety bar verbatim.
    private const double FoodPerCapitaThreshold = 3.0;
    private const double BoomPressureThreshold = 0.5;

    private readonly Dictionary<GameEntityId, double> _previousPressure = new();
    private readonly Dictionary<GameEntityId, int> _previousPopulation = new();
    private readonly Dictionary<GameEntityId, bool> _wasGrowingComfortably = new();

    public PopulationEcologySystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var settlement in _worldState.Settlements)
        {
            var housingCapacity = settlement.HousingCapacity;
            var foodCapacity = settlement.Storage.GetQuantity("Food") / FoodPerCapitaThreshold;
            var carryingCapacity = Math.Min(housingCapacity, foodCapacity);
            settlement.CarryingCapacity = carryingCapacity;

            var population = settlement.MemberIds.Count;
            var pressure = carryingCapacity > 0
                ? population / carryingCapacity
                : population > 0 ? double.PositiveInfinity : 0.0;

            var previousPressure = _previousPressure.GetValueOrDefault(settlement.Id, pressure);
            var previousPopulation = _previousPopulation.GetValueOrDefault(settlement.Id, population);

            // "Growing comfortably" is a state (real growth AND ample
            // headroom), not a threshold crossing on its own - growth
            // itself pushes pressure up, so a pure "pressure fell below X"
            // trigger can never coincide with population actually rising.
            // The event fires on the transition into that state instead.
            var isGrowingComfortably = pressure <= BoomPressureThreshold && population > previousPopulation;
            var wasGrowingComfortably = _wasGrowingComfortably.GetValueOrDefault(settlement.Id, isGrowingComfortably);

            if (pressure >= 1.0 && previousPressure < 1.0)
            {
                _eventBus.Publish(new PopulationDeclineEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name,
                    Population = population,
                    CarryingCapacity = carryingCapacity
                });
            }
            else if (isGrowingComfortably && !wasGrowingComfortably)
            {
                _eventBus.Publish(new PopulationBoomEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name,
                    Population = population,
                    CarryingCapacity = carryingCapacity
                });
            }

            _previousPressure[settlement.Id] = pressure;
            _previousPopulation[settlement.Id] = population;
            _wasGrowingComfortably[settlement.Id] = isGrowingComfortably;
        }

        _nextExecutionTick = tick + IntervalTicks;
    }
}
