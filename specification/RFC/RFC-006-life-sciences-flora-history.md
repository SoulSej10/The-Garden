# RFC-006: Life Sciences — Flora History (First Increment)

**Status:** Implemented (Week 10, 2026-07-10 - see DEVELOPMENT_PLAN.md Days 46-50)
**Date:** 2026-07-10
**Author:** DEVELOPMENT_PLAN.md (Week 9 close-out backlog triage)
**Governing spec:** `03_Sciences/02_Life/TG-200_Biology_Foundations.md`, `TG-210_Flora.md`

---

## Why this needs an RFC before a day-to-day plan, and why it looks different from RFC-001 through RFC-005

Volume IV (Biological Sciences) is ten documents (`TG-200` through `TG-290`) and is explicitly
the backlog's largest single greenfield item: "`AgricultureSystem`/`CitizenSystem` currently
hardcode biology this volume is supposed to own — untangling that is itself a design
question." That untangling question is real and is **not** resolved by this RFC. Unlike
RFC-001 through RFC-005 (each a clean, additive pairwise system with no existing code to
reconcile), this first increment instead found and reused **already-existing, already-firing
code that nothing currently observes** - closer in spirit to Week 7's anomaly cleanup than to
a from-scratch RFC.

## What was found: a second silent Law IV gap, bigger than the Week 7 one

Investigating before scoping (rather than assuming Volume IV has zero footprint, the way
prior RFCs' governing specs did) found that `Garden.Core.Events.EnvironmentalEvents.cs`
already defines nine `EnvironmentalEvent` types (`RainStarted`/`Stopped`, `SeasonChanged`,
`RiverExpanded`/`Shrank`, `LakeDried`, `ForestExpanded`/`Declined`, `ResourceRegenerated`,
`DroughtStarted`), and `EcologySystem`/`ResourceSystem` already publish several of them from
real, already-gated conditions:

- `ForestExpandedEvent`: fires when a Plains/Grassland tile with `growthPotential > 0.6` rolls
  under a `0.005 * growthPotential` chance, or spreads to a suitable neighbor tile - a real,
  rare, meaningful transition, not routine noise.
- `ForestDeclinedEvent`: fires when a Forest tile's moisture drops below 0.15 and temperature
  exceeds 30 - again rare and meaningful (drought-driven deforestation).
- `ResourceRegeneratedEvent`: fires whenever a tile's `Trees`/`WildPlants`/etc. resource
  deposit regenerates by more than 1 unit - this one is genuinely high-frequency (every 24
  ticks, across every tile with a regenerating deposit) and would flood the archive the same
  way Week 4 Day 18 found `FarmHarvested` doing, if archived unconditionally.

**`HistorySystem` subscribes to zero `EnvironmentalEvent` types** - confirmed by grep. This is
the same TG-001 Law IV violation Week 1 Day 1 closed for civilization events, and Week 7
Day 31 re-closed for `DialectFormedEvent`, except this one predates this entire development
cycle rather than being introduced by it.

Separately, real consumption of these same resources already happens today:
`CitizenSystem` has citizens forage `WildPlants` for food and gather `Trees` ("Wood") for
construction - so both sides of TG-210's "plants grow, plants are consumed" cycle are real
and already running, just invisible to history.

## Scope decision: archive the two rare, meaningful flora events; defer the noisy one

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| `HistorySystem` subscribes to `ForestExpandedEvent`/`ForestDeclinedEvent` - both map directly onto TG-210's own named events (`ForestExpanded`, and a decline counterpart to `ForestRegenerated`) | `ResourceRegeneratedEvent` - too high-frequency to archive unconditionally without repeating the Day-18 `FarmHarvested` mistake; would need its own significance-scaling pass first |
| — | The other seven `EnvironmentalEvent` types (`RainStarted`/`Stopped`, `SeasonChanged`, `RiverExpanded`/`Shrank`, `LakeDried`, `DroughtStarted`) - real gaps too, but out of this RFC's Flora-specific scope; worth a follow-up covering Climate/Hydrology (`TG-1xx`) instead |
| — | `AgricultureSystem`'s hardcoded crop growth - the actual "untangling" question this backlog item names; not touched here |
| — | Any new entity, individual plant identities, Fauna (`TG-230`), Population Ecology (`TG-240`), Disease (`TG-260`), Evolution (`TG-250`) - all of Volume IV beyond this one slice |

This increment adds **zero new state, zero new entities, and zero new systems** - it is
purely a `HistorySystem` subscription change plus a `SignificanceEvaluator` weighting
decision, the smallest possible real slice of Volume IV's Law IV gap.

## Mechanism

Two new `HistorySystem` handlers, mirroring the existing `OnCulturalFestivalHeld`/
`OnDialectFormed` pattern:

1. `OnForestExpanded`: archived under a new `HistoryCategories.Nature` category (none of the
   existing categories fit a biological/ecological event - `Discovery`/`Harvest` are
   civilization-scoped). Moderate severity (~5.0, comparable to `CulturalFestivalHeld`'s 4.5) -
   a forest expanding is locally notable, not civilization-shaking.
2. `OnForestDeclined`: same category, similar severity - forest decline from drought is a
   real environmental event worth remembering, consistent with TG-210's own framing of forests
   as multi-generational actors ("forests reclaim [cities] in generations... plants are
   patient").

`ResourceRegeneratedEvent` is explicitly **not** subscribed in this increment - archiving it
unconditionally would flood the archive with routine regrowth ticks, the exact mistake
Week 4 Day 18 already fixed once for `FarmHarvested`. A future increment could scale its
severity by regrowth magnitude the same way `FarmHarvested` was fixed, if this data proves
useful later.

## Explicitly out of scope for the next cycle

- Everything in the "Deferred" column above.
- Any change to `EcologySystem`'s or `ResourceSystem`'s actual growth/decline logic - this
  RFC only makes existing, already-correct logic observable to history, it doesn't change it.
- The `AgricultureSystem` hardcoding question named in the original backlog entry - still
  open, still needs its own ADR-style decision before any RFC can responsibly touch it.

## Open questions for review before implementation starts

1. Is `HistoryCategories.Nature` the right new category name, or should this reuse
   `HistoryCategories.Event` (the existing generic bucket)? (Recommendation: add `Nature` -
   `Event` is used as a true catch-all elsewhere and a dedicated category lets the History
   Explorer's faceted search (Week 4 Day 17) actually filter on it meaningfully, the same
   reason `Technology`/`Religion`/`Culture` each got their own category in Week 1.)
2. Should severity differ between expansion and decline, given TG-210 frames decline as more
   narratively significant (drought, loss) than routine expansion? (Recommendation: keep them
   equal for this increment - both are equally rare/gated in the current code, and TG-STRY-050's
   own criteria weigh actual consequences, which this increment doesn't yet model either way;
   revisit once real gameplay consequences of forest cover exist.)

## Implementation notes (Week 10, added at close-out)

- Implemented as designed: `HistoryCategories.Nature` added, `HistorySystem` now subscribes
  to `ForestExpandedEvent`/`ForestDeclinedEvent`. Both open questions resolved as recommended.
- 2 new unit tests, plus a third covering a separate finding below. Verified live: 50
  `Nature` records archived within Year 1 of a fresh simulation run, all with correct
  titles/descriptions matching real terrain transitions.
- **A second, more significant, unrelated finding surfaced during live verification**:
  `HistorySystem.Archive()` - the shared method underlying all ~25 event handlers, not just
  this RFC's two - hardcoded `LocationY = locationX + 1`, silently discarding the real Y
  coordinate for every historical record ever archived across all nine weeks of this
  project. No prior test asserted on `LocationY`, which is why it went unnoticed. Fixed by
  giving `Archive()` separate `locationX`/`locationY` parameters and updating all 25 call
  sites; verified live (`locationY` now independent of `locationX`, matching real tile
  coordinates). This fix is unrelated to Flora specifically but was found and fixed here
  since it was directly in the method this RFC's own new handlers call.
- **Day 49 assessment**: Week 11 should not default to continuing Volume IV. The next
  obvious slice (Population Ecology) immediately hits the `AgricultureSystem` hardcoding
  question this RFC deliberately avoided; recommend either the `AgricultureSystem` ADR or
  pivoting to Borders & Territorial Dynamics instead. See `DEVELOPMENT_PLAN.md` Week 10.
