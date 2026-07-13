using Garden.Core.Identifiers;

namespace Garden.World.Entities;

// RFC-013 (specification/RFC/RFC-013-warfare-dispute-escalation.md): first
// increment of TG-640_Warfare_Military_Organization.md - an escalated
// territorial dispute (RFC-007) between two settlements with a Hostile
// DiplomaticRelation. Pure in-memory, following the LegalCase/Apprenticeship
// pattern (not EF-persisted). WarfareSystem owns all updates to this state.
public class War
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public GameEntityId SettlementAId { get; init; }
    public GameEntityId SettlementBId { get; init; }
    public long StartedTick { get; set; }
    public long? EndedTick { get; set; }
    public bool IsActive { get; set; } = true;
    public double Intensity { get; set; }
    public int BattlesFought { get; set; }
}
