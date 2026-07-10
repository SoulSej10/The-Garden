using Garden.Core.Identifiers;

namespace Garden.World.Entities;

// RFC-003 (specification/RFC/RFC-003-language-divergence.md): first
// increment of TG-510_Language.md - pairwise settlement-level language
// drift, distinct from Relationship (citizen-level) and DiplomaticRelation
// (settlement-level but tracks political standing, not language). No named
// Language entity exists yet - see RFC-003's "Why no named Language entity
// yet" section. LanguageSystem owns all updates to this state.
public class LanguageDivergence
{
    public GameEntityId Id { get; init; } = GameEntityId.New();

    // Canonically ordered by LanguageSystem (lower GUID first), same
    // pattern as Relationship.EntityAId/EntityBId.
    public GameEntityId SettlementAId { get; init; }
    public GameEntityId SettlementBId { get; init; }

    // 0 = mutually intelligible, 100 = a distinct dialect has formed.
    // Starts at 0 - settlements are assumed to share a common tongue at
    // first contact, since nothing in this codebase models multiple
    // starting language groups. Only ever grows from isolation or shrinks
    // from contact, never reset outright (TG-510 frames this as gradual
    // generational drift, not sudden change).
    public double Divergence { get; set; }

    public bool DialectFormed { get; set; }
    public long EstablishedTick { get; set; }
    public long LastEvaluatedTick { get; set; }
}
