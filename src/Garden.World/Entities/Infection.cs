using Garden.Core.Identifiers;

namespace Garden.World.Entities;

// RFC-009 (specification/RFC/RFC-009-disease-health-overcrowding.md): first
// increment of TG-260_Disease_Health.md - overcrowding-driven infection on
// citizens, the one population that already exists in this codebase. Pure
// in-memory, following the same LegalCase/Apprenticeship pattern (not
// EF-persisted). DiseaseSystem owns all updates to this state.
public class Infection
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public GameEntityId CitizenId { get; init; }
    public long StartedTick { get; set; }
    public double Severity { get; set; }
    public bool IsActive { get; set; } = true;

    public const double MaxSeverity = 100.0;
}
