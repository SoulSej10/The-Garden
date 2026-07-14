using Garden.Core.Identifiers;

namespace Garden.World.Entities;

// RFC-015 (specification/RFC/RFC-015-technology-independent-discovery.md):
// replaces the single shared Technology.CurrentProgress/IsDiscovered pair
// per named technology with one row per (SettlementId, TechnologyName),
// so settlements can discover technologies independently of one another
// (ADR-004 - the old shared state made Independent Discovery/Parallel
// Inventions/Technological Divergence structurally impossible, not just
// unmodeled). Pure in-memory, following the LegalCase/Apprenticeship/War
// pattern - not EF-persisted. TechnologyService owns all updates.
public class SettlementTechnology
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public GameEntityId SettlementId { get; init; }
    public string TechnologyName { get; init; } = string.Empty;
    public double CurrentProgress { get; set; }
    public bool IsDiscovered { get; set; }
    public long? DiscoveredTick { get; set; }
    public GameEntityId? DiscoveredByCitizenId { get; set; }
    public string DiscoveredByCitizenName { get; set; } = string.Empty;
}
