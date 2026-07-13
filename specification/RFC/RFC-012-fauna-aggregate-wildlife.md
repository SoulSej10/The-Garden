# RFC-012: Fauna & Animal Behavior — Aggregate Wildlife Population (First Increment)

**Status:** Implemented (Week 18, 2026-07-13 - see DEVELOPMENT_PLAN.md Days 87-91)
**Date:** 2026-07-13
**Author:** `DEVELOPMENT_PLAN.md` Week 18
**Governing spec:** `03_Sciences/02_Life/TG-230_Fauna_Animal_Behavior.md`

---

## Why this needs an RFC before a day-to-day plan

`TG-230` is the most ambitious document read so far in this cycle: individual animal perception,
decision hierarchies, territory, social structures, migration, and predation — describing "the
world's first autonomous decision-makers." It gives no formula for any of it, and — critically — its
own **Performance Considerations** section explicitly rejects the individual-agent approach its prose
otherwise describes: *"Most animal populations should be simulated using aggregate ecological models.
Only nearby, significant, or historically important individuals require persistent behavioral
simulation."* This RFC takes that sentence as the actual scope boundary, not the vivid individual-
animal prose surrounding it.

## Why Fauna, and why now

This is the first RFC in the Life Sciences series that genuinely cannot reuse `Citizen` — Disease,
Evolution, and Population Ecology all applied their principle to the one population that already
existed; Fauna requires *wildlife*, a population that has never existed in this codebase in any form
(confirmed via grep: zero references to animals, wildlife, or fauna anywhere in `src/`). This RFC
introduces the minimum new state that lets `TG-230`'s aggregate model exist at all, without building
individual animal agents.

## Scope decision: settlement-territory aggregate population, driven by habitat, detection only

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| `Settlement.WildlifePopulation` (a single aggregate number, not a species breakdown — `TG-230` explicitly allows "Species diversity may expand indefinitely" as a *later* step, not a first-increment requirement) | Individual animal agents, perception, decision hierarchies, movement, territory establishment by animals themselves, social structures (packs/herds/flocks) — all explicitly the *individual* layer `TG-230`'s own Performance Considerations defers |
| `FaunaSystem`: monthly evaluation computes a settlement's **habitat capacity** from real, already-existing map data (`Forest`-terrain tile count within the settlement's territory, reusing `WorldMap`/`Settlement.IsWithinTerritory` — no new environmental state), then grows/shrinks `WildlifePopulation` toward that capacity, the same "population moves toward a capacity" shape `RFC-008` already established for citizens | Predator-prey dynamics, migration, reproduction strategies, competition — all named in `TG-230` but requiring multiple distinct species/categories this increment doesn't model (there is only one aggregate number, not herbivores vs. carnivores) |
| 2 of `TG-230`'s 10 named events: `SpeciesExpandedEvent` (population crosses meaningfully upward relative to habitat capacity) and `AnimalDiedEvent`, **reinterpreted at the aggregate level** as a meaningful population die-off (crossing meaningfully downward) rather than a single animal's death — an explicit, documented reinterpretation, not a literal implementation of the named event | `AnimalBorn`, `PackFormed`, `MigrationStarted`, `PredatorHunted`, `PreyEscaped`, `NestConstructed`, `TerritoryEstablished`, `OffspringProtected` — the other 8, each requiring individual-agent or multi-species concepts this increment doesn't add |
| — | Any feedback from Fauna into Flora, Disease, Agriculture, or Population Ecology — `TG-230` names all of these as influenced, but this increment is read-only against `Forest` tile data, not a write-back into any of them, unlike `RFC-011`'s deliberate one-directional write into Agriculture |

## Why Forest-tile density specifically

`TG-230` names Biome, resource availability, and habitat as capacity drivers. Of everything already
tracked in this codebase, `Forest`-terrain tile count is the only real, already-computed environmental
number that plausibly represents "habitat" — it's the same signal `EcologySystem`'s
`ForestExpanded`/`DeclinedEvent` (Week 10) already tracks changes to, so wildlife capacity rising and
falling with forest cover is both spec-consistent (`TG-230`'s "Relationships" section names Flora as
something Fauna "directly influences," and the reverse relationship — habitat availability shaping
population — is equally well-established ecology) and requires zero new environmental state.

## Mechanism

A new `FaunaSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, monthly cadence (`IntervalTicks =
24 * 30`, matching `DecomposerSystem`'s/`PopulationEcologySystem`'s granularity for slow-moving
ecological state).

1. Each evaluation, for each settlement, count `Forest`-terrain tiles within `TerritoryRadius` of the
   settlement (iterating the same tile range `Settlement.IsWithinTerritory` already checks against).
2. `HabitatCapacity = ForestTileCount * 2` (invented multiplier — no `TG-230` formula given; a
   settlement with meaningful forest cover should support a population noticeably larger than its
   own citizen count, since wildlife isn't gated by housing/food-per-capita the way citizens are).
3. Move `WildlifePopulation` a fraction of the way toward `HabitatCapacity` each month (invented rate,
   the same "gradual movement toward a target" shape `TerritorySystem`'s territory expansion uses,
   rather than snapping instantly).
4. Compare against the previous evaluation's population (tracked in-memory on the system, the same
   posture every prior RFC's system uses for its own previous-value tracking): a rise beyond an
   invented threshold, while population remains below habitat capacity, publishes
   `SpeciesExpandedEvent`; a fall beyond an invented threshold publishes `AnimalDiedEvent`
   (reinterpreted as an aggregate die-off, documented above).
5. Both events subscribed to `HistorySystem` **at introduction time** (`HistoryCategories.Nature`),
   continuing the practice reinforced Week 12 Day 61.

## Explicitly out of scope for the next cycle

- Individual animals, species categories, predator-prey dynamics, migration, social structures —
  all the *literal* content of `TG-230`'s prose sections, deliberately deferred per its own
  Performance Considerations.
- Any feedback from `WildlifePopulation` into other systems (Agriculture, Disease, Flora growth
  rate) — read-only against `Forest` tile data this increment, unlike `RFC-011`'s Agriculture link.
- Domestication (`TG-270`) — explicitly a separate, later document, not attempted here.

## Open questions for review before implementation starts

1. Is `ForestTileCount * 2` the right habitat-capacity formula, or should it also factor in
   non-Forest terrain (Grassland, Plains)? (Recommendation: `Forest` only for this increment — it's
   the one terrain type this codebase already treats as ecologically significant via
   `ForestExpanded`/`DeclinedEvent`; Grassland/Plains have no equivalent tracked significance yet.)
2. Should `WildlifePopulation` ever go negative or be clamped at 0? (Recommendation: clamp at 0 —
   a settlement with zero forest tiles should be able to reach zero wildlife, not go negative, the
   same `Math.Max(0, ...)` pattern already used for `Needs.Health` in `RFC-009`.)

## Implementation notes (Week 18, added at close-out)

- Implemented as designed: `Settlement.WildlifePopulation` (same EF migration as `RFC-011`,
  `AddSettlementSoilHealthAndWildlife`) + `FaunaSystem` (monthly cadence), computing habitat capacity
  from `Forest`-terrain tile count within the settlement's territory and moving `WildlifePopulation`
  toward it each month. Both open questions resolved as recommended.
- 4 new `FaunaSystemTests` plus 2 more for the `HistorySystem` wiring, covering the zero-forest case,
  real growth toward habitat capacity, `SpeciesExpandedEvent` firing on real growth, and
  `AnimalDiedEvent` firing when a territory is deforested out from under an already-established
  population.
- Verified live against a resumed simulation run to Year 1: `WildlifePopulation` diverged
  meaningfully across all 8 real settlements based on real forest cover within each territory
  (0 in the least-forested settlement, up to 56 in the most-forested), and 37 organic
  `SpeciesExpandedEvent` records archived correctly. `AnimalDiedEvent`/`NutrientPulseOccurredEvent`
  (Week 17) did not occur organically within the verification window — a legitimate non-finding,
  since wildlife was still growing toward capacity (not yet declining) and no farm had harvested
  enough times yet to meaningfully deplete `SoilHealth`, consistent with the established precedent
  for slow-timescale mechanics not fabricating false positives. Observatory's new "Ecology" section
  rendered correctly (real Soil/Wildlife numbers) with no console errors. Full verification: build
  clean, 208/208 unit tests, 3/3 fast integration tests, `tsc --noEmit` clean.
