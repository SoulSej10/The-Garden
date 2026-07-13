# RFC-009: Disease & Health — Overcrowding-Driven Infection (First Increment)

**Status:** Implemented (Week 15, 2026-07-13 - see DEVELOPMENT_PLAN.md Days 72-76)
**Date:** 2026-07-13
**Author:** `DEVELOPMENT_PLAN.md` Week 15
**Governing spec:** `03_Sciences/02_Life/TG-260_Disease_Health.md`

---

## Why this needs an RFC before a day-to-day plan

Same reasoning as every prior RFC: `TG-260` is thorough on vocabulary (Health, Disease, Pathogens,
Transmission, Immunity, Injury, Recovery, Epidemics) and names 10 events (`OrganismInfected`,
`DiseaseRecovered`, `PopulationOutbreak`, `InjurySustained`, `HealthImproved`, `ImmunityDeveloped`,
`EpidemicStarted`, `EpidemicContained`, `DiseaseExtinct`, `PopulationHealthDeclined`) but gives no
formula for transmission probability, severity, or recovery. `TG-260`'s own "Depends On" list names
`TG-220` (Decomposers), `TG-230` (Fauna), and `TG-250` (Evolution) — none of which have any code
anywhere in this repository (confirmed via grep: zero references to pathogens, disease, or infection
in `src/`).

## Why Disease & Health, and why now

Per the Week 14 timeline note, remaining Life Sciences needs "its own scoping RFC deciding what
minimal species/organism concept to introduce" for each document. Unlike Fauna/Decomposers (which
genuinely require a wildlife/microbe population that doesn't exist), Disease & Health can follow the
exact precedent `RFC-008` (Population Ecology) established: apply the biological principle to the one
population that already exists in this codebase — `Citizen` — rather than inventing new species. TG-260
itself frames disease as depending on population density ("Population density increases epidemic
risk"), and `RFC-008` just shipped exactly that signal (`Settlement.CarryingCapacity` /
`Settlement.MemberIds.Count`, i.e. population pressure) — this RFC reuses it directly, continuing the
"reuse an existing field" posture every RFC since RFC-004 has used.

## Scope decision: overcrowding-driven infection on citizens, detection + one real consequence

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| A new pure in-memory `Infection` entity (`Garden.World/Entities/`, following the exact `LegalCase`/`Apprenticeship` pattern — not EF-persisted) tracking a citizen's active infection and severity | Pathogen categories, immunity mechanics, injury (a separate `TG-260` concept from disease) — no existing state to build these on yet |
| `DiseaseSystem`: daily cadence; citizens in an over-capacity settlement (`Population >= CarryingCapacity`, reusing RFC-008's pressure signal verbatim) risk contracting infection; an infected citizen's severity grows daily, damaging existing `Needs.Health`; severity reaching its maximum kills the citizen via the existing `CitizenDiedEvent` (cause: `"Disease"`) rather than inventing a parallel death path; a citizen may naturally recover, chance scaled by their current `Needs.Health` | Endemic/background disease unrelated to overcrowding, seasonal outbreaks, disease reservoirs, long-term carriers — all named in `TG-260`'s Edge Cases but requiring concepts (a persistent pathogen identity, immunity) this increment doesn't model |
| 4 of `TG-260`'s 10 named events: `OrganismInfectedEvent`, `DiseaseRecoveredEvent`, `EpidemicStartedEvent`/`EpidemicContainedEvent` (settlement-level infection-rate crossing a threshold, tracked the same way `TerritorySystem`/`PopulationEcologySystem` track their own crossings) | `PopulationOutbreak`, `InjurySustained`, `HealthImproved`, `ImmunityDeveloped`, `DiseaseExtinct`, `PopulationHealthDeclined` — the other 6, each requiring concepts (injury as distinct from disease, immunity, a notion of "extinct" pathogen identity) this increment doesn't add |
| — | Writing disease risk back into `CarryingCapacity`/`Population` — one-directional read only, the same posture every prior RFC in this series has held |

## Why overcrowding (`Population >= CarryingCapacity`) specifically

`TG-260` names high population density, poor sanitation, migration, climate, and food shortages as
epidemic drivers. Of these, population density relative to what a settlement can actually sustain is
the only one with a real, already-computed number (`RFC-008`, shipped last week). Reusing it directly
means this RFC needs zero new supporting systems — the same reasoning RFC-004 used for `Intelligence`
and RFC-007/008 used for `Population`/`Legitimacy`/`Food`.

## Mechanism

A new `DiseaseSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, daily cadence (`IntervalTicks =
24`, matching the granularity of individual `Needs`/`Health` changes rather than the settlement-level
monthly/yearly cadences).

1. **Onset**: for each settlement where `Population >= CarryingCapacity` (the exact condition
   `PopulationEcologySystem` uses for `PopulationDeclineEvent` — overcrowded), every living,
   uninfected citizen has a small daily chance (invented — no `TG-260` number given) of contracting a
   new `Infection` (`Severity` starts low). Publishes `OrganismInfectedEvent`.
2. **Progression**: each day, an active infection's `Severity` increases by a small invented amount,
   and proportionally reduces the citizen's `Needs.Health` (their existing field — no duplicate health
   concept introduced). Each day also rolls a recovery chance, scaled by the citizen's *current*
   `Needs.Health` (a healthier citizen resists and recovers better, per `TG-260`'s "Healthy organisms
   resist disease more effectively") — on recovery, the infection is marked inactive and
   `DiseaseRecoveredEvent` fires.
3. **Death**: if `Severity` reaches its maximum before recovery, the citizen dies. `DiseaseSystem`
   performs the same minimal death bookkeeping `CitizenSystem.Die()` does internally (`IsAlive = false`,
   `DeathTick`, `CauseOfDeath = "Disease"`) and publishes the *existing* `CitizenDiedEvent` — reusing
   the established death event rather than inventing a parallel one, so every existing consequence
   already wired to it (the Week 12 grief-Trust mechanic, `HistorySystem`'s death archiving, population
   counts) applies automatically, with zero new integration work.
4. **Epidemic detection**: each evaluation, compute a settlement's active-infection rate
   (`activeInfections / Population`). Crossing above 20% (invented threshold) publishes
   `EpidemicStartedEvent`; falling back below it publishes `EpidemicContainedEvent` — tracked via a
   per-settlement boolean on the system (the same crossing-detection shape `TerritorySystem`'s
   dispute tracking and `PopulationEcologySystem`'s pressure tracking both already use).
5. All four events subscribed to `HistorySystem` **at introduction time** (`HistoryCategories.Death`
   for `OrganismInfected`/`DiseaseRecovered` — reusing the category `CitizenDied` already established,
   since these are the same biological domain; a new `HistoryCategories.Disaster` fit for the epidemic
   pair, matching how `MajorFlood`/`LongDrought`/`Plague`/`Wildfire` are already whitelisted there),
   continuing the practice reinforced Week 12 Day 61 rather than risking a sixth instance of the
   TG-001 Law IV gap.

## Explicitly out of scope for the next cycle

- Injury as a concept distinct from disease (`TG-260` treats them separately; this increment only
  models disease).
- Immunity, pathogen identity/categories, disease reservoirs, seasonal/endemic disease unrelated to
  overcrowding.
- Any consequence beyond `Needs.Health` damage and death — no quarantine, no medicine, no sanitation
  mechanic (all explicitly "future" in `TG-260` itself).
- Fauna/Decomposers as actual wildlife/microbe populations — this increment, like `RFC-008`, applies
  the *principle* to citizens rather than building the dependency `TG-260` formally lists.

## Open questions for review before implementation starts

1. Should overcrowding be the *only* infection trigger, or should there also be a small ambient
   baseline risk regardless of capacity? (Recommendation: overcrowding-only for this increment —
   `TG-260` explicitly calls out density as a driver, and an ambient/endemic baseline needs its own
   invented rate with no crowding signal to ground it, unlike this one which reuses `RFC-008` directly.)
2. Should `DiseaseSystem` duplicate `CitizenSystem.Die()`'s minimal bookkeeping, or should
   `CitizenSystem` expose a shared method? (Recommendation: duplicate the ~4-line bookkeeping —
   `ReproductionSystem` already sets its own `Citizen` fields directly rather than routing through
   `CitizenSystem`, so this matches an existing precedent rather than adding new coupling between
   systems for a handful of field assignments.)

## Implementation notes (Week 15, added at close-out)

- Implemented as designed: a pure in-memory `Infection` entity (`Garden.World/Entities/Infection.cs`,
  following the exact `LegalCase`/`Apprenticeship` pattern - no EF migration needed), `DiseaseSystem`
  (daily cadence), and the four named events, all subscribed to `HistorySystem` at introduction time.
  Both open questions resolved as recommended.
- `DiseaseSystem.ProgressInfections` duplicates `CitizenSystem.Die()`'s minimal death bookkeeping
  (`IsAlive`, `DeathTick`, `CauseOfDeath`) but reuses the *existing* `CitizenDiedEvent` rather than
  inventing a parallel death path - every consequence already wired to that event (the Week 12
  grief-Trust mechanic, `HistorySystem`'s death archiving, population counts) applies automatically.
- A minor severity-tuning issue was found and fixed during test-writing, not live verification:
  `OrganismInfected`/`DiseaseRecovered` were first archived at severity `4.0`, which
  `SignificanceEvaluator` classifies as "Low" (requires `> 4.0` for "Medium") - meaning
  `ShouldArchive` silently dropped them despite being subscribed correctly. Caught by the
  `HistorySystemCivilizationEventTests` regression tests failing, not a live-verification surprise;
  bumped to `5.0`.
- 7 new `DiseaseSystemTests` plus 4 more for the `HistorySystem` wiring. Recovery-chance tests use
  citizens with near-zero `Needs.Health` where determinism matters (e.g. the death test), since
  recovery is checked *before* the severity/death check each evaluation - a healthy citizen can
  recover even at maximum severity, a deliberate "recovery is always possible" reading of `TG-260`'s
  Design Philosophy, not a bug, but it would otherwise make specific tests flaky.
