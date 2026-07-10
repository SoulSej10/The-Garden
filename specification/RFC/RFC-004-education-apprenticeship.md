# RFC-004: Education — Apprenticeship (First Increment)

**Status:** Proposed
**Date:** 2026-07-10
**Author:** DEVELOPMENT_PLAN.md (Week 7 close-out backlog triage)
**Governing spec:** `03_Sciences/04_Social/TG-550_Education.md`

---

## Why this needs an RFC before a day-to-day plan

Same reasoning as RFC-001/002/003: TG-550 is thorough on vocabulary (Educational
Institutions, Literacy Rate, Knowledge Accessibility, Teacher/Student Population,
Curriculum, Educational Equality, Innovation Capacity) and names 10 events
(`ChildTaught`, `ApprenticeshipStarted`/`Completed`, `SchoolFounded`, `LibraryBuilt`,
`TeacherAppointed`, `DiscoveryPublished`, `CurriculumExpanded`, `LiteracyIncreased`,
`KnowledgeLost`) but gives no formula, no data type, and no starting condition. `Education`
has **zero code footprint** anywhere in `src/` (confirmed by grep). This RFC picks one
small, real, buildable slice rather than attempting TG-550 in full.

## Why Education, and why now - and why not wait for its other dependencies

TG-550 lists six dependencies: `TG-500` Communication, `TG-510` Language, `TG-520` Groups,
`TG-530` Culture, `TG-540` Social Norms, and Volume V Cognitive Sciences. Communication and
Language shipped Weeks 5-6. Groups and Social Norms still have **zero code footprint each**,
same as Education did before this RFC - waiting for either would mean waiting on two more
full RFC cycles with no end in sight. RFC-002 and RFC-003 already established the working
precedent: build a real, testable first increment against whatever infrastructure already
exists (`Relationship`, `EmotionalState`, `CitizenAttributes`), and explicitly defer the
concepts that require infrastructure this codebase doesn't have yet. Education's own spec
supports this - "Learning within society progresses through observation, mentorship,
apprenticeship, instruction..." lists apprenticeship as the *first*, most informal form, not
requiring institutions (Groups) or codified norms (Social Norms) to exist first.

## Scope decision: informal apprenticeship only, two events

TG-550 spans informal mentorship all the way up to universities, literacy, curricula, and
educational inequality. Building all of it in one increment would repeat the mistake
RFC-001 explicitly avoided. First increment covers exactly one thing: **a citizen with high
`Attributes.Intelligence` gradually raising a close relationship's Intelligence toward their
own**, via the existing `Relationship` graph (Week 3) - the same "reuse the existing social
graph as the transmission channel" approach RFC-002 used for Communication and RFC-003 used
for Language.

| In scope | Deferred (needs its own RFC later) |
|---|---|
| `Apprenticeship` entity: one mentor, one student, tracked via the existing `Relationship` pair | Formal institutions (Schools, Libraries, Universities, Academies) - each needs a `Building`-like concept this RFC doesn't add |
| `ApprenticeshipStarted`/`ApprenticeshipCompleted` events (2 of TG-550's 10) | `ChildTaught`, `SchoolFounded`, `LibraryBuilt`, `TeacherAppointed`, `DiscoveryPublished`, `CurriculumExpanded`, `LiteracyIncreased`, `KnowledgeLost` - each needs state (Curriculum, Literacy, institutions) this RFC doesn't add |
| Intelligence transfer from mentor to student, gated by an existing close `Relationship` (mirrors RFC-002/003's contact-gating pattern) | Literacy Rate, Knowledge Accessibility, Teacher/Student Population as tracked aggregates - none of these have a real trigger yet |
| A minimal Observatory surfacing: a citizen's active apprenticeship (mentor or student role), if any | Educational Inequality, Curriculum, professional licensing - all require state this increment doesn't model |

## Why Intelligence, specifically

`CitizenAttributes.Intelligence` is the only state in this codebase that plausibly
represents "what a citizen has learned" - it already gates `TechnologyService`'s
discoverer selection (`OrderByDescending(c => c.Attributes.Intelligence)`) and
`ReligionService`'s founder selection. Raising it via apprenticeship is a real, observable,
testable mechanism consistent with TG-550's own framing ("well-educated societies innovate
more rapidly"). Notably, `TechnologyService.CalculateProgress` already has a vestigial,
always-zero `intelligenceFactor` local variable - a dead stub for exactly this kind of
input that was apparently anticipated but never wired up. This RFC does **not** wire it in
(that would mean touching `TechnologyService` a third time this session for a change outside
this RFC's own scope) - it's noted here as the natural next integration point for whoever
picks up "Education actually affects Technology" as a future increment.

## Data model

```csharp
// New file: Garden.World/Entities/Apprenticeship.cs
public class Apprenticeship
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public GameEntityId MentorId { get; init; }
    public GameEntityId StudentId { get; init; }
    public long StartedTick { get; set; }
    public long? CompletedTick { get; set; }
    public bool IsActive { get; set; } = true;
}
```

Not EF-persisted - follows the same pure in-memory `WorldState` list pattern as
`Relationship`/`LanguageDivergence` (only `Citizen` and `Settlement` are actually
EF-persisted in this codebase).

## Mechanism

A new `EducationSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, yearly cadence
(`IntervalTicks = 336`, matching `TechnologyService`/`ReligionService`/`KingdomService`/
`LanguageSystem`'s existing convention in `CivilizationSystem` - Week 6 Day 27 already
flagged that this cadence is actually ~14 days, not a year, but this RFC follows the
established convention rather than fixing that separately-tracked backlog item here).

1. For every `Relationship` with `SocialDistance < 40` (RFC-002/003's convergence
   threshold, reused for consistency) where one citizen's `Intelligence` is at least 2.0
   higher than the other's, and no `Apprenticeship` already exists for that ordered pair,
   create one with the higher-Intelligence citizen as mentor. Publish
   `ApprenticeshipStartedEvent`.
2. Each yearly evaluation, an active apprenticeship raises the student's `Intelligence` by
   a fixed rate (invented - no TG-550 number given) toward the mentor's, never exceeding it.
3. When the student's `Intelligence` comes within 0.5 of the mentor's, or the pair's
   `Relationship` decays past the threshold (the bond that enabled teaching no longer
   exists), the apprenticeship completes/ends. Publish `ApprenticeshipCompletedEvent`.

## Explicitly out of scope for the next cycle

- Any institution (School/Library/University/Academy) - all require a `Building`-like
  concept and a settlement-level "where teaching happens" decision this RFC doesn't make.
- Literacy, Curriculum, Educational Inequality, and the aggregate population-level state
  variables TG-550 names - none have a real trigger anywhere in this codebase yet.
- Wiring the mentor/student Intelligence transfer into `TechnologyService`'s
  `intelligenceFactor` stub - noted as the obvious next step, not done here.
- Multiple simultaneous students per mentor, or re-apprenticing after completion - this
  increment models one mentor/student pair at a time, matching RFC-002/003's practice of
  picking the smallest real mechanic rather than the most general one.

## Open questions for review before implementation starts

1. Is a flat "+2.0 Intelligence gap" threshold the right gate for mentor/student pairing,
   or should it scale with age/life stage (e.g. only Adults/Elders can mentor, only
   Children/Teens can be students)? (Recommendation: gate by life stage as well as the
   Intelligence gap - TG-550's own "Apprenticeship" section frames this as an early-life
   activity ("progressive responsibility"), and an Elder mentoring another Elder doesn't
   match the spec's own framing of generational knowledge transfer.)
2. Should `SocialDistance < 40` reuse RFC-002/003's exact threshold, or does teaching
   require closer contact than casual information diffusion? (Recommendation: reuse it
   for consistency, per RFC-002/003's own established position that per-system invented
   thresholds are expected and a shared value across systems isn't required - but flag
   this as worth revisiting if apprenticeships turn out to form too rarely or too often in
   live verification.)
