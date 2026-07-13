using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-003 (specification/RFC/RFC-003-language-divergence.md): first
/// increment of TG-510_Language.md - pairwise settlement-level language
/// drift, driven by whether two settlements have real ongoing contact (an
/// active TradeRoute or a positive DiplomaticRelation) or none. No named
/// Language entity yet - see RFC-003's "Why no named Language entity yet".
///
/// Yearly cadence (IntervalTicks = SimulationTime.TicksPerYear), matching
/// TechnologyService/ReligionService/KingdomService's own cadence in
/// CivilizationSystem - TG-510 explicitly says language should change
/// "gradually over generations rather than daily".
/// </summary>
public class LanguageSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "LanguageSystem";
    public long IntervalTicks => SimulationTime.TicksPerYear;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-003: invented thresholds/rates (TG-510 gives no numbers).
    // Divergence grows at 2x the rate contact reverses it - RFC-003's
    // recommendation that isolation is the "downhill" default direction and
    // convergence is something contact has to actively fight against.
    private const double DialectThreshold = 70.0;
    private const double ConvergenceRatePerYear = 3.0;
    private const double DivergenceRatePerYear = 6.0;
    // RFC-003 open question 2: loosely mirrors DiplomacyService's band
    // shape without literally importing it - settlement-level diplomacy
    // bands are a different mechanic.
    private const double ContactRelationScoreThreshold = 50.0;

    public LanguageSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        // A row only exists for settlement pairs that have ever had real
        // contact - mirrors RelationshipSystem.GetOrCreate's "no background
        // noise between parties that never interacted" rule, just at
        // settlement scale.
        foreach (var route in _worldState.TradeRoutes.Where(r => r.IsActive))
            GetOrCreate(route.FromSettlementId, route.ToSettlementId, tick);
        foreach (var relation in _worldState.DiplomaticRelations
                     .Where(r => r.EntityAIsSettlement && r.EntityBIsSettlement))
            GetOrCreate(relation.EntityAId, relation.EntityBId, tick);

        var settlementsById = _worldState.Settlements.ToDictionary(s => s.Id);

        foreach (var divergence in _worldState.LanguageDivergences)
        {
            var hasContact = HasActiveContact(divergence.SettlementAId, divergence.SettlementBId);

            divergence.Divergence = hasContact
                ? Math.Max(0.0, divergence.Divergence - ConvergenceRatePerYear)
                : Math.Min(100.0, divergence.Divergence + DivergenceRatePerYear);
            divergence.LastEvaluatedTick = tick;

            if (!divergence.DialectFormed && divergence.Divergence >= DialectThreshold)
            {
                divergence.DialectFormed = true;

                var aName = settlementsById.TryGetValue(divergence.SettlementAId, out var a) ? a.Name : "Unknown";
                var bName = settlementsById.TryGetValue(divergence.SettlementBId, out var b) ? b.Name : "Unknown";

                _eventBus.Publish(new DialectFormedEvent
                {
                    Tick = tick,
                    SettlementAId = divergence.SettlementAId,
                    SettlementAName = aName,
                    SettlementBId = divergence.SettlementBId,
                    SettlementBName = bName,
                    Divergence = divergence.Divergence
                });
            }
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private bool HasActiveContact(GameEntityId settlementA, GameEntityId settlementB)
    {
        var hasTradeRoute = _worldState.TradeRoutes.Any(r => r.IsActive &&
            ((r.FromSettlementId == settlementA && r.ToSettlementId == settlementB) ||
             (r.FromSettlementId == settlementB && r.ToSettlementId == settlementA)));
        if (hasTradeRoute) return true;

        var relation = _worldState.DiplomaticRelations.FirstOrDefault(r =>
            (r.EntityAId == settlementA && r.EntityBId == settlementB) ||
            (r.EntityAId == settlementB && r.EntityBId == settlementA));
        return relation != null && relation.RelationScore > ContactRelationScoreThreshold;
    }

    /// <summary>
    /// Finds or creates the LanguageDivergence for a settlement pair,
    /// canonically ordering the two ids (lower GUID first) so the same pair
    /// is never stored as both (A,B) and (B,A).
    /// </summary>
    public LanguageDivergence GetOrCreate(GameEntityId settlementA, GameEntityId settlementB, long tick)
    {
        var (a, b) = settlementA.Value.CompareTo(settlementB.Value) <= 0
            ? (settlementA, settlementB)
            : (settlementB, settlementA);

        var existing = _worldState.LanguageDivergences
            .FirstOrDefault(d => d.SettlementAId == a && d.SettlementBId == b);
        if (existing != null) return existing;

        var created = new LanguageDivergence
        {
            SettlementAId = a,
            SettlementBId = b,
            EstablishedTick = tick,
            LastEvaluatedTick = tick
        };
        _worldState.LanguageDivergences.Add(created);
        return created;
    }
}
