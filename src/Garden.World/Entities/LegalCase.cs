using Garden.Core.Identifiers;

namespace Garden.World.Entities;

// RFC-005 (specification/RFC/RFC-005-law-dispute-resolution.md): first
// increment of TG-590_Law_Justice.md - informal dispute resolution between
// two citizens in the same settlement, resolved by the settlement's
// existing leader, gated by its existing Legitimacy score. No formal
// institutions (courts, judges, juries) exist yet. LawSystem owns all
// updates to this state.
public class LegalCase
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public GameEntityId SettlementId { get; init; }
    public GameEntityId CitizenAId { get; init; }
    public GameEntityId CitizenBId { get; init; }
    public long OpenedTick { get; set; }
    public long? ResolvedTick { get; set; }
    public bool IsOpen { get; set; } = true;
    public bool WasResolvedFairly { get; set; }
}
