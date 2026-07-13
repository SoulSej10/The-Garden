# RFC-008: Population Ecology — Carrying Capacity (First Increment)

**Status:** Implemented (Week 14, 2026-07-13 - see DEVELOPMENT_PLAN.md Days 67-71)
**Date:** 2026-07-13
**Author:** `DEVELOPMENT_PLAN.md` Week 13 (ADR-002 unblocking assessment)
**Governing spec:** `03_Sciences/02_Life/TG-240_Population_Ecology.md`

---

## Why this needs an RFC before a day-to-day plan

Same reasoning as every prior RFC: `TG-240` is thorough on vocabulary (Carrying Capacity, Population
Pressure, Migration Pressure, Competition Index, Predation Pressure, Community Diversity, Ecological
Stability) and names 10 events (`PopulationBoom`, `PopulationDecline`, `MigrationWave`,
`LocalExtinction`, `PopulationRecovered`, `PredatorExpansion`, `HabitatRecolonized`,
`SpeciesRangeShift`, `CommunityStabilized`, `EcologicalImbalanceDetected`) but gives no formula for
any of them. It is also the single largest-scoped TG-2xx document read so far — predator-prey
dynamics, migration, competition, community ecology, and genetic diversity are all explicitly named
concepts this codebase has zero equivalent state for (no species other than `Citizen` exists at all;
there is no fauna, no wildlife population, no predator/prey relationship anywhere in the code).

## Why Population Ecology, and why now

`ADR-002` (`specification/ADR/ADR-002-agriculture-system-crop-growth-disposition.md`) resolved the
blocker that had sat in the Backlog table since Week 10 Day 49: Population Ecology does **not** need
a real Flora/soil model built first. It can reuse `AgricultureSystem`'s existing `Food` output and
`ReproductionSystem`'s existing `foodPerCapita >= 3.0` / `HasAvailableHousing` gates as its carrying-
capacity inputs, the same "reuse an existing field" posture every RFC since RFC-004 has used.

## Scope decision: settlement-level Carrying Capacity + two named events, detection only

`TG-240` is written entirely in terms of wild ecological populations (predators, prey, migrating
herds, communities) — this codebase has none of that; the only "population" that exists anywhere is
`Settlement.MemberIds` (citizens). This increment does **not** invent a wildlife population system.
It applies `TG-240`'s carrying-capacity concept to the one population that already exists: a
settlement's citizens, exactly as the "Relationship to Civilization" section of `TG-240` itself
frames it ("The same principles governing animal populations will later govern towns, cities,
kingdoms, and civilizations").

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| `Settlement.CarryingCapacity`, derived from existing `Food` storage and existing housing occupancy math | Wildlife/Fauna populations (`TG-230`) — no species other than `Citizen` exists to apply predator-prey, competition, or migration concepts to |
| `PopulationEcologySystem`: monthly recompute; a settlement crossing from under-capacity to over-capacity publishes `PopulationDeclineEvent` (one of TG-240's 10 named events); a settlement with real, sustained population growth while comfortably under capacity publishes `PopulationBoomEvent` (another of the 10) | `MigrationWave`, `LocalExtinction`, `PopulationRecovered`, `PredatorExpansion`, `HabitatRecolonized`, `SpeciesRangeShift`, `CommunityStabilized`, `EcologicalImbalanceDetected` — the other 8 named events, each requiring concepts (migration, predation, community diversity) this increment doesn't model |
| — | Disease (`TG-260`), Evolution (`TG-250`) — separate TG-2xx documents, each needing their own RFC |
| — | Any consequence of crossing carrying capacity beyond what `ReproductionSystem`/starvation already do — no new death/emigration mechanic. `TG-240` says population pressure influences survival; `ReproductionSystem`'s existing food/housing gates and `CitizenSystem`'s existing starvation mechanic already are that influence. This RFC only makes the pressure visible and historically recorded, it doesn't add a second consequence on top |

## Why Food and Housing specifically

`TG-240` names Biome, Climate, Water, Food, Habitat, Season, and Environmental health as carrying-
capacity inputs. Of these, only Food (`Settlement.Storage["Food"]`) and Habitat (housing occupancy,
already computed by `Settlement.HasAvailableHousing`'s underlying sum) are already real, tracked,
per-settlement numbers — the rest have no equivalent state anywhere yet. This mirrors RFC-004 reusing
`Intelligence`, RFC-005 reusing `Legitimacy`, and RFC-007 reusing `Population`/`Legitimacy`.

## Mechanism

A new `PopulationEcologySystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, monthly cadence
(`IntervalTicks = 720`, i.e. `24 * 30` — population dynamics are observable on a monthly timescale,
unlike the civilization-milestone systems' yearly cadence `SimulationTime.TicksPerYear` established
Week 12 Day 58).

1. `HousingCapacity = Buildings.Where(Shelter/House, Completed).Sum(GetMaxOccupants)` (already computed
   inline by `Settlement.HasAvailableHousing` — this RFC exposes the underlying number, not a new
   concept).
2. `FoodCapacity = Storage.GetQuantity("Food") / 3.0` — reusing `ReproductionSystem`'s existing
   `foodPerCapita >= 3.0` threshold verbatim, interpreted as "how many citizens the current food
   supply could support at the same food-security bar reproduction already requires."
3. `CarryingCapacity = Math.Min(HousingCapacity, FoodCapacity)` (invented combination rule — no TG-240
   formula given). Stored on `Settlement` so the Observatory can surface it directly, same posture
   as `TerritorialInfluence`.
4. Each monthly evaluation, compute `Pressure = MemberIds.Count / CarryingCapacity` (or `+infinity` if
   `CarryingCapacity` is 0 and population is nonzero). Compare against the previous evaluation's
   pressure (stored on the system via a `Dictionary<GameEntityId, double>`, not the settlement — same
   "avoid EF migration surface for a recomputable value" reasoning RFC-007 used for its own previous-
   influence tracking):
   - Crossing from `Pressure < 1.0` to `Pressure >= 1.0` (population now exceeds what the settlement
     can sustain) publishes `PopulationDeclineEvent` once per crossing.
   - Crossing from `Pressure > 0.5` to `Pressure <= 0.5` (comfortably under capacity) **while
     population has grown since the last evaluation** publishes `PopulationBoomEvent` once per
     crossing — the growth condition prevents a settlement that simply builds a lot of housing
     (capacity rises, pressure falls, population unchanged) from being misreported as a "boom."
5. Both events subscribed to `HistorySystem` **at introduction time** (`HistoryCategories.Settlement`,
   matching `BorderContracted`/`BorderDisputeBegins`'s precedent of reusing an existing category
   rather than inventing a `Population` one) — continuing the practice established Week 11 and
   reinforced Week 12 Day 61, rather than risking a fifth instance of the TG-001 Law IV gap.

## Explicitly out of scope for the next cycle

- Any wildlife/Fauna population (`TG-230`) — no species other than `Citizen` exists in this codebase.
- Migration, predation, competition, community diversity — all named in `TG-240` but requiring
  concepts (multiple populations, territory contention beyond `TG-620`'s already-shipped model,
  species identity) this increment doesn't add.
- A second consequence mechanic layered on top of crossing carrying capacity — `ReproductionSystem`'s
  gates and `CitizenSystem`'s starvation already are the real consequence; this RFC only names and
  records the state, per TG-STRY-050's "consequences, not spectacle" (the pressure crossing is itself
  a consequence of real food/housing scarcity, not decorative).
- Writing back into `Food`/`HasAvailableHousing` based on `CarryingCapacity` — one-directional read
  only, the same posture every prior RFC in this series has held.

## Open questions for review before implementation starts

1. Is `Math.Min(HousingCapacity, FoodCapacity)` the right combination rule, or should it be a weighted
   average? (Recommendation: keep `Math.Min` — TG-240 frames carrying capacity as a hard ecological
   ceiling ("Unlimited growth is impossible"), and a settlement genuinely cannot sustain more citizens
   than either its beds or its food can support, whichever binds first; averaging would let a
   food-rich, bed-poor settlement look falsely sustainable.)
2. Should `PopulationBoomEvent`/`PopulationDeclineEvent` use `HistoryCategories.Settlement` or a new
   dedicated category? (Recommendation: reuse `Settlement` — these are settlement-level demographic
   events, exactly analogous to `BorderContracted`, and TG-240's own framing treats civilization
   population as governed by "the same principles" as wild populations, not a separate domain deserving
   its own category.)

## Implementation notes (Week 14, added at close-out)

- Implemented as designed: `Settlement.CarryingCapacity` (with a real EF migration,
  `AddSettlementCarryingCapacity`, applied live), `PopulationEcologySystem` (monthly cadence),
  `PopulationDeclineEvent`/`PopulationBoomEvent`, and a minimal per-settlement carrying-capacity
  surfacing in the Observatory. Both open questions resolved as recommended.
- **A real design flaw was found and fixed during implementation, before it shipped**: the first
  draft detected `PopulationBoomEvent` as a pressure-crossing (falling below 0.5), the same shape as
  `TerritorySystem`'s expand/contract logic. Writing the unit tests exposed that this can never fire
  in practice — real population growth *raises* pressure, it never lowers it, so a "pressure fell
  below X" trigger can never coincide with the population actually growing. Redesigned around a
  state transition instead (`isGrowingComfortably`: real growth AND ample headroom this period), with
  its own dedicated `_wasGrowingComfortably` tracking dictionary, so it fires once when that state
  begins rather than never firing at all or refiring every month it continues. This was caught by
  writing the test *before* trusting the implementation, not by a later live-verification surprise.
- **Both new events were subscribed to `HistorySystem` at introduction time**, continuing the
  practice reinforced Week 12 Day 61, rather than risking a fifth instance of the TG-001 Law IV
  pattern.
- 6 new unit tests for `PopulationEcologySystem` plus 2 more for the `HistorySystem` wiring, plus 9
  new tests for `AgricultureSystem` (the Day 63 gap-fill) — 175 total, up from 158.
- Verified live: `CarryingCapacity` computed correctly from real Food/housing data across all 8
  settlements in a resumed simulation — most currently show `0` because most settlements' `Food`
  storage is genuinely `0` right now (the same critically-low-food condition Week 12 Day 59 observed
  independently), which is the mathematically correct output of `Math.Min(housing, 0)`, not a bug.
  One settlement (Littledale) had non-zero `Food` at check time and showed a real non-zero
  `CarryingCapacity` (`0.32 Food / 3.0 = 0.107`) before its food was consumed later in the same run.
  The Observatory's Population section rendered correctly in both states (real numbers and the `X / 0`
  zero-capacity case) with no console errors or crashes. No `PopulationDecline`/`PopulationBoomEvent`
  occurred organically within the verification window — consistent with the Week 11 Day 54 / Week 12
  Day 59 precedent, both are gated by real state transitions that weren't crossed in this particular
  run; the mechanism itself is directly unit-tested, including the specific false-positive RFC-008's
  own open questions worried about (capacity rising without real population growth).
