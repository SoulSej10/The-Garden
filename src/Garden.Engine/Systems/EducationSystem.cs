using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Systems;

/// <summary>
/// RFC-004 (specification/RFC/RFC-004-education-apprenticeship.md): first
/// increment of TG-550_Education.md - a citizen with high Attributes.
/// Intelligence gradually raising a close Relationship's Intelligence
/// toward their own, modeling informal apprenticeship. No formal
/// institutions (Schools/Libraries/Universities) exist yet.
///
/// Yearly cadence (IntervalTicks = 336), matching TechnologyService/
/// ReligionService/KingdomService/LanguageSystem's existing convention in
/// CivilizationSystem (Week 6 Day 27 already flagged this cadence is
/// actually ~14 days, not a year - a separately tracked backlog item, not
/// re-litigated here).
/// </summary>
public class EducationSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private long _nextExecutionTick;

    public string Name => "EducationSystem";
    public long IntervalTicks => 336;
    public long NextExecutionTick => _nextExecutionTick;

    // RFC-004: invented thresholds (TG-550 gives no numbers). Reuses
    // RFC-002/003's SocialDistance < 40 convergence threshold for
    // consistency, per RFC-004 open question 2's recommendation.
    private const double ContactSocialDistanceThreshold = 40.0;
    private const double MinIntelligenceGap = 2.0;
    private const double IntelligenceTransferPerYear = 0.5;
    private const double CompletionGap = 0.5;

    public EducationSystem(WorldState worldState, IEventBus eventBus)
    {
        _worldState = worldState;
        _eventBus = eventBus;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var citizensById = _worldState.Citizens
            .Where(c => c.IsAlive)
            .ToDictionary(c => c.Id);

        ProgressActiveApprenticeships(tick, citizensById);
        FormNewApprenticeships(tick, citizensById);

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void ProgressActiveApprenticeships(long tick, Dictionary<GameEntityId, Citizen> citizensById)
    {
        foreach (var apprenticeship in _worldState.Apprenticeships.Where(a => a.IsActive))
        {
            if (!citizensById.TryGetValue(apprenticeship.MentorId, out var mentor) ||
                !citizensById.TryGetValue(apprenticeship.StudentId, out var student))
            {
                Complete(apprenticeship, tick, citizensById);
                continue;
            }

            var gap = mentor.Attributes.Intelligence - student.Attributes.Intelligence;
            if (gap <= CompletionGap || !HasContact(mentor.Id, student.Id))
            {
                Complete(apprenticeship, tick, citizensById);
                continue;
            }

            student.Attributes.Intelligence = Math.Min(
                mentor.Attributes.Intelligence,
                student.Attributes.Intelligence + IntelligenceTransferPerYear);
        }
    }

    private void FormNewApprenticeships(long tick, Dictionary<GameEntityId, Citizen> citizensById)
    {
        foreach (var rel in _worldState.Relationships)
        {
            if (rel.SocialDistance >= ContactSocialDistanceThreshold) continue;
            if (!citizensById.TryGetValue(rel.EntityAId, out var a)) continue;
            if (!citizensById.TryGetValue(rel.EntityBId, out var b)) continue;

            TryPair(a, b, tick);
            TryPair(b, a, tick);
        }
    }

    private void TryPair(Citizen candidateMentor, Citizen candidateStudent, long tick)
    {
        // RFC-004 open question 1, resolved: gated by life stage as well as
        // the Intelligence gap - TG-550 frames apprenticeship as an
        // early-life activity ("progressive responsibility"), not a
        // general mechanic between any two citizens.
        if (candidateMentor.Stage is not (LifeStage.Adult or LifeStage.Elder)) return;
        if (candidateStudent.Stage is not (LifeStage.Child or LifeStage.Teen)) return;

        var gap = candidateMentor.Attributes.Intelligence - candidateStudent.Attributes.Intelligence;
        if (gap < MinIntelligenceGap) return;

        var alreadyExists = _worldState.Apprenticeships.Any(a =>
            a.IsActive && a.MentorId == candidateMentor.Id && a.StudentId == candidateStudent.Id);
        if (alreadyExists) return;

        var apprenticeship = new Apprenticeship
        {
            MentorId = candidateMentor.Id,
            StudentId = candidateStudent.Id,
            StartedTick = tick
        };
        _worldState.Apprenticeships.Add(apprenticeship);

        _eventBus.Publish(new ApprenticeshipStartedEvent
        {
            Tick = tick,
            MentorId = candidateMentor.Id,
            MentorName = $"{candidateMentor.FirstName} {candidateMentor.LastName}",
            StudentId = candidateStudent.Id,
            StudentName = $"{candidateStudent.FirstName} {candidateStudent.LastName}"
        });
    }

    private bool HasContact(GameEntityId a, GameEntityId b)
    {
        var rel = _worldState.Relationships.FirstOrDefault(r =>
            (r.EntityAId == a && r.EntityBId == b) || (r.EntityAId == b && r.EntityBId == a));
        return rel != null && rel.SocialDistance < ContactSocialDistanceThreshold;
    }

    private void Complete(Apprenticeship apprenticeship, long tick, Dictionary<GameEntityId, Citizen> citizensById)
    {
        apprenticeship.IsActive = false;
        apprenticeship.CompletedTick = tick;

        var mentorName = citizensById.TryGetValue(apprenticeship.MentorId, out var mentor)
            ? $"{mentor.FirstName} {mentor.LastName}" : "Unknown";
        var studentName = citizensById.TryGetValue(apprenticeship.StudentId, out var student)
            ? $"{student.FirstName} {student.LastName}" : "Unknown";

        _eventBus.Publish(new ApprenticeshipCompletedEvent
        {
            Tick = tick,
            MentorId = apprenticeship.MentorId,
            MentorName = mentorName,
            StudentId = apprenticeship.StudentId,
            StudentName = studentName
        });
    }
}
