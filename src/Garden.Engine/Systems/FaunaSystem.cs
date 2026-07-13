using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.Core.World;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-012 (specification/RFC/RFC-012-fauna-aggregate-wildlife.md): first
/// increment of TG-230_Fauna_Animal_Behavior.md - a single aggregate
/// wildlife population per settlement, driven by Forest-tile habitat
/// within its territory. No individual animal agents, per TG-230's own
/// Performance Considerations ("Most animal populations should be
/// simulated using aggregate ecological models").
///
/// Monthly cadence (IntervalTicks = 24 * 30), matching
/// DecomposerSystem's/PopulationEcologySystem's granularity for
/// slow-moving ecological state.
/// </summary>
public class FaunaSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "FaunaSystem";
    public long IntervalTicks => 24 * 30;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-012: invented rates/thresholds (TG-230 gives no numbers).
    private const double HabitatCapacityPerForestTile = 2.0;
    private const double MonthlyMovementFraction = 0.2;
    private const double ExpansionThreshold = 2.0;
    private const double DieOffThreshold = 2.0;

    private readonly Dictionary<GameEntityId, double> _previousPopulation = new();

    public FaunaSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var settlement in _worldState.Settlements)
        {
            var habitatCapacity = CountForestTiles(settlement) * HabitatCapacityPerForestTile;
            var previousPopulation = _previousPopulation.GetValueOrDefault(settlement.Id, settlement.WildlifePopulation);

            settlement.WildlifePopulation = Math.Max(0,
                settlement.WildlifePopulation + (habitatCapacity - settlement.WildlifePopulation) * MonthlyMovementFraction);

            var delta = settlement.WildlifePopulation - previousPopulation;

            if (delta >= ExpansionThreshold && settlement.WildlifePopulation <= habitatCapacity)
            {
                _eventBus.Publish(new SpeciesExpandedEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name,
                    WildlifePopulation = settlement.WildlifePopulation
                });
            }
            else if (delta <= -DieOffThreshold)
            {
                _eventBus.Publish(new AnimalDiedEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name,
                    WildlifePopulation = settlement.WildlifePopulation
                });
            }

            _previousPopulation[settlement.Id] = settlement.WildlifePopulation;
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
