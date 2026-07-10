# RFC-005: Law & Justice — Dispute Resolution (First Increment)

**Status:** Proposed
**Date:** 2026-07-10
**Author:** DEVELOPMENT_PLAN.md (Week 8 close-out backlog triage)
**Governing spec:** `03_Sciences/04_Social/TG-590_Law_Justice.md`

---

## Why this needs an RFC before a day-to-day plan

Same reasoning as RFC-001 through RFC-004: TG-590 is thorough on vocabulary (Legal Code,
Judicial Institutions, Rights Framework, Crime Definitions, Dispute Resolution Capacity,
Judicial Independence) and names 10 events (`LawCreated`, `LawReformed`, `CourtEstablished`,
`CaseResolved`, `LegalPrecedentSet`, `RightsExpanded`, `CorruptionExposed`,
`ConstitutionWritten`, `JudicialReform`, `JusticeFailure`) but gives no formula, no crime
taxonomy, and no starting condition. `Law & Justice` has **zero real code footprint**
anywhere in `src/` (confirmed by grep - the one match is a comment in
`GovernanceService.cs` explicitly noting it's unimplemented). This RFC picks one small,
real, buildable slice rather than attempting TG-590 in full.

## Why Law & Justice, and why now - and why not wait for Social Norms

TG-590 lists six dependencies: `TG-530` Culture, `TG-540` Social Norms, `TG-550` Education,
`TG-560` Religion, `TG-570` Economics, `TG-580` Politics & Governance, plus Volume V
Cognitive Sciences. Five of six already have real code (`CultureService`, `ReligionService`,
`EconomySystem`, `GovernanceService`, and now `EducationSystem` as of Week 8) - only Social
Norms has zero footprint, same position Groups was in for Education (RFC-004). Following
RFC-002 through RFC-004's established precedent, this RFC does not wait for it.

## Scope decision: informal dispute resolution via the existing settlement leader

TG-590 spans customary law all the way to constitutions, courts, judges, juries, and
international law. Building all of it in one increment would repeat the mistake RFC-001
explicitly avoided. First increment covers exactly one thing: **a citizen-pair dispute
(a `Relationship` with very low Trust) getting resolved by the settlement's existing
leader**, with the outcome gated by the settlement's existing `Legitimacy` score
(`GovernanceService`, Week 2 Day 9) - no courts, no written law, no crime taxonomy. This is
directly the spec's own framing: "Initially, communities rely upon customs and shared
norms... conflicts may be resolved through negotiation, mediation, community councils" -
informal resolution by an existing authority figure is the *first* rung of TG-590's own
ladder, before formal institutions appear.

| In scope | Deferred (needs its own RFC later) |
|---|---|
| A `LegalCase` entity: a citizen pair, a settlement, opened/resolved ticks, outcome | Any formal institution (`CourtEstablished`, judges, juries, appeal systems) - each needs a `Building`-like or role concept this RFC doesn't add |
| `CaseResolvedEvent`/`JusticeFailureEvent` (2 of TG-590's 10 named events) | `LawCreated`/`LawReformed`/`LegalPrecedentSet`/`ConstitutionWritten`/`JudicialReform` - each requires a written Legal Code this RFC doesn't model |
| Resolution chance gated by the settlement's existing `Legitimacy` score (reused, not re-invented) | `RightsExpanded`, `CorruptionExposed` - both require a Rights Framework/corruption-tracking concept that doesn't exist yet |
| A minimal Observatory surfacing: a settlement's open/resolved case count | Crime taxonomy (violence/theft/fraud/etc.), restorative vs. punitive outcome types, Judicial Independence as tracked state |

## Why the settlement leader, and why Legitimacy as the resolution input

`Settlement.LeaderId`/`LeaderName` and `Settlement.Legitimacy` (Week 2 Day 9,
`GovernanceService`) already exist and are exactly what TG-590 asks an informal dispute
resolver to have: standing in the community and legitimacy to be trusted with a decision.
Reusing them - rather than inventing a new "judge" concept - is consistent with RFC-002/003/
004's practice of attaching new systems to existing state wherever a real, adequate hook
already exists. This RFC does **not** write back into `Legitimacy` (that would mean
touching `GovernanceService` for a change outside this RFC's scope, and Legitimacy already
has a documented formula from Week 2 Day 9 that this RFC shouldn't casually extend) - it
only *reads* Legitimacy as an input, the same one-directional relationship Education (RFC-
004) established with `TechnologyService`'s `intelligenceFactor` stub.

## Data model

```csharp
// New file: Garden.World/Entities/LegalCase.cs
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
```

Not EF-persisted - follows the same pure in-memory `WorldState` list pattern as
`Relationship`/`LanguageDivergence`/`Apprenticeship` (only `Citizen` and `Settlement` are
actually EF-persisted in this codebase).

## Mechanism

A new `LawSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, yearly cadence
(`IntervalTicks = 336`, matching `TechnologyService`/`ReligionService`/`KingdomService`/
`LanguageSystem`/`EducationSystem`'s existing convention in `CivilizationSystem` - the
Week 6 Day 27 cadence-naming finding applies here too and isn't re-litigated).

1. For every `Relationship` with `Trust < 20` (an invented threshold - "actively hostile,"
   distinct from RFC-002/003/004's `SocialDistance`-based contact gate) where both citizens
   share the same `HomeSettlementId` and no open `LegalCase` already exists for that pair,
   open one.
2. Each yearly evaluation, an open case has a `settlement.Legitimacy / 100.0` chance
   (reusing the existing 0-100 field directly as a probability - the higher a settlement's
   legitimacy, the more likely its leader resolves disputes fairly) of resolving. On
   success: `WasResolvedFairly = true`, partially restore the pair's `Relationship.Trust`
   (a real, observable "justice was served" outcome), publish `CaseResolvedEvent`.
3. If a case remains open past 3 yearly evaluations without resolving, it closes unresolved
   (`WasResolvedFairly = false`) and publishes `JusticeFailureEvent` instead - TG-590 names
   `JusticeFailure` as a real outcome, not just an absence of `CaseResolved`.

## Explicitly out of scope for the next cycle

- Any formal institution, written law, or precedent-setting mechanic - see the deferred
  column above.
- Writing back into `Settlement.Legitimacy` based on case outcomes (a real, natural future
  loop - "Justice influences public trust" per TG-590's own Simulation Rules - deferred
  the same way Education deferred its `TechnologyService` integration).
- Crime as a concept distinct from low-Trust conflict - this increment treats "dispute" as
  a proxy for any severe relationship breakdown, not a typed crime taxonomy.
- Cases spanning citizens in different settlements, or without a settlement at all.

## Open questions for review before implementation starts

1. Is `Trust < 20` the right dispute-detection threshold, or should it also require low
   `Affection`/high `SocialDistance` to avoid flagging citizens who are merely distant
   rather than in active conflict? (Recommendation: `Trust < 20` alone - TG-590 frames
   disputes as breakdowns of trust specifically ("law is civilization's memory of
   fairness"), and requiring multiple conditions risks the same "structurally unreachable"
   problem Week 8 found with `EducationSystem`'s pairing gate.)
2. Should the 3-yearly-evaluation unresolved window before `JusticeFailureEvent` be
   configurable per settlement (e.g. tied to `Legitimacy` or `Tier`), or a flat constant?
   (Recommendation: flat constant for this increment - per-system invented thresholds are
   expected given TG-### specs never provide numbers, and a flat window is simpler to
   verify than a settlement-scaled one.)
