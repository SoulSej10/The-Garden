# RFC-001: Emotion System (First Increment)

**Status:** Implemented (2026-07-09, DEVELOPMENT_PLAN.md Week 3 Days 11-14 — both open questions resolved as recommended: additive hooks kept, `Relationship.Trust` kept separate from `EmotionalState.Trust`)
**Date:** 2026-07-09
**Author:** DEVELOPMENT_PLAN.md Week 2 Day 10
**Target:** DEVELOPMENT_PLAN.md Week 3 (Days 11-15)
**Governing spec:** `03_Sciences/03_Cognitive/TG-330_Emotion.md`

---

## Why this needs an RFC before a day-to-day plan

Per `SPEC_INDEX.md`'s cross-cutting finding, `TG-330_Emotion.md` is pure design
philosophy: it names 15 emotions (Joy, Fear, Sadness, Anger, Curiosity, Hope,
Love, Grief, Pride, Shame, Trust, Disgust, Loneliness, Awe, Calmness) and
asserts they coexist with independent intensities and different decay rates
("seconds to decades"), but gives no formula, no data type, no decay
half-life, and no integration point. `Emotion` currently has **zero code
footprint** anywhere in `src/` — confirmed by grep. This RFC exists to invent
the missing numbers and pick a concrete, buildable scope before Week 3 opens,
per `DEVELOPMENT_PLAN.md`'s own rule that greenfield systems need this step
first.

## Scope decision: 6 emotions, not 15

Building all 15 named emotions in one increment is disproportionate to every
other system in this codebase, which ships thin, real subsets before
expanding (see `PersonalityTraits` at 6 of TG-370's 16+ dimensions,
`CitizenNeeds` covering only TG-340's physiological tier). First increment
covers exactly the emotions with a direct, already-real trigger somewhere in
`CitizenSystem.cs`/`ReproductionSystem.cs`/`HistorySystem.cs` today:

| Emotion | Real trigger available today | Deferred emotions |
|---|---|---|
| **Fear** | Needs.Health or Needs.Hunger/Thirst critical (see `CitizenSystem.cs` lines 152-165, the same thresholds that already drive `FindWater`/`FindFood`/`Rest` goals) | Awe, Disgust |
| **Joy** | Needs recovering after being critical; `SettlementFoundedEvent`, `CitizenBornEvent` involving the citizen | Hope, Pride |
| **Sadness** | `CitizenDiedEvent` for a settlement-mate or family member | Grief (treated as an intensified/slow-decaying Sadness in this increment, not a separate emotion - see below) |
| **Trust** | Existing `Citizen.Reputation` interactions and (once Week 3 lands) the pairwise `Relationship` from `TG-380` | Love |
| **Curiosity** | Already a `PersonalityTraits.Curiosity` trait exists (0-100 baseline) - this increment adds a transient state on top of that stable trait | — |
| **Loneliness** | `Citizen.HomeSettlementId == null` combined with time-since-last-social-interaction | — |

**Grief, Hope, Pride, Love, Awe, Disgust, Shame, Anger, Calmness are explicitly
out of scope for this increment.** They require inputs this codebase doesn't
have yet (a distinct grief/mourning timeline, achievement tracking, romantic
pair-bonding, aesthetic/environmental triggers, social transgression
detection, or existing anger/frustration state) - each is a candidate for its
own RFC once Relationships (also Week 3) or Norms/Law (backlog) exist.

## Data model

```csharp
// Garden.World/Entities/Citizen.cs - new nested type, same pattern as
// PersonalityTraits/CitizenNeeds already on Citizen.
public class EmotionalState
{
    public double Fear { get; set; }
    public double Joy { get; set; }
    public double Sadness { get; set; }
    public double Trust { get; set; } = 50.0; // matches Reputation's neutral baseline
    public double Curiosity { get; set; }
    public double Loneliness { get; set; }
}
```

Added to `Citizen` as `public EmotionalState Emotions { get; set; } = new();`
- same shape as the existing `Attributes`/`Personality`/`Needs` properties, so
it follows the codebase's own established convention rather than inventing a
new one. All six values are 0-100 doubles, matching every other citizen stat
in this codebase (`CitizenNeeds`, `PersonalityTraits`, `Reputation`,
`ContributionScore` are all already 0-100 or unbounded-positive doubles) -
picked for consistency, not because the spec requires it (it doesn't specify
a scale at all).

## Decay model

TG-330 says emotions decay at different rates ("Startle" fast, "Love" decades)
but gives no numbers. Proposed half-lives, chosen to be distinguishable at the
simulation's existing tick cadence (1 tick = 1 hour, per `TG-004`) without
requiring new scheduling infrastructure:

| Emotion | Half-life | Rationale |
|---|---|---|
| Fear | 6 ticks (~6 hours) | Matches how fast Needs themselves swing back once the citizen acts on `FindWater`/`FindFood` |
| Joy | 24 ticks (~1 day) | Outlasts the triggering event by about a day, then fades |
| Sadness | 168 ticks (~1 week) | Grief-adjacent; deliberately the slowest of this first batch |
| Trust | 720 ticks (~1 month) | Mirrors `DiplomacyService`'s existing weekly-evaluated, slow-drifting `RelationScore` pattern (`DiplomacyService.cs` `CalculateDiplomaticScoreChange`) |
| Curiosity | 12 ticks (~12 hours) | Transient state on top of the stable Personality trait; short-lived by design |
| Loneliness | 48 ticks (~2 days) | Slow enough to matter for goal selection, fast enough to resolve once a citizen joins a settlement |

Decay formula (exponential toward a per-emotion baseline, not toward zero -
Trust in particular should decay toward 50/neutral, not toward 0):

```csharp
value = baseline + (value - baseline) * Math.Pow(0.5, ticksElapsed / halfLife);
```

This is a genuinely invented formula (TG-330 gives none) - flag any
alternative in code review, but don't block Week 3 on relitigating it; it's a
one-line change to swap later if it proves wrong in practice, same as every
other invented threshold already in this codebase (`HungerCriticalThreshold`,
`GovernanceService`'s population thresholds, this RFC's own Legitimacy
formula from Day 9).

## Integration point: CitizenSystem's existing decision chain

`CitizenSystem.cs` currently drives `CurrentGoal`/`CurrentActivity` through a
hardcoded if/else priority chain keyed on `Needs` thresholds (lines ~150-320).
Per `TG-350_Decision_Making.md`, which is explicitly permissive about
implementation ("utility systems... may be used internally provided they
preserve the behavioral principles"), this RFC proposes **not** rewriting
that chain into a full utility-AI system in Week 3 - that's disproportionate
to a first Emotion increment. Instead:

1. A new `EmotionSystem` (in `Garden.Engine/Systems/`, following the existing
   `IScheduledSystem` pattern used by every other system) ticks emotional
   decay and applies triggers by subscribing to existing events
   (`CitizenDiedEvent`, `SettlementFoundedEvent`, `CitizenBornEvent`) the same
   way `HistorySystem` already does (see `HistorySystem.cs` `SubscribeToEvents()`
   for the established pattern) - reuse it, don't reinvent it.
2. `CitizenSystem`'s existing if/else chain gets **narrow, additive** hooks:
   e.g. high `Fear` lowers the threshold at which `FindWater`/`FindFood`
   trigger (a citizen who is already afraid seeks safety sooner), and high
   `Loneliness` adds a new low-priority goal (`Socialize`) beneath the
   existing physiological-needs checks. This is a few added branches, not a
   restructure.
3. No new API controller in this first increment - Emotion is exposed
   read-only through the existing `CitizensController` citizen-detail
   projection (add `Emotions` alongside the existing `Needs`/`Attributes`
   projections), consistent with Day 8/Day 9's practice of always surfacing
   new domain concepts to the Observatory rather than leaving them
   backend-only.

## Explicitly out of scope for Week 3

- Rewriting `CitizenSystem`'s decision loop into a formal utility-AI system
  (`TG-350`'s "Decision Cycle" with Perception/Recall/Emotional
  Eval/Motivational Analysis stages) - track as a future RFC once Emotion,
  Relationships (also Week 3), and Memory-strength decay (`TG-320`, also
  unimplemented - see `SPEC_INDEX.md`) all exist to feed it.
- The other 9 named emotions (see Scope Decision table above).
- Any UI beyond a read-only field in the existing citizen detail view -
  no new charts/visualizations this increment.

## Open questions for review before Week 3 Day 11 starts

1. Does "narrow additive hooks" into `CitizenSystem`'s if/else chain risk
   making that method harder to reason about over time, such that the
   decision-loop-rewrite RFC should be pulled forward instead? (Recommendation:
   no - the chain is already read top-to-bottom procedurally; a handful of
   `if (citizen.Emotions.Fear > X)` branches follow the same shape as what's
   there today, this doesn't compound complexity the way a parallel
   scoring system bolted on top would.)
2. Should `Trust` in this RFC be the same concept as the pairwise
   `Relationship.Trust` that `TG-380`/Week 3 Days 13-14 will add, or a
   separate citizen-level scalar? (Recommendation: keep them separate for
   now - `EmotionalState.Trust` is a general disposition/mood, `Relationship.
   Trust` is pair-specific; merging them is a valid future simplification but
   premature before either exists in code.)
