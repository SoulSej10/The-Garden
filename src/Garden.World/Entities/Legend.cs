using Garden.Core.Identifiers;

namespace Garden.World.Entities;

// RFC-016 (specification/RFC/RFC-016-legends-myth-formation.md): first
// increment of TG-STRY-040_Legends_Myths.md - a High-importance
// HistoricalRecord, once old enough (Historical Distance), grows a
// distorted narrative alongside (never overwriting) the original record.
// Pure in-memory, following the Story entity's own shape - not
// EF-persisted. LegendSystem owns all updates.
public class Legend
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public GameEntityId SourceRecordId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string DistortedNarrative { get; init; } = string.Empty;
    public double LegendaryStatus { get; set; }
    public long FormedTick { get; init; }
}
