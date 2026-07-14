# RFC-016: Legends & Myths — First Increment (Myth Formation from Aged High-Importance History)

**Status:** Implemented (Week 24, 2026-07-14 - see DEVELOPMENT_PLAN.md Days 117-121)
**Date:** 2026-07-14
**Author:** `DEVELOPMENT_PLAN.md` Week 24
**Governing spec:** `04_Story/TG-STRY-040_Legends_Myths.md`

---

## Why this needs an RFC before a day-to-day plan

`TG-STRY-040`'s Core Principle is *"Memory changes faster than history. Facts remain constant.
Stories evolve."* It names 10 legendary-figure transformations (Explorer → World Founder, General →
Invincible Warrior, Scholar → Great Sage, Physician → Miracle Healer, Inventor → Master Artisan,
Leader → Chosen Monarch), a "Myth Formation" source list (heroes, disasters, wars, discoveries,
exploration, founding events, natural phenomena, remarkable animals, ruins, mysteries), and 8 state
variables (Legendary Status, Historical Accuracy, Myth Evolution, Cultural Significance, Narrative
Longevity, Folklore Diversity, Collective Belief, Historical Distance) — but, consistent with every
other `TG-STRY` document, gives no formula for any of it. This RFC scopes a first, genuinely testable
increment rather than attempting all 8 state variables and 10 transformation categories at once.

## Why now, and why this hook specifically

This codebase already has the two pieces `TG-STRY-040` explicitly depends on: `HistoricalArchive`
(`HistoryManager`, Week 1) stores every archived `HistoricalRecord` with a real `Importance` field
(`SignificanceEvaluator`), and `StoryEngine` (Week 4) already generates a faithful `Story` narrative
from any `HistoricalRecord`. `TG-STRY-040`'s Design Philosophy — *"No civilization intentionally
creates myths. Myths emerge because memory is imperfect"* — is best modeled as something that happens
to *old* history specifically, since "Historical Distance" (a named state variable) is the one concept
this spec ties myth formation to most directly: the closing parable's engineer is remembered accurately
right after the bridge is built, and only becomes a "river spirit's blessing" *generations later*. This
RFC reuses `HistoricalArchive`'s existing `Importance`/`Tick` fields as the trigger, rather than
inventing a new significance model to decide what "deserves" a legend.

## Scope decision: distortion-on-aging for already-High-importance records

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| A new pure in-memory `Legend` entity (`Garden.World/Entities/`, following the `Story` entity's own shape — not EF-persisted): `SourceRecordId`, a real **distorted** narrative (not the faithful `StoryEngine` one), `LegendaryStatus` (0-100, invented — grows over time), `FormedTick` | Historical Accuracy/Myth Evolution/Cultural Significance/Narrative Longevity/Folklore Diversity/Collective Belief as their own distinct tracked state variables — `TG-STRY-040` names these but gives no formula, out of this increment's budget |
| `LegendSystem`: yearly evaluation. A `HistoricalRecord` becomes eligible once it is both `Importance == "High"` (reusing `SignificanceEvaluator`'s existing output) and older than an invented "Historical Distance" threshold (e.g. 3 in-game years) — modeling *"memory changes faster than history"* as a real age-gated transformation, not an immediate one | A second, independent mythologization of the *same* event producing multiple divergent versions across different settlements ("different civilizations tell entirely different versions") — this increment produces one canonical distortion per source record, not per-settlement variants |
| A **category-keyed distortion template** (invented — no `TG-STRY-040` formula given) that transforms the record's faithful description into an exaggerated one, directly implementing the spec's own named transformations (e.g. a `Death`-category record about a renowned figure becomes "a legendary figure said to have achieved the impossible"; a `Disaster` becomes attributed to "the will of unseen forces"; a `Discovery` becomes "a secret whispered by the world itself") | A genuinely generative/compositional narrative engine (grammar-based or LLM-backed) — this increment uses a fixed, honest template set per category, the same "invented but candid" posture every prior RFC's formulas have used |
| `LegendaryStatus` grows by an invented fixed amount each year a `Legend` exists (capped at 100) — modeling "legends grow with retelling" as a simple monotonic accumulation, the simplest defensible version of "Narrative Longevity" without inventing that as a separate tracked variable | Legends fading/being forgotten, folklore merging, "living folklore" as bidirectional evolution — `TG-STRY-040`'s own "Living Folklore" section names these, but they require a decay/merge mechanic this increment doesn't add |
| 1 event: `LegendFormedEvent` (new — `TG-STRY-040` doesn't name a specific event list the way other TG-6xx/TG-STRY documents do, so this is invented to match this project's established "every new mechanic gets an observable event" convention) | Any of the 10 named legendary-figure *transformation records* as their own tracked entities (e.g. a `LegendaryFigure` distinct from the underlying `Citizen`) — this increment produces a narrative-level distortion, not a new character-tracking concept |

## Why age-gating on `Importance == "High"` specifically

`TG-STRY-040`'s Relationships section names History, Culture, Language, Religion, Education,
Collective Memory, Character Stories, and Civilization Stories as dependencies — of these, only
`HistoricalArchive`'s `Importance` field is a real, already-computed, per-record number requiring no new
system to invent, the same "reuse an existing field" discipline this whole series has used since
`RFC-004`. Restricting to `"High"` (not `"Medium"`/`"Low"`) keeps this from generating a legend out of
every routine building completion — `TG-STRY-050`'s "consequences, not spectacle" principle, reused
here for a second purpose (deciding what's worth mythologizing, not just what's worth archiving).

## Mechanism

A new `LegendSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, yearly cadence
(`IntervalTicks = SimulationTime.TicksPerYear`, matching every other civilization-scale system's
established cadence).

1. `Legend` (new entity): `SourceRecordId`, `Title`, `DistortedNarrative`, `LegendaryStatus`,
   `FormedTick`. `WorldState.Legends` (new `List<Legend>`, pure in-memory).
2. Each yearly evaluation, scan `HistoricalArchive.Records` for records where `Importance == "High"`,
   `tick - record.Tick >= HistoricalDistanceThreshold` (invented, e.g. 3 years), and no `Legend` already
   exists for that `SourceRecordId`.
3. For each newly-eligible record, generate `DistortedNarrative` via a category-keyed template
   (`GenerateDistortion`, invented mapping per `HistoryCategories`), create the `Legend`, and publish
   `LegendFormedEvent`.
4. Every yearly evaluation, every existing `Legend`'s `LegendaryStatus` increases by an invented fixed
   amount (e.g. +4/year), capped at 100 — no event fires on this growth alone (per this series'
   established "don't event every tick of a smooth accumulator" discipline; only formation is an event).
5. `LegendFormedEvent` subscribed to `HistorySystem` **at introduction time** (`HistoryCategories.Culture`
   — a legend forming is itself a cultural event, distinct from (and referencing) the historical record
   it distorts), continuing the practice reinforced Week 12 Day 61.

## Explicitly out of scope for the next cycle

- Historical Accuracy/Myth Evolution/Cultural Significance/Narrative Longevity/Folklore
  Diversity/Collective Belief as distinct tracked state.
- Per-settlement divergent versions of the same legend.
- Legend fading/forgetting, folklore merging.
- A generative (grammar-based or LLM-backed) distortion engine, as opposed to fixed category templates.
- Named legendary-figure entities distinct from the underlying `Citizen`/event participants.

## Open questions for review before implementation starts

1. Should the Historical Distance threshold be a fixed number of years, or scale with how significant
   the record was (a bigger event takes longer to fade into myth, or the opposite — becomes legend
   faster)? (Recommendation: fixed — `TG-STRY-040` gives no basis to weight one way or the other, and a
   fixed threshold is simpler to reason about and test than an invented severity-scaled one.)
2. Should `LegendFormedEvent` be archived under `HistoryCategories.Culture` or a new dedicated category?
   (Recommendation: reuse `Culture` — this codebase already treats cultural traits/festivals under that
   category, and a new category for exactly one event type would be premature ahead of any other
   Culture-adjacent legend/folklore event actually needing to share it.)

## Implementation notes (Week 24, added at close-out)

Shipped as specified, with both open questions resolved as recommended (fixed Historical Distance
threshold, `HistoryCategories.Culture` reused rather than a new dedicated category).

- `Legend` (new pure in-memory entity, `WorldState.Legends`) + `LegendSystem` (yearly cadence)
  implemented exactly per the mechanism: `HistoricalDistanceYears = 3`,
  `LegendaryStatusGrowthPerYear = 4.0` (both invented, documented as such). `GenerateDistortion`
  implements the category-keyed template table for `Death`/`Disaster`/`Discovery`/`War`/`Settlement`/
  `Building`, with a generic fallback for every other category.
- `LegendFormedEvent` subscribed to `HistorySystem` at introduction time (`HistoryCategories.Culture`,
  severity 5.0).
- **A minor phrasing bug was caught during live verification, not by a unit test**: the `Building`
  distortion template originally interpolated `record.Title.ToLowerInvariant()` directly (e.g. "house
  completed"), producing the grammatically awkward "people came to believe house completed was raised
  overnight." Fixed by dropping the title interpolation entirely in favor of a generic "it was raised
  overnight" — the template doesn't need to name the specific building type to convey the myth.
- Tests: 6 new `LegendSystemTests` (no-legend-while-recent, legend-forms-once-old-enough,
  low-importance-never-becomes-a-legend, no-duplicate-legend-per-record, event-publishes-on-formation,
  legendary-status-grows-and-caps-at-100) plus 1 `HistorySystem` regression test.
- Observatory: `CivilizationController` gained a `GET /civilization/legends` endpoint joining each
  `Legend` with its source `HistoricalRecord` (via `HistoricalArchive.GetById`, newly injected into the
  controller) so the Observatory can show the myth and the fact side by side, per `TG-STRY-040`'s
  "Legends never overwrite objective history. They exist alongside it." `CivilizationPage.tsx` gained a
  "Legends" tab following the exact same tab/query pattern as every other Civilization sub-view.
- Live verification: resumed the same Year-4, 8-settlement world used for RFC-015's check. **46 organic
  `LegendFormed` records had already archived by Year 5**, with real category-appropriate distortions
  visible via `/civilization/legends` — e.g. "The Legend of Ulric Fernwood Has Passed" ("They say Ulric
  Fernwood did not truly die, but passed into legend...") generated from a real `CitizenDied` record,
  and multiple "The Legend of House Completed" legends (one per settlement/building, each correctly
  paired with its own `originalTitle`/`originalDescription` via the source-record join). This is a
  genuine, confirmed organic finding, not a non-finding — the mechanism fired exactly as designed once
  enough High-importance history aged past the 3-year threshold. Full verification: build clean,
  242/242 unit tests, 3/3 fast integration tests, `tsc --noEmit` clean.
