using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 3 Day 13: pairwise citizen Relationships per
/// TG-380_Relationships.md. A Relationship row is created lazily the first
/// time two citizens have a real, already-modeled interaction (trade, or
/// having a child together) - there is no relationship "background noise"
/// between citizens who have never actually interacted. Runs decay daily
/// (not every tick) since the relationship graph can grow to O(citizens^2)
/// and nothing about relationship fade needs hourly precision.
/// </summary>
public class RelationshipSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private long _nextExecutionTick;

    public string Name => "RelationshipSystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    // Half-lives in ticks for decay back toward neutral/distant baselines
    // when a pair isn't interacting - invented (TG-380 gives no numbers),
    // deliberately much slower than Emotion's half-lives since relationships
    // are framed as more durable than transient feelings.
    private const double TrustHalfLife = 2000;
    private const double AffectionHalfLife = 2000;
    private const double SocialDistanceHalfLife = 1000;

    public RelationshipSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;

        eventBus.Subscribe<TradeCompletedEvent>(OnTradeCompleted);
        eventBus.Subscribe<CitizenBornEvent>(OnCitizenBorn);
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var factorTrustAffection = Math.Pow(0.5, IntervalTicks / TrustHalfLife);
        var factorDistance = Math.Pow(0.5, IntervalTicks / SocialDistanceHalfLife);

        foreach (var rel in _worldState.Relationships)
        {
            rel.Trust = 50.0 + (rel.Trust - 50.0) * factorTrustAffection;
            rel.Affection = 50.0 + (rel.Affection - 50.0) * factorTrustAffection;
            // Distance drifts toward 100 (forgotten) without contact, not
            // toward 0 - the same "not interacting" reasoning as EmotionSystem's
            // Loneliness, just applied pairwise instead of per-citizen.
            rel.SocialDistance = 100.0 + (rel.SocialDistance - 100.0) * factorDistance;
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    /// <summary>
    /// Finds or creates the Relationship for a pair, canonically ordering
    /// the two ids (lower GUID first) so the same pair is never stored as
    /// both (A,B) and (B,A).
    /// </summary>
    public Relationship GetOrCreate(GameEntityId citizenA, GameEntityId citizenB, long tick)
    {
        var (a, b) = citizenA.Value.CompareTo(citizenB.Value) <= 0
            ? (citizenA, citizenB)
            : (citizenB, citizenA);

        var existing = _worldState.Relationships
            .FirstOrDefault(r => r.EntityAId == a && r.EntityBId == b);
        if (existing != null) return existing;

        var created = new Relationship
        {
            EntityAId = a,
            EntityBId = b,
            EstablishedTick = tick,
            LastInteractionTick = tick
        };
        _worldState.Relationships.Add(created);
        return created;
    }

    public void RecordInteraction(
        GameEntityId citizenA, GameEntityId citizenB, long tick,
        double trustDelta, double affectionDelta, double closenessDelta)
    {
        var rel = GetOrCreate(citizenA, citizenB, tick);
        rel.Trust = Math.Clamp(rel.Trust + trustDelta, 0.0, 100.0);
        rel.Affection = Math.Clamp(rel.Affection + affectionDelta, 0.0, 100.0);
        rel.SocialDistance = Math.Clamp(rel.SocialDistance - closenessDelta, 0.0, 100.0);
        rel.LastInteractionTick = tick;
        rel.InteractionCount++;
    }

    private void OnTradeCompleted(TradeCompletedEvent e)
    {
        RecordInteraction(e.FromCitizenId, e.ToCitizenId, e.Tick,
            trustDelta: 3.0, affectionDelta: 2.0, closenessDelta: 8.0);
    }

    private void OnCitizenBorn(CitizenBornEvent e)
    {
        // Having a child together is the strongest bonding interaction
        // modeled this increment - a much larger bump than a single trade.
        RecordInteraction(e.ParentAId, e.ParentBId, e.Tick,
            trustDelta: 15.0, affectionDelta: 20.0, closenessDelta: 25.0);
    }
}
