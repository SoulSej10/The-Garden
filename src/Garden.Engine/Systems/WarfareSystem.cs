using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-013 (specification/RFC/RFC-013-warfare-dispute-escalation.md): first
/// increment of TG-640_Warfare_Military_Organization.md - escalates an
/// already-detected TerritorySystem border dispute (RFC-007) between
/// settlements with a Hostile DiplomaticRelation into a real, resolvable
/// war. "Wars do not begin when armies move. They begin when trust fails."
///
/// Yearly cadence (IntervalTicks = SimulationTime.TicksPerYear), matching
/// the civilization-milestone systems' established cadence.
/// </summary>
public class WarfareSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly TerritorySystem _territorySystem;
    private long _nextExecutionTick;

    public string Name => "WarfareSystem";
    public long IntervalTicks => SimulationTime.TicksPerYear;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-013: invented rates/thresholds (TG-640 gives no numbers).
    private const double MilitaryStrengthPerPopulation = 2.0;
    private const double MilitaryStrengthPerLegitimacy = 0.5;
    private const double PopulationLossFraction = 0.1;
    private const double LegitimacyLossPerBattle = 10.0;
    private const int MaxBattlesBeforePeace = 5;
    private const int CriticalPopulationThreshold = 5;
    private const double PeaceRelationRestoreAmount = 15.0;
    private const double IntensityGrowthPerBattle = 20.0;

    public WarfareSystem(WorldState worldState, IEventBus eventBus, TerritorySystem territorySystem)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _territorySystem = territorySystem;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var settlement in _worldState.Settlements)
        {
            settlement.MilitaryStrength = settlement.Population * MilitaryStrengthPerPopulation
                + settlement.Legitimacy * MilitaryStrengthPerLegitimacy;
        }

        DeclareWars(tick);
        ResolveBattles(tick);

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void DeclareWars(long tick)
    {
        foreach (var (settlementAId, settlementBId) in _territorySystem.ActiveDisputes)
        {
            var relation = FindRelation(settlementAId, settlementBId);
            if (relation is not { CurrentRelation: RelationType.Hostile }) continue;

            var alreadyAtWar = _worldState.Wars.Any(w => w.IsActive && IsSamePair(w, settlementAId, settlementBId));
            if (alreadyAtWar) continue;

            var settlementA = _worldState.Settlements.FirstOrDefault(s => s.Id == settlementAId);
            var settlementB = _worldState.Settlements.FirstOrDefault(s => s.Id == settlementBId);
            if (settlementA == null || settlementB == null) continue;

            _worldState.Wars.Add(new War
            {
                SettlementAId = settlementAId,
                SettlementBId = settlementBId,
                StartedTick = tick
            });

            _eventBus.Publish(new WarDeclaredEvent
            {
                Tick = tick,
                SettlementAId = settlementAId,
                SettlementAName = settlementA.Name,
                SettlementBId = settlementBId,
                SettlementBName = settlementB.Name
            });
        }
    }

    private void ResolveBattles(long tick)
    {
        foreach (var war in _worldState.Wars.Where(w => w.IsActive))
        {
            var settlementA = _worldState.Settlements.FirstOrDefault(s => s.Id == war.SettlementAId);
            var settlementB = _worldState.Settlements.FirstOrDefault(s => s.Id == war.SettlementBId);
            if (settlementA == null || settlementB == null)
            {
                war.IsActive = false;
                war.EndedTick = tick;
                continue;
            }

            var totalStrength = settlementA.MilitaryStrength + settlementB.MilitaryStrength;
            var settlementAWins = totalStrength > 0
                ? System.Random.Shared.NextDouble() < settlementA.MilitaryStrength / totalStrength
                : System.Random.Shared.NextDouble() < 0.5;

            var winner = settlementAWins ? settlementA : settlementB;
            var loser = settlementAWins ? settlementB : settlementA;

            loser.Population = Math.Max(0, loser.Population - (int)Math.Round(loser.Population * PopulationLossFraction));
            loser.Legitimacy = Math.Max(0, loser.Legitimacy - LegitimacyLossPerBattle);

            war.BattlesFought++;
            war.Intensity += IntensityGrowthPerBattle;

            _eventBus.Publish(new BattleFoughtEvent
            {
                Tick = tick,
                WinnerId = winner.Id,
                WinnerName = winner.Name,
                LoserId = loser.Id,
                LoserName = loser.Name
            });

            if (loser.Population <= CriticalPopulationThreshold || war.BattlesFought >= MaxBattlesBeforePeace)
            {
                war.IsActive = false;
                war.EndedTick = tick;

                var relation = FindRelation(war.SettlementAId, war.SettlementBId);
                if (relation != null)
                    relation.RelationScore = Math.Min(100.0, relation.RelationScore + PeaceRelationRestoreAmount);

                _eventBus.Publish(new PeaceNegotiatedEvent
                {
                    Tick = tick,
                    SettlementAId = settlementA.Id,
                    SettlementAName = settlementA.Name,
                    SettlementBId = settlementB.Id,
                    SettlementBName = settlementB.Name
                });
            }
        }
    }

    private DiplomaticRelation? FindRelation(GameEntityId settlementAId, GameEntityId settlementBId) =>
        _worldState.DiplomaticRelations.FirstOrDefault(r =>
            (r.EntityAId == settlementAId && r.EntityBId == settlementBId) ||
            (r.EntityAId == settlementBId && r.EntityBId == settlementAId));

    private static bool IsSamePair(War war, GameEntityId settlementAId, GameEntityId settlementBId) =>
        (war.SettlementAId == settlementAId && war.SettlementBId == settlementBId) ||
        (war.SettlementAId == settlementBId && war.SettlementBId == settlementAId);
}
