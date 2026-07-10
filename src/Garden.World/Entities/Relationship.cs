using Garden.Core.Identifiers;

namespace Garden.World.Entities;

// TG-380_Relationships.md (DEVELOPMENT_PLAN.md Week 3 Day 13): a pairwise
// citizen-to-citizen relationship, distinct from EmotionalState.Trust (a
// citizen's general disposition, see RFC-001 open question 2) and from the
// existing global Reputation scalar (a single settlement-wide number, not
// pair-specific). TG-380 names Trust, Affection Profile, Reciprocity
// Balance, Social Distance, and Conflict History as relationship state, but
// gives no formulas - only Trust, Affection, and SocialDistance are
// implemented this increment (the three with a real, already-available
// trigger via RelationshipSystem); Reciprocity Balance and Conflict History
// are deferred until a system that actually tracks favors/conflicts exists.
public class Relationship
{
    public GameEntityId Id { get; init; } = GameEntityId.New();

    // Canonically ordered by RelationshipSystem (lower GUID first) so a pair
    // is never stored twice as both (A,B) and (B,A).
    public GameEntityId EntityAId { get; init; }
    public GameEntityId EntityBId { get; init; }

    public double Trust { get; set; } = 50.0;
    public double Affection { get; set; } = 50.0;

    // Lower = closer. 0 = inseparable, 100 = strangers. Starts at a neutral
    // "just met" midpoint rather than 100, since a Relationship row is only
    // ever created in response to a real interaction (see
    // RelationshipSystem.GetOrCreate) - two citizens who have never
    // interacted simply have no Relationship row at all.
    public double SocialDistance { get; set; } = 50.0;

    public long EstablishedTick { get; set; }
    public long LastInteractionTick { get; set; }
    public int InteractionCount { get; set; }
}
