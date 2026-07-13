# RFC-010: Evolution & Adaptation — Adaptive Drift Detection (First Increment)

**Status:** Implemented (Week 16, 2026-07-13 - see DEVELOPMENT_PLAN.md Days 77-81)
**Date:** 2026-07-13
**Author:** `DEVELOPMENT_PLAN.md` Week 16
**Governing spec:** `03_Sciences/02_Life/TG-250_Evolution_Adaptation.md`

---

## Why this needs an RFC before a day-to-day plan

Same reasoning as every prior RFC: `TG-250` is thorough on vocabulary (Adaptation, Selective Pressure,
Variation, Inheritance, Speciation) and names 10 events (`AdaptiveShiftObserved`, `PopulationAdapted`,
`BehaviorModified`, `EnvironmentalPressureIncreased`, `TraitBecameCommon`, `EvolutionaryStagnation`,
`PopulationFailedToAdapt`, `SpeciationCandidateIdentified`, `EvolutionaryBottleneck`) but gives no
formula for selection strength, adaptive fitness, or drift rate. It also explicitly states
"Initial releases of The Garden do not require full speciation mechanics" (line 246) — the document
itself scopes down its own first increment.

## Why Evolution & Adaptation, and why now

`TG-250`'s core claim is that "Individuals do not evolve. Populations evolve over many generations" —
this is, unusually among the Volume IV documents, **already happening** in this codebase, just never
measured or named: `ReproductionSystem.Average()` already inherits each child's `Attributes` from both
parents with random variance, and `CitizenSystem.CheckHealth()` already kills citizens whose `Needs`
(hunger, thirst, warmth, energy) go critical — meaning citizens with better-suited attributes already
survive and reproduce more than poorly-suited ones, a real (if crude) selection pressure. This RFC
does not invent evolution; it **observes** the population-level drift that inheritance-plus-differential-
survival already produces, exactly as `TG-250`'s "Design Philosophy" frames it: adaptation is not
engineered, it emerges.

## Scope decision: detect population-level attribute drift, no new consequence

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| `EvolutionSystem`: yearly cadence (`SimulationTime.TicksPerYear`); computes each settlement's average `Citizen.Attributes` (Strength/Endurance/Intelligence/Dexterity/Perception) across its living members and compares against the previous year's average (tracked in-memory on the system, same posture `TerritorySystem`/`PopulationEcologySystem` use for their own previous-value tracking) | Genetics, mutation, gene flow, sexual/artificial selection — all explicitly "Future Extensions" in `TG-250` itself |
| 2 of `TG-250`'s 10 named events: `AdaptiveShiftObservedEvent` (an attribute's average moved meaningfully since last year) and `EvolutionaryStagnationEvent` (no attribute has shifted meaningfully for several consecutive years — the direct converse, matching `TG-250`'s "Stable environments slow evolutionary change") | `PopulationAdapted`, `BehaviorModified`, `EnvironmentalPressureIncreased`, `TraitBecameCommon`, `PopulationFailedToAdapt`, `SpeciationCandidateIdentified`, `EvolutionaryBottleneck` — the other 8, each requiring concepts (behavior as distinct from attributes, explicit environmental-pressure tracking, speciation) this increment doesn't model |
| — | Speciation in any form — `TG-250` itself says this isn't required for initial releases (line 246) |
| — | Any new selection mechanic — `ReproductionSystem`'s existing inheritance-with-variance and `CitizenSystem`'s existing differential-survival already are the selection pressure `TG-250` calls for; this RFC only observes their aggregate effect, it doesn't add a second mechanic on top |

## Why yearly cadence and settlement-level averaging specifically

`TG-250` explicitly requires "Multiple generations" and "Evolutionary Time" measured in generations,
not days — the yearly cadence already established for civilization-milestone systems
(`SimulationTime.TicksPerYear`, Week 12 Day 58) is the natural fit, since a single citizen generation
(reproductive age 18-45, per `ReproductionSystem`) spans many years. Settlement-level averaging reuses
the same population boundary `RFC-008` (Population Ecology) already established — a settlement's
citizens are the one population this codebase actually tracks.

## Mechanism

A new `EvolutionSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, yearly cadence
(`IntervalTicks = SimulationTime.TicksPerYear`).

1. Each yearly evaluation, for each settlement, compute the average of each of the five
   `CitizenAttributes` fields across its living members.
2. Compare each average against the value recorded at the previous evaluation (stored per-settlement,
   per-attribute, on the system — not the settlement, avoiding EF migration surface for a fully
   recomputable value, the same reasoning `RFC-007`/`RFC-008` used for their own previous-value
   tracking).
3. A rise or fall in any single attribute's average beyond an invented threshold (0.5, roughly 10% of
   a starting attribute's typical 5.0 baseline) since last year publishes `AdaptiveShiftObservedEvent`,
   naming which attribute shifted and in which direction.
4. If no attribute has shifted beyond that threshold for 3 consecutive yearly evaluations (invented —
   no `TG-250` number given), publishes `EvolutionaryStagnationEvent` once, not re-firing every
   subsequent stagnant year (tracked via a per-settlement boolean, the same crossing-detection shape
   `TerritorySystem`/`PopulationEcologySystem`/`RFC-009`'s `DiseaseSystem` all already use).
5. Both events subscribed to `HistorySystem` **at introduction time** (`HistoryCategories.Nature`,
   reusing the category `ForestExpanded`/`SeasonChanged` already established for biological/
   environmental change, rather than inventing an `Evolution` category), continuing the practice
   reinforced Week 12 Day 61.

## Explicitly out of scope for the next cycle

- Any new mechanic that *causes* attribute drift — `ReproductionSystem`'s inheritance and
  `CitizenSystem`'s differential survival already are the cause; this RFC only measures the effect.
- Behavior evolution, environmental-pressure tracking as its own state variable, speciation,
  genetics/mutation — all named in `TG-250` but requiring concepts this increment doesn't add.
- Writing back into `Attributes`/`ReproductionSystem` based on detected drift — one-directional read
  only, the same posture every prior RFC in this series has held.

## Open questions for review before implementation starts

1. Is 0.5 the right "meaningful shift" threshold, or should it scale with each attribute's typical
   range? (Recommendation: keep a flat 0.5 — `CitizenAttributes`'s five fields all share the same
   0-10 scale and the same `Average()` variance formula in `ReproductionSystem`, so there's no reason
   one attribute's threshold should differ from another's, unlike `RFC-007`'s independently-tuned
   expand/contract vs. dispute thresholds which answered genuinely different questions.)
2. Should stagnation reset the moment any shift occurs, or only after a shift *and* a full year passes?
   (Recommendation: reset immediately on any shift — a settlement that adapts one year and stagnates
   the next should be able to report both, rather than the stagnation counter carrying over
   incorrectly from before the shift.)

## Implementation notes (Week 16, added at close-out)

- Implemented as designed: `EvolutionSystem` (yearly cadence) compares each settlement's average
  `Citizen.Attributes` against the previous year's, publishing `AdaptiveShiftObservedEvent`/
  `EvolutionaryStagnationEvent`, both subscribed to `HistorySystem` at introduction time. Both open
  questions resolved as recommended. No new `Settlement`/`Citizen` field was added - the averages are
  computed live each evaluation and tracked only in the system's own in-memory dictionaries, the same
  "avoid EF migration surface for a recomputable value" posture `RFC-007`/`RFC-008` used.
- **A subtle correctness issue was found and fixed before it ever shipped**: the first draft counted a
  settlement's very first yearly evaluation (which has no prior average to compare against) as a
  "stagnant" year, since no shift could be detected against a baseline that didn't exist yet. This
  would have let `EvolutionaryStagnationEvent` fire after only 3 calls to a brand-new settlement that
  had never actually been observed for 3 real years. Fixed by tracking whether a baseline already
  existed per settlement and skipping the stagnation counter entirely on the baseline-establishing
  year - the same "no event fires on the first `Execute()`" convention `TerritorySystem`/
  `PopulationEcologySystem` already use, applied correctly to a secondary counter this time, not just
  the primary shift detection.
- 5 new `EvolutionSystemTests` plus 2 more for the `HistorySystem` wiring, including a dedicated test
  confirming the stagnation counter resets after a real shift rather than continuing to accumulate
  across it.
- Minimal Observatory surfacing added as an "Adaptation" section showing live per-settlement Attribute
  averages (Strength/Endurance/Intelligence/Dexterity/Perception) - the same signal `EvolutionSystem`
  itself watches for drift, not a separate concept.
