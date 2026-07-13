# RFC-011: Decomposers & Microbiology — Soil Health (First Increment)

**Status:** Implemented (Week 17, 2026-07-13 - see DEVELOPMENT_PLAN.md Days 82-86)
**Date:** 2026-07-13
**Author:** `DEVELOPMENT_PLAN.md` Week 17
**Governing spec:** `03_Sciences/02_Life/TG-220_Decomposers_Microbiology.md`

---

## Why this needs an RFC before a day-to-day plan

Same reasoning as every prior RFC: `TG-220` is thorough on vocabulary (Organic Matter, Nutrient
Recycling, Soil Biology, Microbial Communities, Waste Processing) and names 9 events
(`TreeDecomposed`, `AnimalRemainsRecycled`, `NutrientPulseOccurred`, `SoilRecovered`,
`OrganicMatterAccumulated`, `MicrobialBloom`, `ForestFloorEnriched`, `WasteFullyDecomposed`,
`EcologicalRecyclingImproved`) but gives no formula for decomposition rate, nutrient conversion, or
soil health.

## Why Decomposers, and why now

Unlike Fauna (this week's other item), Decomposers does **not** require inventing a new species/
organism concept — `TG-220`'s own Performance Considerations require "ecosystem-level indicators
rather than individual organisms" and "regional aggregation," and the raw material it processes
(dead organisms, fallen trees) is **already produced by real, already-firing events**:
`CitizenDiedEvent` (every citizen death) and `ForestDeclinedEvent` (RFC-006, Week 10). This increment
needs no new species, only a new aggregate state variable per settlement, continuing the "reuse
what already exists" posture every RFC since RFC-004 has used.

## Scope decision: settlement-level Soil Health, fed by existing death/decline events, one real feedback into Agriculture

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| `Settlement.SoilHealth` (0-100, default 100 — civilization inherits an already-functioning ecosystem, the same framing `TG-240`'s "Relationship to Civilization" section uses) | Individual decomposer categories (Fungi, Bacteria, Detritivores, etc.) — `TG-220` explicitly says these can be added "without changing the ecological framework" later |
| `DecomposerSystem`: subscribes to the *existing* `CitizenDiedEvent`/`ForestDeclinedEvent` to accumulate per-settlement organic matter; subscribes to the *existing* `FarmHarvestedEvent` to apply a small soil-depletion cost (real-world soil science: farming without replenishment depletes nutrients — `TG-220`'s own "Relationship to Civilization" names this explicitly: "healthy soils support abundant harvests... neglected waste increases disease risk"); monthly evaluation decomposes a fraction of pending organic matter into `SoilHealth` gain | Carbon storage, water retention, disease resistance as separate tracked variables — `TG-220` lists these as things soil biology *influences*, not things this increment needs its own state for |
| 3 of `TG-220`'s 9 named events: `NutrientPulseOccurredEvent` (a meaningful `SoilHealth` rise), `OrganicMatterAccumulatedEvent`/`WasteFullyDecomposedEvent` (a settlement's pending-organic-matter backlog crossing a "waste is piling up faster than it can decompose" threshold, and clearing back below it) | `TreeDecomposed`, `AnimalRemainsRecycled`, `SoilRecovered`, `MicrobialBloom`, `ForestFloorEnriched`, `EcologicalRecyclingImproved` — the other 6, each requiring concepts (individual decomposition events per organism, a distinct "recovery" state) this increment doesn't model |
| **One real, deliberate feedback into `AgricultureSystem`**: farm yield is multiplied by `SoilHealth / 100.0` — the first RFC in this series to write into an earlier system's formula rather than staying strictly read-only, because `TG-220` explicitly names Agriculture as one of the systems Decomposers "directly influences," and a soil-health signal with no effect on anything would be purely decorative, violating `TG-STRY-050`'s "consequences, not spectacle" | Any other write-back (Flora growth rate, Disease resistance, Evolution) — `TG-220` names these as influenced too, but each needs its own justified integration point, not a blanket multiplier applied everywhere at once |

## Why this doesn't break `AgricultureSystemTests`

`Settlement.SoilHealth` defaults to `100.0`, and the multiplier is `SoilHealth / 100.0` — at the
default, this is `1.0`, an exact no-op. Every existing `AgricultureSystemTests` case constructs a
fresh `Settlement` (implicitly `SoilHealth = 100`) and calls `Execute()` once, before any harvest has
had a chance to deplete it — so every existing assertion's expected yield is unaffected. The
depletion/recovery dynamic only becomes visible after repeated harvests and real time passing, which
is exactly the multi-month timescale this system is meant to operate on.

## Mechanism

A new `DecomposerSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, monthly cadence
(`IntervalTicks = 24 * 30`, matching `PopulationEcologySystem`'s granularity — soil health changes
are observable monthly, not daily or yearly).

1. Subscribes to `CitizenDiedEvent`: looks up the citizen's `HomeSettlementId` and adds a fixed amount
   of organic matter to that settlement's pending backlog (tracked in-memory on the system, not the
   settlement, the same "avoid EF migration surface for a recomputable value" posture `RFC-007`/
   `RFC-008` used for their own previous-value tracking).
2. Subscribes to `ForestDeclinedEvent`: finds any settlement whose territory contains the event's
   tile (`Settlement.IsWithinTerritory`) and adds organic matter proportional to `AreaLost`.
3. Subscribes to `FarmHarvestedEvent`: reduces that settlement's `SoilHealth` by a small amount
   proportional to `Yield` (farming depletes soil without replenishment).
4. Each monthly evaluation, for each settlement with pending organic matter: decomposes a fixed
   fraction of the backlog, converting it into `SoilHealth` gain (capped at 100) and removing it from
   the backlog. A `SoilHealth` rise beyond an invented threshold publishes `NutrientPulseOccurredEvent`.
   A backlog crossing above an invented "piling up" threshold publishes `OrganicMatterAccumulatedEvent`;
   falling back below it publishes `WasteFullyDecomposedEvent` — the same crossing-detection shape
   every prior RFC's system already uses.
5. `AgricultureSystem.ProcessFarm` multiplies its existing yield formula by `settlement.SoilHealth /
   100.0`.
6. All three new events subscribed to `HistorySystem` **at introduction time** (`HistoryCategories.Nature`,
   reusing the category `ForestExpanded`/`SeasonChanged`/`AdaptiveShiftObserved` already established
   for biological/environmental change), continuing the practice reinforced Week 12 Day 61.

## Explicitly out of scope for the next cycle

- Individual decomposer species/categories, carbon storage, water retention as separate variables.
- Any feedback into Flora, Disease, or Evolution — only the Agriculture link is built this increment,
  since it's the one `TG-220` names most directly and concretely ("healthy soils support abundant
  harvests").
- A genuine "waste accumulation crisis" consequence (e.g. disease risk scaling with backlog) —
  `TG-220`'s own text suggests this ("neglected waste increases disease risk") but wiring it into
  `RFC-009`'s `DiseaseSystem` is a second cross-system feedback this RFC doesn't attempt in one pass.

## Open questions for review before implementation starts

1. Should the Agriculture feedback be a flat multiplier, or should it only apply above/below some
   `SoilHealth` band? (Recommendation: flat multiplier — simplest, and matches how `RFC-007`/`RFC-008`
   both used direct linear formulas rather than banded ones for their first increments.)
2. Should `CitizenDiedEvent` from disease (`RFC-009`) contribute the same organic matter as any other
   death? (Recommendation: yes — `TG-220` processes "Dead Animals"/"Dead Plants" generally, with no
   distinction by cause of death; inventing a cause-based scaling would be unsupported by the spec.)

## Implementation notes (Week 17, added at close-out)

- Implemented as designed: `Settlement.SoilHealth` (new EF migration `AddSettlementSoilHealthAndWildlife`,
  applied live) + `DecomposerSystem` (monthly cadence), subscribed to the existing
  `CitizenDied`/`ForestDeclined`/`FarmHarvested` events, feeding back into `AgricultureSystem`'s yield.
  Both open questions resolved as recommended.
- **A real migration-scaffolding bug was caught and fixed before applying it to the database**:
  `dotnet ef migrations add` scaffolded `defaultValue: 0.0` for the new `SoilHealth` column, even
  though the C# property default is `100.0` — EF Core scaffolds the CLR type's zero-value default for
  a new column, not the property initializer. Applying the migration as-generated would have silently
  reset every existing settlement's soil to "fully depleted" instead of "healthy." Caught by reviewing
  the generated migration file before running `database update`, not by a later live-verification
  surprise; corrected to `defaultValue: 100.0` before applying.
- **A second real bug was caught by the regression tests, not live verification**: the first
  `Execute()` draft captured `previousSoilHealth` *after* that same evaluation's decomposition had
  already updated it, so the "previous" baseline always equaled the post-decomposition value on a
  settlement's first-ever evaluation — meaning `NutrientPulseOccurredEvent` could never fire on the
  first real rise. Fixed by capturing the baseline before applying decomposition, the same fix shape
  Week 16's `EvolutionSystem` needed for a different reason.
- 6 new `DecomposerSystemTests` plus 3 more for the `HistorySystem` wiring, including a dedicated test
  confirming `AgricultureSystemTests`' existing assertions are unaffected by the default `SoilHealth`
  of 100 (multiplier = 1.0, an exact no-op).
- Verified live: all 8 real settlements' `SoilHealth` remained at the correct default (100) at server
  start (confirming the migration fix took effect correctly), and `AgricultureSystem`'s yield
  continues unaffected until the first real depletion/decomposition cycle plays out over more
  simulated time than this session's verification window covered — a legitimate non-finding, not a
  bug, per the established precedent for slow-timescale mechanics.
