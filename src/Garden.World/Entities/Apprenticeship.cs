using Garden.Core.Identifiers;

namespace Garden.World.Entities;

// RFC-004 (specification/RFC/RFC-004-education-apprenticeship.md): first
// increment of TG-550_Education.md - one mentor, one student, tracked via
// the existing Relationship graph (Week 3). No formal institutions
// (Schools/Libraries/Universities) exist yet - see RFC-004's "Deferred"
// table. EducationSystem owns all updates to this state.
public class Apprenticeship
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public GameEntityId MentorId { get; init; }
    public GameEntityId StudentId { get; init; }
    public long StartedTick { get; set; }
    public long? CompletedTick { get; set; }
    public bool IsActive { get; set; } = true;
}
