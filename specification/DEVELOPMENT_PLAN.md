# The Garden — Development Plan

**Derived from:** full read-through of all 84 documents in `specification/` (2026-07-09)
**Duration:** 4 working weeks (20 days), organized as a day-to-day backlog
**Status:** Active
**Owner:** assign per day

---

## Why this scope, and why this duration

The specification library is thorough on *philosophy* (vocabulary, causality rules, event
catalogs) but almost entirely silent on *mechanics* (formulas, thresholds, data types) — see
`SPEC_INDEX.md` for the full finding. That means two categories of work exist, and they
should not be scheduled the same way:

1. **Concrete, already-scoped work** — bugs, doc inconsistencies, and systems where the spec
   is clear enough (or the gap is small enough) to size and schedule day-by-day. This plan
   covers that category in detail, below.
2. **Large greenfield systems** — entire science volumes (Life sciences: Flora/Fauna/Disease/
   Evolution), entire civilization subsystems (Warfare, Borders/Territory), and full parity
   with the Observatory's aspirational UX (multi-scale camera, layered maps, replay/branching,
   modding). These are each multi-week efforts whose actual mechanics don't exist yet even on
   paper. **Do not day-plan these yet.** Each one needs an RFC first (see `RFC/`) to invent the
   missing numbers and get sign-off on scope before a schedule means anything. The backlog at
   the bottom of this document lists them with their required precondition.

4 weeks was chosen because it's exactly enough to (a) close every currently-known
compliance/consistency gap, (b) fix the one broken deployment path and one CI liability found
during the code audit, and (c) land one real, load-bearing cognitive system (Emotion +
Relationships) that everything in the Social/Civilization volumes implicitly depends on but
which has zero code today. Anything past that point is genuinely open-ended and shouldn't be
padded into a false-precision schedule.

---

## Week 1 — Stabilization & Documentation Integrity

Goal: the repo's *documentation* stops lying about the repo's *code*, and the two known
constitutional violations are either fixed or formally acknowledged.

| Day | Task | Spec reference(s) | Deliverable / exit criteria |
|---|---|---|---|
| 1 | Wire `HistorySystem` as a subscriber to civilization events (`LeaderElected`, `KingdomFounded`, etc.) currently published to `IEventBus` but never archived. | `TG-001` Law IV, `TG-DEV-006`, `TG-DEV-008` Known Limitations | Civilization events appear in `HistoricalArchive`; new unit test proves it; existing 40 tests still pass. |
| 2 | Fix `docker-compose build` failure — `src/Garden.Observatory/Dockerfile` COPYs `docker/nginx/nginx.conf` from a path that doesn't exist inside its own build context. | (infra, no TG doc) | `docker-compose up --build` succeeds end-to-end. |
| 3 | Write ADR-001: disposition of `Garden.Contracts`, `Garden.Shared`, `Garden.Story`, `Garden.Simulation`, `Garden.Tools` — all declared in `TheGarden.slnx`, all empty; two are referenced-but-unused, three are fully orphaned. Decide: retire from solution vs. migrate logic in. | `TG-DEV-001`, `TG-DEV-002` (original project responsibilities), `SPEC_INDEX.md` Architecture Overview | `ADR/ADR-001-empty-project-disposition.md` committed; `TheGarden.slnx` updated to match the decision. |
| 4 | Repair the four broken/stale cross-references found during the read-through: `TG-STRY-020`'s dead `TG-440` ref, `TG-STRY-040`'s dead `TG-410`/`TG-420` refs, `TG-570`'s dead `TG-005` ref, `TG-690`'s "Volume VI" mislabel. | `SPEC_INDEX.md` Dependency Graph table | All four docs edited; `grep -r "TG-4[0-9][0-9]"` and `grep -r "Volume VI"` in `04_Story`/`03_Sciences/05_Civilization` return nothing unexpected. |
| 5 | Update `TG-DEV-009`'s stale "Repository Summary" (still shows the deleted `blueprint/` folder; omits `Garden.Contracts/Shared/Simulation/Story/Tools` from its project list). Re-run `dotnet test` and controller count to confirm/correct the "17 tests / 11 controllers" claims. | `TG-DEV-009` | `TG-DEV-009` repository tree and counts match current reality; discrepancies logged in `SPEC_INDEX.md` Change Log. |

---

## Week 2 — Test/CI Health + Quick-Win Social/Political Depth

Goal: PR feedback loop stops being gated by a 7-minute test suite, and the two shallowest
"Social" stubs (Settlement tiering, Governance) get one honest increment of depth without
requiring a full redesign.

| Day | Task | Spec reference(s) | Deliverable / exit criteria |
|---|---|---|---|
| 6 | Profile `Garden.IntegrationTests` (`SurvivalSimulationTests` — 4 tests, 7m20s, each running ~1440 ticks). Identify what's actually slow (tick cost vs. simulated duration vs. setup). | `TG-DEV-000` Engineering Standard (performance priority is last, correctness first — don't over-optimize, just stop blocking CI) | Profiling notes attached to the task; a concrete reduced-duration or sampled variant proposed. |
| 7 | Split CI into a fast unit-test stage (blocking) and a slow integration/simulation stage (nightly or post-merge). | (infra — `.github/workflows/ci.yml`) | PRs get green/red from unit tests in under a minute; integration suite still runs, just not on the critical path. |
| 8 | Add a `SettlementTier` field (Hamlet → Village → Town → City → Regional Capital → Metropolis) derived from population thresholds; surface it in `SettlementsPage.tsx`. | `TG-650_Cities_Urbanization.md` | `Settlement.cs` has the field; thresholds documented inline; Observatory shows the tier. |
| 9 | Extend `GovernanceService` past its current 4-item population lookup table: add an `AuthoritySource` field (competence/tradition/inheritance/election/religious/military, per spec) and a minimal `Legitimacy` score feeding `DiplomacyService`. | `TG-580_Politics_Governance.md` | `GovernanceService` exposes authority source + legitimacy; at least one downstream consumer (diplomacy or civilization stability) reads it. |
| 10 | Write RFC for the Emotion system (Week 3 work) — propose which of the 15 named emotions to implement first, an intensity+decay-half-life model, and integration points into `CitizenSystem`. | `TG-330_Emotion.md` | `RFC/RFC-001-emotion-system.md` committed; reviewed/approved before Day 11 starts. |

---

## Week 3 — Emotion & Relationships Core

Goal: close the single largest true void in the spec-vs-code gap. `TG-330` (Emotion) and
`TG-380` (Relationships) currently have **zero code footprint** despite being load-bearing
dependencies for Communication credibility, Norm compliance, Religion, and Politics
legitimacy — all of which lean on trust/affect that doesn't exist yet.

| Day | Task | Spec reference(s) | Deliverable / exit criteria |
|---|---|---|---|
| 11 | Implement core `EmotionalState` on `Citizen`: start with 6 emotions (Joy, Fear, Sadness, Anger, Trust, Curiosity) as independent intensities per the approved RFC-001. | `TG-330_Emotion.md` | `EmotionalState` type exists; citizens' emotions update per tick from at least one real trigger (e.g. hunger→Fear, social interaction→Joy). |
| 12 | Add per-emotion decay (each emotion decays toward baseline at its own rate — "Startle" fast, "Grief" slow, per spec's own framing). | `TG-330_Emotion.md` | Decay is unit-tested; no emotion is permanently pinned at max/min. |
| 13 | Implement a pairwise `Relationship` entity (Trust, Affection, Social Distance) between two citizens, replacing/augmenting the flat global `Reputation` scalar where two citizens actually interact. | `TG-380_Relationships.md` | `Relationship` persisted per citizen-pair; created/updated on interaction events. |
| 14 | Wire Emotion + Relationship into `CitizenSystem`'s existing decision logic as an additional weighting input (not a rewrite of the whole decision loop). | `TG-350_Decision_Making.md` (explicitly permissive on implementation approach), `TG-DEV-004` | At least one citizen decision (e.g. who to socialize with, whether to help) visibly changes based on Emotion/Relationship state. |
| 15 | Unit + integration tests for Emotion/Relationship; update `TG-330`/`TG-380` status notes (still "Living Document" design docs, but note in `SPEC_INDEX.md` Change Log that a first implementation now exists). | — | Tests pass; Change Log updated. |

---

## Week 4 — Observatory Surfacing + Story/History Fidelity

Goal: make Week 2–3's backend work visible to the player, and close the gap between
`SignificanceEvaluator`'s actual scoring and `TG-STRY-050`'s documented criteria.

| Day | Task | Spec reference(s) | Deliverable / exit criteria |
|---|---|---|---|
| 16 | Surface Settlement tier (Day 8) and Governance/Legitimacy (Day 9) in the Observatory UI properly (not just a raw field dump). | `TG-OBS-002_Interface_Principles.md` (Clarity Before Density, Progressive Disclosure) | Both are visible and explained in context, not just numbers on a page. |
| 17 | Add faceted search to the History Explorer (Person/Family/Settlement/Event/Date/Theme) per spec — currently only basic timeline+search exists. | `TG-OBS-005_History_Explorer.md` | `HistoryController`/`StoriesController` support facet filters; `HistoryPage.tsx` exposes them. |
| 18 | Audit `SignificanceEvaluator` against `TG-STRY-050`'s weighted criteria (duration, breadth, depth, # systems affected, persistence, narrative continuity); close the biggest scoring gap found. | `TG-STRY-050_Dynamic_Historical_Events.md` | At least one previously-mis-scored event class now scores correctly against the documented criteria; documented in code comments why the chosen weights were picked (since spec gives none). |
| 19 | Basic accessibility pass on the Observatory: keyboard navigation, color-independent status indicators, a reduced-motion toggle. | `TG-OBS-008_Performance_Accessibility.md` | All three present; spot-checked manually (no accessibility tooling currently in the stack). |
| 20 | Sprint close: update `SPEC_INDEX.md` Change Log with everything shipped in Weeks 1–4; triage remaining backlog; write RFCs for whichever backlog item comes next. | `SPEC_INDEX.md` | Change Log current; at least one new RFC opened for the next cycle. |

---

## Weeks 1–4 Retrospective (2026-07-09)

All 20 days shipped, tested (build + full unit/integration suite green throughout), and
live-verified against a running/resumed simulation at every step — see `SPEC_INDEX.md`
Change Log for the day-by-day record. Two things worth naming explicitly:

- **Real bugs were only found because live verification was pushed past "the tests pass."**
  The Day-9 EF migration crash, the Day-14 Loneliness-hook placement bug, and the Day-18
  `FarmHarvested` archive-flooding were all invisible to unit tests and only surfaced by
  actually running the app against real data. Keep doing that.
- **Two dormant/latent issues are now tracked but deliberately not fixed**, since neither is
  causing live harm today: `TradeCompletedEvent` is defined and even whitelisted as
  always-High significance, but is never actually published anywhere in this codebase (found
  Week 3 Day 13, re-confirmed Week 4 Day 18). Pick this up whenever a real citizen-to-citizen
  or settlement-to-settlement trade mechanic gets built.

## Backlog — Requires an RFC Before Scheduling

These are real, spec-documented systems with **zero code footprint**, but the specification
gives no formulas/thresholds for any of them — each needs a design pass (via `RFC/`) before
it can get its own day-to-day plan. Listed roughly by dependency order, not priority:

| Backlog item | Spec reference(s) | Why it's not day-planned yet |
|---|---|---|
| ~~Life Sciences foundation~~ | `TG-210`-`TG-260` (Volume IV) | **All six Volume IV foundational documents now have a shipped first increment**: Flora (Week 10, `RFC-006`), Population Ecology (Week 14, `RFC-008`, unblocked by `ADR-002`), Disease & Health (Week 15, `RFC-009`), Evolution & Adaptation (Week 16, `RFC-010`), Decomposers & Microbiology (Week 17, `RFC-011`), Fauna & Animal Behavior (Week 18, `RFC-012`). The first four applied their principle to `Citizen` (the one population that already existed); the last two introduced the minimum new aggregate state (`SoilHealth`, `WildlifePopulation`) needed since no wildlife/microbe population existed at all. Deeper Volume IV work (individual species, predator-prey, genetics) remains open-ended future scope, not tracked as a single backlog row anymore. |
| `HistorySystem.Archive()`'s `LocationY` was always `LocationX + 1` | Week 10 Day 48 finding, **fixed 2026-07-10** | Affected all ~25 `Archive()` call sites (every historical event type), not just this week's new ones — every historical record's Y coordinate was silently wrong since Week 1. No prior test asserted on `LocationY`, which is why it went undetected. Fixed by giving `Archive()` separate `locationX`/`locationY` parameters and updating all 25 call sites; a new regression test locks in the correct behavior. Verified live: `locationY` now genuinely independent of `locationX`. |
| ~~Borders & Territorial Dynamics~~ | `TG-620_Borders_Territorial_Dynamics.md` (scoped via `RFC-007`) | **Scoped for Week 11 (2026-07-10) via `RFC/RFC-007-borders-territorial-influence.md`** — a regional-influence field derived from existing Population/Legitimacy, replacing the flat ever-growing `TerritoryRadius` int with something that can also contract. |
| ~~Warfare & Military Organization~~ | `TG-640_Warfare_Military_Organization.md` (scoped via `RFC-013`) | **Shipped Weeks 19-20 (2026-07-13)** — `Settlement.MilitaryStrength` + a pure in-memory `War` entity + `WarfareSystem` escalates active border disputes between Hostile settlements into declared war, resolves yearly battle attrition, and negotiates peace. Full army/logistics/command-structure organization remains deferred. |
| ~~Infrastructure-as-network~~ | `TG-660_Infrastructure.md` (scoped via `ADR-003`/`RFC-014`) | **Shipped Weeks 21-22 (2026-07-14)** — `ADR-003` resolved the philosophy conflict by extending the already-existing `TradeRoute` entity (route-level `InfrastructureQuality`, growing with sustained trade and decaying with neglect) rather than rewriting `Building`/`ConstructionSystem`. `Building.cs`/`ConstructionSystem` left untouched. |
| ~~Science & Technology redesign~~ | `TG-670_Science_Technology.md` (scoped via `ADR-004`/`RFC-015`) | **Shipped Week 23 (2026-07-14)** — `ADR-004` identified the real defect underneath the "predefined technology tree" framing: `Technology.CurrentProgress`/`IsDiscovered` were shared across every settlement, making Independent Discovery/Parallel Inventions/Technological Divergence structurally impossible. `RFC-015` fixed this with a new per-settlement `SettlementTechnology` entity plus an Intelligence-driven research-contribution factor. |
| ~~Legends & Myths generation~~ | `TG-STRY-040_Legends_Myths.md` (scoped via `RFC-016`) | **Shipped Week 24 (2026-07-14)** — a new `Legend` entity + `LegendSystem` distorts already-High-importance `HistoricalRecord`s once they age past a 3-year Historical Distance threshold, via category-keyed templates matching `TG-STRY-040`'s named transformations. 46 organic legends confirmed live within a Year-5 verification window. |
| ~~Communication~~ / ~~Language~~ / ~~Education~~ / ~~Law & Justice~~ | `TG-500` (scoped, shipped), `TG-510` (scoped, shipped), `TG-550` (scoped, shipped), `TG-590` (scoped, shipped) | **Communication shipped Week 5, Language shipped Week 6, Education shipped Week 8, Law & Justice shipped Week 9 — see `RFC/RFC-002` through `RFC/RFC-005`.** All four Volume VI items originally grouped together are now shipped. |
| ~~`RelationshipSystem` has too narrow a trigger set~~ | Week 8 Day 39 + Week 9 Day 44 findings, **fixed Week 12 Days 56-57 (2026-07-13)** | Two related gaps found back-to-back: (1) `RelationshipSystem`'s only live trigger (`CitizenBornEvent`) bonded a newborn's two *parents*, never the parent and the child — making `EducationSystem`'s mentor/student pairing structurally unreachable; (2) both of `RelationshipSystem`'s triggers applied only *positive* Trust deltas — nothing ever lowered Trust, making `LawSystem`'s dispute detection (`Trust < 20`) equally unreachable. Fixed by also bonding each parent with the newborn (Day 56), and by adding `OnCitizenDied` — a close mourner's Trust drops in their *other* existing relationships (Day 57). 3 new tests. **Live re-verification (Day 59) ran ~3 in-game years and saw neither mechanic fire organically** — no citizen died in that window — consistent with this project's precedent of not fabricating false positives; both mechanisms are directly unit-tested. |
| ~~`TechnologyService` progress-scaling bug~~ | Week 5 Day 22 finding, **fixed 2026-07-10** | `EvaluateTechnology()` accumulated each individual `Technology.CurrentProgress` at `settlementProgress * 0.1`, but nothing else in the codebase treated `settlement.TechnologyProgress` as 10x the per-tech scale — confirmed live, zero technologies discovered after 55+ simulated years. Fixed by removing the scale-down (category multipliers for Agriculture/Construction retained). Verified: 3 new unit tests, and live — a fresh run discovered 10 technologies across 2 settlements within Year 1 alone. |
| ~~`TradeRouteService` never creates routes~~ | Week 6 Day 29 finding, **fixed 2026-07-10** | Root cause: once a route existed for a settlement pair (active or not), `EvaluateTradeRoutes()`'s `existing != null` check unconditionally skipped re-evaluating that pair forever — so a route that went quiet once (an ordinary occurrence) permanently locked that pair out of trading again, even when a fresh surplus/scarcity later appeared. A secondary bug was found alongside it: goods always flowed a fixed direction regardless of which settlement actually held the surplus. Fixed by letting an inactive route reactivate against a newly-found trade good, and by determining flow direction from the actual surplus holder. Verified: 3 new unit tests (124 total) including an exact reproduction of the live numbers reported (Food 74 vs 0, 23 tiles apart) and a reactivation-after-abandonment test. **Live re-verification was inconclusive** — a fresh run's settlements repeatedly sat exactly at the FindTradeGood boundary (Food = 10, needs strictly <10) rather than crossing it, a separate equilibrium detail worth noting but not chased further here. |
| ~~`CivilizationSystem`'s "yearly" cadence isn't a year~~ | Week 6 Day 27 finding, **fixed Week 12 Day 58 (2026-07-13)** | `_lastYearlyTick >= 336` (used by `TechnologyService`/`ReligionService`/`KingdomService`/`CultureService`/`LanguageSystem`/`EducationSystem`/`LawSystem`/`TerritorySystem`) was ~14 days at `SimulationTime`'s actual scale (1 year = 24 × 30 × 12 = 8640 ticks), not a year — affected eight systems' cadence naming at once. Fixed by adding `SimulationTime.TicksPerYear` as a single source of truth and replacing every hardcoded `336` (production code and tests) with it. One test (`LawSystemTests.FailsAsJusticeFailure_WhenLegitimacyIsZero_AfterUnresolvedWindow`) had hardcoded the old value directly and needed updating. |
| ~~`History/search`'s `totalRecords` under-reports the real record count~~ | Week 12 Day 59 finding, **fixed Week 12 Day 61 (2026-07-13)** | Root cause: `HistoricalArchive.Search()`'s `take` parameter defaults to 50, and `HistoryController.Search`'s total-count call never overrode it — so the "total" silently inherited the default page size regardless of how many records actually matched. Fixed by passing `take: int.MaxValue` for that call. Verified live: `totalRecords` now reports 478 against a `pageSize=10` query, not capped at 50. 1 new unit test (`Search_DefaultTake_CapsResultsAt50_EvenWhenMoreRecordsMatch`). |
| ~~`SeasonChangedEvent` never archived by `HistorySystem`~~ | Week 12 Day 61 finding, **fixed same day (2026-07-13)** | Found during this week's leftover sweep: `SeasonChangedEvent` has been published by `SeasonSystem` since before this development cycle began, but — unlike `ForestExpanded`/`Declined`, which shared this exact gap and were fixed Week 10 — it had never been noticed as unsubscribed in `HistorySystem`. A fourth instance of the recurring TG-001 Law IV violation (after Week 1, Week 7's `DialectFormedEvent`, Week 10's Forest events). Fixed by subscribing it under `HistoryCategories.Nature`. 1 new regression test; verified live — a real "Summer Begins" record now archives on the season boundary. |
| ~~`DialectFormedEvent` never archived by `HistorySystem`~~ | 2026-07-10 audit finding, **scheduled Week 7 Day 31** | `HistorySystem` subscribes to all 12 of Week 1's original `CivilizationEvent` types, but `DialectFormedEvent` (added Week 6) was never added alongside them — reproducing the exact TG-001 Law IV ("History Is Permanent") violation Week 1 Day 1 was created to close, this time on new code rather than old. |
| ~~`EmotionalState` never surfaced in the Observatory~~ | 2026-07-10 audit finding, **scheduled Week 7 Day 32** | `Citizen.Emotions` (6 emotions, Week 3 Days 11-12) is returned by `CitizensController.GetCitizen` but `CitizenDetail`/`CitizensPage.tsx` never expose or render it — confirmed via grep, zero references anywhere in `Garden.Observatory`. Week 4 Day 16 surfaced Settlement tier/Governance but nothing ever covered surfacing Emotion on the Citizen page itself. |
| ~~`Relationship` data never surfaced in the Observatory~~ | 2026-07-10 audit finding, **scheduled Week 7 Day 33** | `CitizensController` has a dedicated `GET /citizens/{id}/relationships` endpoint (Trust/Affection/SocialDistance per pair, Week 3 Day 13) that the frontend never calls — confirmed via grep, no "relationship" reference in `CitizensPage.tsx` or anywhere else in the Observatory beyond an unrelated `tradeRelationships: unknown` placeholder field. |
| ~~Replay & Timeline Branching~~ | `TG-OBS-007_Save_Load_Replay.md` (scoped via `RFC-017`) | **Shipped Week 25 (2026-07-14)** — fixed two real save/load fidelity bugs (`LoadAsync` never restored `WorldState.CurrentTime` or most civilization-level collections) and added save lineage (`Id`/`ParentSaveId`) so the Observatory can show which save each world continued from. Full replay/scrubbing playback remains deferred. |
| ~~Modding & Extensibility~~ | `TG-OBS-009_Modding_Extensibility.md` (disposition via `ADR-005`) | **Formally deferred Week 26 (2026-07-14)** — `TG-OBS-009` contains no concrete data model, formula, or named event to build against (unlike every other TG-### document this project has shipped an increment for), so `ADR-005` declines to invent scope prematurely rather than fabricating a placeholder feature. |
| ~~Real LLM-backed AI narrator~~ | `TG-DEV-009` Known Limitations (scoped via `RFC-018`) | **Shipped Week 27 (2026-07-14)** — `IAiNarrator`/`AnthropicNarrator` grounded strictly in facts `NarrationService` already computes, falling back to the unchanged template narrative whenever no `AI:ApiKey` is configured or the request fails. |
| ~~API rate limiting & versioning~~ | `TG-DEV-009` Known Limitations (scoped via `RFC-018`) | **Shipped Week 27 (2026-07-14)** — a global per-IP rate limiter and a `v1` route-prefix convention, plus two real production-routing bugs found and fixed alongside it (`nginx.conf`'s `/api/`/`VITE_API_URL` mismatch, missing `/simulationHub` proxy). |
| `TradeCompletedEvent` is dead code | Week 3 Day 13, Week 4 Day 18 findings | Defined and even whitelisted as always-High significance, but never published anywhere. Not urgent (nothing currently depends on it firing), but worth either wiring a real trade trigger or removing the unused event type so it stops looking implemented. |

## Week 5 (2026-07-10, complete) — Communication: Knowledge Diffusion

Committed day-to-day plan, scoped from `RFC/RFC-002-communication-knowledge-diffusion.md`,
mirroring Week 3's Emotion/Relationship cadence (a new entity + a new `IScheduledSystem` +
tests + minimal UI + close-out) since RFC-002 explicitly reuses that shape as its template.

| Day | Task | Status |
|---|---|---|
| 21 | `Citizen.KnownEventIds` data model + EF Core migration | Done — caught and fixed a real bug live: the initial migration failed against the populated database (`NOT NULL` array column with no default), fixed with `defaultValueSql: "ARRAY[]::text[]"` |
| 22 | `CommunicationSystem`: subscribe to civilization milestone events, daily propagation loop over the `Relationship` graph (SocialDistance < 40, Trust > 30 gate per RFC-002) | Done — also fixed a real gap: `TechnologyDiscoveredEvent`/`ReligionEstablishedEvent` never exposed the discoverer/founder citizen ID that the underlying services already computed; added `DiscoveredByCitizenId`/`FounderCitizenId` |
| 23 | Unit tests for initial knowledge marking and propagation gating (distance/trust/dead-citizen negative cases) | Done — 9 new tests, `CommunicationSystemTests.cs` |
| 24 | Minimal Observatory surfacing: read-only "what this citizen knows" in citizen detail panel | Done — `CitizensController` + `CitizenDetailPanel`, verified live in browser |
| 25 | Close-out: changelog, RFC-002 status update, full verification, commit/push | Done |

**Live-verification note:** organic milestone events (Kingdom/Religion/Technology) could not
be reliably triggered within the session's time budget — Kingdom/Religion formation are
chance-gated per year, and Technology discovery is currently unreachable in practice due to
an unrelated pre-existing bug (see Backlog table below). Verified instead via
`CommunicationSystemTests.cs` publishing the domain events directly, the same approach
`EmotionSystemTests.cs` used for RFC-001's rare emotion triggers — consistent with this
project's standing rule that emergent-only verification isn't a substitute for direct tests
when the emergent path is rare or broken.

## Week 6 (2026-07-10, complete) — Language: Settlement Divergence

Committed day-to-day plan, scoped from `RFC/RFC-003-language-divergence.md`, mirroring
Week 5's shape (a new pairwise entity + a new yearly `IScheduledSystem` + tests + minimal
UI + close-out) since RFC-003 explicitly follows that same template.

| Day | Task | Status |
|---|---|---|
| 26 | `LanguageDivergence` entity + `LanguageSystem` skeleton, wired into DI/scheduler | Done |
| 27 | Divergence mechanic: decay toward convergence with active `TradeRoute`/`DiplomaticRelation` contact, growth toward divergence under isolation, `DialectFormedEvent` at threshold | Done — implemented alongside Day 26 |
| 28 | Unit tests for convergence/divergence gating and the one-time `DialectFormed` firing | Done — 6 new tests, `LanguageSystemTests.cs` |
| 29 | Minimal Observatory surfacing: each settlement's most-diverged/most-converged neighbor | Done — `SettlementsController` + settlement detail panel, verified live (no crash, correct absence when no data) |
| 30 | Close-out: changelog, RFC-003 status update, full verification, commit/push | Done |

**Live-verification note:** organic contact (the precondition for any `LanguageDivergence`
row to exist at all) could not be triggered within the session's time budget — diplomatic
relations require settlements within 15 tiles (none qualify in this world's geography), and
trade routes turned out to never form at all despite their own conditions being clearly met
(a real, separate, pre-existing `TradeRouteService` bug, flagged via `spawn_task`
`task_b82147bd`, not fixed here). Verified instead via `LanguageSystemTests.cs` publishing
synthetic settlement pairs directly, the same fallback Weeks 5 and this week both used when
the organic trigger is rare or broken.

## Week 7 (2026-07-10, complete) — Anomaly Cleanup

Consolidates the three findings from the 2026-07-10 anomaly audit (see `SPEC_INDEX.md`
Change Log) into a single cleanup week, rather than leaving them as loose backlog rows.
Each is small and contained — no RFC needed, all three follow existing patterns already
established elsewhere in the codebase.

| Day | Task | Status |
|---|---|---|
| 31 | Wire `HistorySystem` to `DialectFormedEvent` (mirrors the other 12 `CivilizationEvent` handlers) so `TG-001` Law IV isn't violated by Week 6's own new event | Done — reuses `HistoryCategories.Culture` (no dedicated Language category exists) |
| 32 | Surface `Citizen.Emotions` in the Observatory's citizen detail panel (mirrors the existing Needs/Attributes/Personality sections) | Done — verified live with real data (Curiosity 35.2, others at baseline) |
| 33 | Surface `GET /citizens/{id}/relationships` in the Observatory's citizen detail panel (mirrors `SettlementsPage`'s Language section added Week 6 Day 29) | Done |
| 34 | Unit tests for the new `HistorySystem` handler; live verification of both new UI sections | Done — 1 new test (125 total); Relationships section correctly renders absent when empty, no console errors |
| 35 | Close-out: changelog, full verification, commit/push | Done |

**Live-verification note:** the Relationships section itself could not be verified against
real (non-empty) data — `Relationship` is pure in-memory (never EF-persisted, same as
`Kingdom`/`DiplomaticRelation`/`TradeRoute`), so every API restart resets it to empty, and
this session's resumed world had every settlement well below the 3.0 food-per-capita
reproduction threshold needed for a new `CitizenBornEvent` to create one — the identical
game-state condition Week 3 Day 13 already documented as a real (not buggy) blocker.
Verified instead via the existing `RelationshipSystemTests.cs` coverage plus confirming the
UI renders cleanly (no crash, no console errors, correct conditional absence) with genuinely
empty data.

## Week 8 (2026-07-10, complete) — Education: Apprenticeship

Committed day-to-day plan, scoped from `RFC/RFC-004-education-apprenticeship.md`, mirroring
Weeks 5-6's shape (a new pairwise entity + a new yearly `IScheduledSystem` + tests + minimal
UI + close-out) since RFC-004 explicitly follows that same template.

| Day | Task | Status |
|---|---|---|
| 36 | `Apprenticeship` entity + `EducationSystem` skeleton, wired into DI/scheduler | Done |
| 37 | Mentor/student pairing (life-stage + Intelligence-gap + `Relationship` gate), gradual Intelligence transfer, `ApprenticeshipStarted`/`Completed` events | Done — implemented alongside Day 36 |
| 38 | Unit tests for pairing gates, transfer, and completion conditions | Done — 8 new tests, `EducationSystemTests.cs` |
| 39 | Minimal Observatory surfacing: a citizen's active apprenticeship (mentor or student role) | Done — `CitizensController` + citizen detail panel, verified live (no crash, correct absence when no data) |
| 40 | Close-out: changelog, RFC-004 status update, full verification, commit/push | Done |

**Live-verification note, and a real finding:** apprenticeships could not be verified
against organic data — not because the trigger is rare, but because it's currently
**structurally unreachable**. `RelationshipSystem`'s only live trigger
(`CitizenBornEvent`) bonds a newborn's two *parents*, never the parent and the child, so
no cross-generation `Relationship` (the precondition for a mentor/student pairing) can ever
exist yet. This is a natural follow-up to `RelationshipSystem` itself, not an
`EducationSystem` bug — noted in the Backlog table below, not fixed here. Verified instead
via `EducationSystemTests.cs`'s synthetic pairs and confirming the Observatory UI handles
empty data cleanly.

## Week 9 (2026-07-10, complete) — Law & Justice: Dispute Resolution

Committed day-to-day plan, scoped from `RFC/RFC-005-law-dispute-resolution.md`, mirroring
Weeks 5-8's shape (a new entity + a new yearly `IScheduledSystem` + tests + minimal UI +
close-out) since RFC-005 explicitly follows that same template.

| Day | Task | Status |
|---|---|---|
| 41 | `LegalCase` entity + `LawSystem` skeleton, wired into DI/scheduler | Done |
| 42 | Dispute detection (`Trust < 20` within a settlement), Legitimacy-gated resolution, `CaseResolved`/`JusticeFailure` events | Done — implemented alongside Day 41 |
| 43 | Unit tests for dispute detection, resolution/failure gating, and Trust restoration on success | Done — 7 new tests, `LawSystemTests.cs` |
| 44 | Minimal Observatory surfacing: a settlement's open/resolved case count | Done — `SettlementsController` + settlement detail panel, verified live (no crash, correct absence when no data) |
| 45 | Close-out: changelog, RFC-005 status update, full verification, commit/push | Done |

**Live-verification note, and a second instance of Week 8's finding:** disputes could not
be verified against organic data. `RelationshipSystem` only ever applies *positive* Trust
deltas (`+3.0` trade, `+15.0` co-parenting) - nothing in this codebase ever lowers Trust, so
a genuine dispute (`Trust < 20`) is currently just as structurally unreachable as Week 8's
mentor/student pairing gate, for the same root cause: `RelationshipSystem`'s narrow trigger
set. Noted in the Backlog table alongside Week 8's parent-child finding. Verified via
`LawSystemTests.cs`'s synthetic disputes and confirming the Observatory UI handles empty
data cleanly.

## Week 10 (2026-07-10, complete) — Life Sciences: Flora History (Increment 1 of Volume IV)

Scoped from `RFC/RFC-006-life-sciences-flora-history.md` - a smaller, differently-shaped
increment than Weeks 5-9's (no new entity/system; wires two already-existing, already-firing
`EnvironmentalEvent` types into `HistorySystem`). This is explicitly **increment 1 of a much
larger Volume IV** - Fauna, Population Ecology, Disease, Evolution, and the
`AgricultureSystem` hardcoding question are all still fully deferred; the original 2-week
budget for "Life Sciences foundation" (Weeks 10-11) covered the whole volume, not this slice.

| Day | Task | Status |
|---|---|---|
| 46 | Add `HistoryCategories.Nature`; wire `HistorySystem` to `ForestExpandedEvent`/`ForestDeclinedEvent` | Done |
| 47 | Unit tests for both new handlers | Done — 2 new tests, `HistorySystemCivilizationEventTests.cs` |
| 48 | Live verification against a running simulation (both events are rare/gated - may need an extended run) | Done — 50 `Nature` records archived live within Year 1 |
| 49 | Re-scope remaining Volume IV backlog: confirm whether Week 11 continues with Fauna/Population Ecology or whether that needs its own RFC first | Done — see assessment below |
| 50 | Close-out: changelog, RFC-006 status update, full verification, commit/push | Done |

**A second, separate, more significant finding surfaced during Day 48 live verification:**
`HistorySystem.Archive()` - the shared method underlying all ~25 event handlers, not just
this week's two new ones - hardcoded `LocationY = locationX + 1`, completely ignoring the
real Y coordinate. This has silently mis-recorded location data for every historical record
ever archived across all nine weeks of this project (Births, Settlements, Kingdoms,
Technology, Religion, everything). No prior test ever asserted on `LocationY`, which is
exactly why it went undetected for so long. Fixed by changing `Archive()`'s signature to
accept `locationX`/`locationY` separately and updating all 25 call sites; a new regression
test (`ArchivedRecord_StoresTheRealLocationY_NotLocationXPlusOne`) locks this in. Confirmed
live: a `ForestExpanded` record's `locationY` (49) now matches its real tile Y, independent
of `locationX` (15) - previously it would have shown 16.

**Day 49 assessment — should Week 11 continue Life Sciences?** No, not without more
groundwork. This week's increment deliberately avoided the actual "untangling" question the
backlog names (`AgricultureSystem`'s hardcoded crop growth), by picking a slice
(`ForestExpanded`/`Declined` history) that didn't need to touch it. The next most obvious
Volume IV slice - Population Ecology (`TG-240`) - explicitly depends on food/population
dynamics that `AgricultureSystem` currently owns outright, meaning it would hit that
untangling question immediately, unlike Flora History. Recommendation: **Week 11 should not
default to more Volume IV.** Two better-shaped options exist in the backlog instead: (a)
write the ADR the backlog already calls for on `AgricultureSystem`'s crop-growth philosophy
(needed regardless of which Volume IV slice comes next), or (b) pick Borders & Territorial
Dynamics (`TG-620`) instead - a single invented decay function on top of the existing
`TerritoryRadius` field, closer in shape to Weeks 5-9's clean additive RFCs than anything
left in Volume IV. This plan defers the final call to whoever picks up Week 11.

**Decision (2026-07-10):** Borders & Territorial Dynamics, via `RFC/RFC-007-borders-
territorial-influence.md`. It doesn't depend on resolving `AgricultureSystem`'s hardcoding
(unlike the next Life Sciences slice would), keeping the same weekly RFC cadence moving
without an open-ended architectural detour. The `AgricultureSystem` ADR stays open in the
Backlog table for whenever someone wants to unblock Population Ecology specifically.

## Week 11 (2026-07-10, complete) — Borders & Territorial Dynamics: Territorial Influence

Committed day-to-day plan, scoped from `RFC/RFC-007-borders-territorial-influence.md`,
mirroring the established shape (new system logic on existing fields + tests + minimal UI +
close-out).

| Day | Task | Status |
|---|---|---|
| 51 | `Settlement.TerritorialInfluence` field + `TerritorySystem` skeleton, wired into DI/scheduler | Done — required a real EF migration (`AddSettlementTerritorialInfluence`), applied and verified live |
| 52 | Influence-driven expand/contract (`BorderContractedEvent`), pairwise dispute detection (`BorderDisputeBeginsEvent`) | Done — implemented alongside Day 51; both events subscribed to `HistorySystem` at introduction time, avoiding a third instance of the Week 7/10 Law IV gap |
| 53 | Unit tests for influence computation, expand/contract thresholds, and dispute detection | Done — 8 new tests, `TerritorySystemTests.cs`, plus 2 more for the `HistorySystem` wiring |
| 54 | Minimal Observatory surfacing: a settlement's `TerritorialInfluence` and any active border disputes | Done — verified live (influence 23.5→24 rounded, matching API exactly; Enter-key activation used since a direct dispatch click was flaky this session, same known false-negative pattern from Week 4 Day 19) |
| 55 | Close-out: changelog, RFC-007 status update, full verification, commit/push | Done |

**Live-verification note:** `TerritorialInfluence` itself was confirmed live with real
Population/Legitimacy data. Neither `BorderContractedEvent` nor `BorderDisputeBeginsEvent`
occurred organically within the verification window - both are gated by real thresholds
that simply weren't crossed in this particular run, not bugs. Verified via
`TerritorySystemTests.cs`'s direct scenarios instead.

## Week 12 (2026-07-13, complete) — Anomaly Cleanup 2: RelationshipSystem + CivilizationSystem Cadence

Consolidates the two open findings still sitting in the Backlog table since Weeks 8-10,
rather than letting them accumulate further - same rationale as Week 7.

| Day | Task | Status |
|---|---|---|
| 56 | Extend `RelationshipSystem` to bond a newborn with both parents (not just parent-to-parent), unblocking `EducationSystem`'s mentor/student gate | Done |
| 57 | Add a real negative-Trust trigger to `RelationshipSystem` (a citizen dying lowers Trust in a close mourner's *other* existing relationships - grief makes someone more guarded generally), unblocking `LawSystem`'s dispute detection | Done |
| 58 | Fix `CivilizationSystem`'s `_lastYearlyTick >= 336` cadence to match `SimulationTime`'s real 8,640-tick year, across all eight affected systems (`TechnologyService`/`ReligionService`/`KingdomService`/`CultureService`/`LanguageSystem`/`EducationSystem`/`LawSystem`/`TerritorySystem`) | Done |
| 59 | Live re-verification: confirm Education/Law & Justice can now trigger organically, and that yearly systems now genuinely run once per in-game year | Done |
| 60 | Close-out: changelog, full verification, commit/push | Done |
| 61 | Leftover consolidation sweep (per user request): re-check the Backlog table and prior RFCs' "not fixed here" notes for anything small and contained still sitting open | Done |

### Days 56-58 actuals (2026-07-13)

- **Day 56**: `RelationshipSystem.OnCitizenBorn` now also bonds each parent with the newborn (same deltas as the parent-parent bond, reused rather than inventing a new profile). Fixed the now-3-relationship `CitizenBorn_CreatesStrongerBond...` test (previously asserted `Assert.Single`) and added `CitizenBorn_AlsoBondsEachParent_WithTheNewborn`.
- **Day 57**: Added `RelationshipSystem.OnCitizenDied` - a citizen's close mourners (existing Relationship, Affection > 60) get Trust lowered on their *other* existing relationships, the first mechanic in the project that can ever push Trust below the neutral baseline. 2 new tests (`CitizenDied_LowersTrust_InSurvivorsOtherRelationships_WhenBondWasClose`, `CitizenDied_DoesNotAffect_RelationshipsOfDistantAcquaintances`).
- **Day 58**: Added `SimulationTime.TicksPerYear` (`24 * 30 * 12 = 8640`) as the single source of truth; replaced every hardcoded `336` in `CivilizationSystem`, `EducationSystem`, `LanguageSystem`, `LawSystem`, `TerritorySystem` (and the tests that simulated yearly ticks) with it. Found and fixed one test (`LawSystemTests.FailsAsJusticeFailure_WhenLegitimacyIsZero_AfterUnresolvedWindow`) that broke because it hardcoded the old 336-tick "year" directly rather than deriving it.
- Full verification after all three days: build 0 warnings/0 errors, 156/156 unit tests, 3/3 fast integration tests (1m15s).

### Day 59 actuals (2026-07-13)

Ran the resumed live simulation at 500x speed for ~3 in-game years (11831 → 25912+ ticks), well past
the 8640-tick real-year boundary the Day 58 fix established. Most settlements showed near-zero food
reserves throughout — a real, non-buggy scarcity condition, not induced. No organic `CitizenDied`,
`ApprenticeshipStarted`, `CaseResolved`, `JusticeFailure`, or `TechnologyDiscovered` event occurred in
that window. This is treated as a legitimate non-finding, not a bug, per the precedent Week 11 Day 54
established for `BorderDispute`'s non-occurrence: these are probability/threshold-gated mechanics
(old-age death chance only above age 70; health-critical death needs sustained zero-food/water; Education/
Law both need specific Relationship states) that simply weren't crossed in this particular ~3-year window,
and every mechanism has direct unit-test coverage confirming it fires correctly when its real trigger
condition is met. **New finding surfaced incidentally**: `/History/search`'s `totalRecords` field
under-reports versus the actual `records` array length (reported `50` while `pageSize=500`/`1000` queries
returned that many real records) — added to the Backlog table, not investigated further (out of scope).
Simulation and `garden-api` preview server stopped cleanly after verification.

### Day 60 close-out (2026-07-13)

Week 12 complete. `SPEC_INDEX.md` Change Log updated with the full Week 12 entry; Backlog table's
`RelationshipSystem` and `CivilizationSystem` cadence rows struck through as fixed, plus the new
`History/search` `totalRecords` finding added as its own row.

### Day 61 actuals (2026-07-13) — leftover consolidation sweep

Per direct user request to check for anything left unconsolidated from prior weeks before moving on.
Re-swept the Backlog table and every RFC's "not fixed here"/"out of scope" notes for anything small,
contained, and still open (not another multi-week greenfield item). Found and fixed two:

- **`SeasonChangedEvent` never archived by `HistorySystem`**: `SeasonSystem` has published this event
  since before this development cycle began, but unlike `ForestExpanded`/`Declined` (the same gap,
  fixed Week 10), nobody had noticed it was never subscribed — a fourth instance of the recurring
  TG-001 Law IV violation. Fixed with a one-line subscription + handler under `HistoryCategories.Nature`.
  1 regression test; verified live — a real "Summer Begins" record now archives on the season boundary
  (confirmed via a fresh simulation run reaching tick 2161).
- **`History/search`'s `totalRecords` under-reporting** (the Day 59 finding): root cause was
  `HistoricalArchive.Search()`'s `take` parameter defaulting to 50, silently applied to the controller's
  total-count call since it never overrode it. Fixed by passing `take: int.MaxValue` for that call.
  1 new unit test; verified live — `totalRecords` now correctly reports 478 against a `pageSize=10` query.

Confirmed via `grep` that no `TODO`/`FIXME`/`HACK` comments exist anywhere in `src/` (this project tracks
findings in `SPEC_INDEX.md`/`DEVELOPMENT_PLAN.md` instead, so this is expected, not a gap). The other
7 `EnvironmentalEvent` types named in RFC-006 (`RainStarted`/`Stopped`, `RiverExpanded`/`Shrank`,
`LakeDried`, `DroughtStarted`/`Ended`) were checked and confirmed still genuinely dead code (never
published anywhere) — wiring them to `HistorySystem` would do nothing, so left as-is pending their own
Climate/Hydrology RFC, exactly as RFC-006 already scoped. `TradeCompletedEvent`'s dead-code status was
also re-confirmed unchanged - fixing it needs an actual citizen-to-citizen trade mechanic invented from
scratch, not a wiring fix, so it stays a backlog item rather than being force-fit into this sweep.

**Week 12 final tally:** 158 unit tests (up from 153), 3 fast integration tests, full solution build
clean (0 warnings/0 errors).

---

## Week 13 (2026-07-13, complete) — The `AgricultureSystem` Crop-Growth ADR

Resolves the Week 10 Day 49 backlog item that had blocked Population Ecology for two full cycles:
"`AgricultureSystem`/`CitizenSystem` still hardcode biology this volume is supposed to own."

| Day | Task | Status |
|---|---|---|
| 62 | Write `ADR-002`: decide whether Population Ecology needs a real Flora-driven crop system first, or can reuse `AgricultureSystem`/`ReproductionSystem`'s existing outputs | Done |
| 63 | Gap-fill found while writing the ADR: `AgricultureSystem` had zero dedicated unit tests | Done |
| 64 | Write `RFC/RFC-008-population-ecology-carrying-capacity.md`, scoping Week 14 | Done |
| 65 | Validate RFC-008's proposed `CarryingCapacity` formula against real running-simulation settlement data | Done |
| 66 | Close-out: changelog, commit/push | Done |

### Days 62-66 actuals (2026-07-13)

- **Day 62**: `ADR-002` accepted — `AgricultureSystem`'s season/moisture yield formula stays as-is (no
  `TG-210` formula exists to conform to either way); Population Ecology will instead reuse
  `AgricultureSystem`'s `Food` output and `ReproductionSystem`'s existing `foodPerCapita >= 3.0` /
  `HasAvailableHousing` gates as its carrying-capacity inputs — the same "reuse an existing field"
  posture every RFC since RFC-004 has used.
- **Day 63**: Writing the ADR surfaced a real, previously-unnoticed gap: `AgricultureSystem` has
  existed since early in this project with zero dedicated unit tests, only ever exercised indirectly
  through `SurvivalSimulationTests`'s multi-year integration runs. Added `AgricultureSystemTests.cs`
  (6 tests) covering the season growth modifiers, the moisture penalty, the no-seeds-no-yield case,
  and the `FarmHarvestedEvent` payload.
- **Day 64**: `RFC-008` written, scoping `Settlement.CarryingCapacity = Math.Min(HousingCapacity,
  FoodCapacity)` and a new `PopulationEcologySystem` (monthly cadence) that detects crossings into/out
  of sustainable capacity, publishing `PopulationDeclineEvent`/`PopulationBoomEvent` (2 of `TG-240`'s
  10 named events) — both subscribed to `HistorySystem` at introduction time, continuing the practice
  reinforced Week 12 Day 61.
- **Day 65**: Queried a resumed live simulation's real settlement data to sanity-check the formula
  before committing to it in code — confirmed `HousingCapacity`/`FoodCapacity` produce sane,
  non-degenerate numbers against real settlements (not just synthetic test data).
- **Day 66**: `SPEC_INDEX.md` Change Log updated; Backlog table's "Life Sciences foundation" row
  narrowed to reflect Population Ecology Increment 1 now being scheduled (Week 14). Full verification:
  build clean, unit tests passing, fast integration tests passing.

---

## Week 14 (2026-07-13, complete) — Population Ecology: Carrying Capacity (Increment 1 of Volume IV, continued)

Scoped from `RFC/RFC-008-population-ecology-carrying-capacity.md`, mirroring RFC-007's shape (a
derived field + a new `IScheduledSystem` + tests + minimal UI + close-out) since RFC-008 explicitly
follows that template.

| Day | Task | Status |
|---|---|---|
| 67 | `Settlement.CarryingCapacity` field (EF migration) + `PopulationEcologySystem` skeleton | Done |
| 68 | Pressure-crossing detection + `PopulationBoomEvent`/`PopulationDeclineEvent`, `HistorySystem` wired at introduction time | Done |
| 69 | Unit tests for `PopulationEcologySystem` | Done |
| 70 | Observatory surfacing of carrying capacity/population pressure | Done |
| 71 | Close-out: live verification, changelog, full verification, commit/push | Done |

### Days 67-71 actuals (2026-07-13)

- **Day 67**: `Settlement.CarryingCapacity` added with EF migration `AddSettlementCarryingCapacity`
  (applied live); `PopulationEcologySystem` skeleton (monthly cadence, `IntervalTicks = 24 * 30`)
  computing `Math.Min(HousingCapacity, Food/3.0)` each evaluation. Also added `Settlement.HousingCapacity`
  as a real `int` property (previously only exposed as the boolean `HasAvailableHousing`), refactored
  the latter to reuse it.
- **Day 68**: Pressure-crossing detection for `PopulationDeclineEvent` (population exceeds capacity)
  and a state-transition detection for `PopulationBoomEvent` (real growth while comfortably under
  capacity). Both subscribed to `HistorySystem` under `HistoryCategories.Settlement` at introduction
  time, continuing the practice reinforced Week 12 Day 61.
- **Day 69**: Writing `PopulationEcologySystemTests.cs` caught a real design flaw before it shipped —
  the first Boom-detection draft (a pure pressure-crossing model, mirroring `TerritorySystem`) could
  never actually fire, since real population growth raises pressure rather than lowering it. Redesigned
  around a state transition (`isGrowingComfortably`) instead. 6 tests, including a dedicated test for
  the exact false-positive RFC-008's own open questions worried about (capacity rising via a food
  surplus without real population growth must not be misreported as a boom).
- **Day 70**: `SettlementsController.GetById` now returns `CarryingCapacity`; Observatory's settlement
  detail panel gained a "Population" section (progress bar + `population / carryingCapacity` numbers),
  mirroring the Territory section's shape.
- **Day 71**: Live-verified against a resumed simulation across all 8 real settlements —
  `CarryingCapacity` computed correctly (most settlements currently `0` because their real `Food`
  storage is genuinely `0`, the same scarcity Week 12 Day 59 observed; one settlement had real food and
  showed a real non-zero capacity). Observatory Population section rendered correctly in both states,
  no console errors. `RFC-008` marked Implemented with full implementation notes. Full verification:
  build clean, 175/175 unit tests, 3/3 fast integration tests, `tsc --noEmit` clean.

**Week 14 final tally:** 175 unit tests (up from 158), 3 fast integration tests, full solution build
clean (0 warnings/0 errors).

---

## Week 15 (2026-07-13, complete) — Disease & Health: Overcrowding-Driven Infection (Increment 3 of Volume IV)

Scoped from `RFC/RFC-009-disease-health-overcrowding.md` — applies `TG-260`'s disease concept to
`Citizen` (the one population that exists), reusing Week 14's `CarryingCapacity`/`Population`
overcrowding signal directly rather than inventing a separate density concept.

| Day | Task | Status |
|---|---|---|
| 72 | Write `RFC/RFC-009-disease-health-overcrowding.md` | Done |
| 73 | `Infection` entity (pure in-memory) + `DiseaseSystem` skeleton | Done |
| 74 | Onset/progression/recovery/death mechanic + `EpidemicStarted`/`Contained` crossing detection, `HistorySystem` wired at introduction time | Done |
| 75 | Unit tests for `DiseaseSystem` | Done |
| 76 | Observatory surfacing (settlement Health section) + close-out: live verification, changelog, commit/push | Done |

### Days 72-76 actuals (2026-07-13)

- **Day 72**: `RFC-009` written — overcrowded settlements (`Population >= CarryingCapacity`) risk
  citizen infection; severity damages the existing `Needs.Health`; death reuses the existing
  `CitizenDiedEvent` rather than a parallel path.
- **Day 73**: `Infection` entity added (`Garden.World/Entities/Infection.cs`), following the exact
  `LegalCase`/`Apprenticeship` pure-in-memory pattern — no EF migration needed. `DiseaseSystem`
  skeleton (daily cadence) registered in DI/scheduler.
- **Day 74**: Onset (small daily infection chance in overcrowded settlements), progression (severity
  growth damages `Needs.Health`, recovery chance scaled by current health), and death (severity
  reaching max reuses `CitizenDiedEvent`, cause `"Disease"`) implemented, plus settlement-level
  epidemic crossing detection (`EpidemicStarted`/`ContainedEvent`). All 4 events subscribed to
  `HistorySystem` at introduction time.
- **Day 75**: 7 new `DiseaseSystemTests`. **A real archiving bug was caught by these tests, not live
  verification**: `OrganismInfected`/`DiseaseRecovered` were first archived at severity `4.0`, which
  `SignificanceEvaluator` silently classifies as "Low" (not archived) since it requires `> 4.0` for
  "Medium" — bumped to `5.0` and the regression tests then passed correctly.
- **Day 76**: Added a settlement "Health" section (active infection count) to the Observatory. Live
  verification against a resumed simulation run to Year 2: organic events fired copiously — 514
  `OrganismInfected`, 436 `DiseaseRecovered`, 39 `EpidemicStarted` records across the run; two real
  settlements (Deepmill, Olddale) showed live active infections in the Observatory UI with no console
  errors.

---

## Week 16 (2026-07-13, complete) — Evolution & Adaptation: Adaptive Drift Detection (Increment 4 of Volume IV)

Scoped from `RFC/RFC-010-evolution-adaptive-drift.md` — observes the population-level attribute drift
that `ReproductionSystem`'s inheritance-with-variance and `CitizenSystem`'s differential survival
already produce, rather than adding a second selection mechanic.

| Day | Task | Status |
|---|---|---|
| 77 | Write `RFC/RFC-010-evolution-adaptive-drift.md` | Done |
| 78 | `EvolutionSystem` skeleton (yearly cadence, per-settlement average-Attribute tracking) | Done |
| 79 | `AdaptiveShiftObserved`/`EvolutionaryStagnation` detection, `HistorySystem` wired at introduction time | Done |
| 80 | Unit tests for `EvolutionSystem` | Done |
| 81 | Observatory surfacing (settlement Adaptation section) + close-out: live verification, changelog, commit/push | Done |

### Days 77-81 actuals (2026-07-13)

- **Day 77**: `RFC-010` written — yearly per-settlement average of `Citizen.Attributes`, compared
  against the prior year's average, no new selection mechanic (the existing inheritance/survival
  already are the selection pressure `TG-250` calls for).
- **Day 78**: `EvolutionSystem` skeleton added (yearly cadence, `SimulationTime.TicksPerYear`), no new
  `Settlement`/`Citizen` field — averages tracked only in the system's own in-memory dictionaries.
- **Day 79**: Attribute-shift detection (`AdaptiveShiftObservedEvent`, threshold 0.5) and multi-year
  stagnation detection (`EvolutionaryStagnationEvent`, 3 consecutive stagnant years) implemented, both
  subscribed to `HistorySystem` at introduction time.
- **Day 80**: 5 new `EvolutionSystemTests`. **A subtle correctness bug was caught before it shipped**:
  the first draft counted a settlement's very first yearly evaluation (no baseline to compare against)
  as "stagnant," which could have fired `EvolutionaryStagnationEvent` after only 3 calls on a
  brand-new settlement rather than 3 real years of genuine stagnation. Fixed by skipping the
  stagnation counter entirely on the baseline-establishing year.
- **Day 81**: Added a settlement "Adaptation" section (live Attribute averages) to the Observatory.
  Live verification against the same resumed simulation run: 37 organic `AdaptiveShiftObserved`
  records by Year 2, real diverging Attribute averages visible per settlement in the Observatory UI
  (e.g. Deepmill: Strength 3.7, Perception 3.1 vs. Littledale: Strength 4.09, Perception 7.71), no
  console errors. Full verification: build clean, 193/193 unit tests, 3/3 fast integration tests,
  `tsc --noEmit` clean.

**Week 15-16 combined final tally:** 193 unit tests (up from 175), 3 fast integration tests, full
solution build clean (0 warnings/0 errors).

---

## Week 17 (2026-07-13, complete) — Decomposers & Microbiology: Soil Health (Increment 5 of Volume IV)

Scoped from `RFC/RFC-011-decomposers-soil-health.md` — feeds `Settlement.SoilHealth` from organic
matter that already-existing `CitizenDied`/`ForestDeclined` events produce, depleted by the existing
`FarmHarvestedEvent`, and writes the result back into `AgricultureSystem`'s yield — the first RFC in
this series to feed back into an earlier system's formula rather than staying strictly read-only.

| Day | Task | Status |
|---|---|---|
| 82 | Write `RFC/RFC-011-decomposers-soil-health.md` | Done |
| 83 | `Settlement.SoilHealth` field (EF migration) + `DecomposerSystem` skeleton | Done |
| 84 | Organic-matter accumulation/decomposition mechanic + `AgricultureSystem` feedback, `HistorySystem` wired at introduction time | Done |
| 85 | Unit tests for `DecomposerSystem` | Done |
| 86 | Observatory surfacing (settlement Ecology section) + close-out: live verification, changelog, commit/push | Done |

### Days 82-86 actuals (2026-07-13)

- **Day 82**: `RFC-011` written — the first RFC in this series to write into an earlier system's
  formula (`AgricultureSystem`'s yield), justified because `TG-220` explicitly names Agriculture as
  directly influenced by decomposition.
- **Day 83**: `Settlement.SoilHealth` added (default `100.0`) with EF migration
  `AddSettlementSoilHealthAndWildlife` (shared with Week 18's `WildlifePopulation` field). **A real
  migration-scaffolding bug was caught before applying it**: `dotnet ef migrations add` generated
  `defaultValue: 0.0` for the new column despite the C# property defaulting to `100.0` — EF Core
  scaffolds the CLR zero-value, not the property initializer. Applying it as-generated would have
  silently reset every existing settlement's soil to "fully depleted." Fixed to `defaultValue: 100.0`
  before running `database update`.
- **Day 84**: `DecomposerSystem` subscribes to `CitizenDied`/`ForestDeclined` (organic matter in) and
  `FarmHarvested` (soil depletion); monthly decomposition converts pending matter into `SoilHealth`
  gain; `AgricultureSystem.ProcessFarm` now multiplies yield by `SoilHealth / 100.0` (a no-op at the
  default). 3 events (`NutrientPulseOccurred`, `OrganicMatterAccumulated`, `WasteFullyDecomposed`)
  subscribed to `HistorySystem` at introduction time.
- **Day 85**: 6 new `DecomposerSystemTests`. **A second real bug was caught by these tests, not live
  verification**: the first `Execute()` draft captured the "previous" `SoilHealth` baseline *after*
  that same evaluation's own decomposition had already changed it, making a real rise on a
  settlement's first evaluation undetectable. Fixed by capturing the baseline before decomposition —
  a test confirming existing `AgricultureSystemTests` are unaffected by the default `SoilHealth`
  passed unchanged.
- **Day 86**: Added an "Ecology" section (Soil Health progress bar + Wildlife number, shared with
  Week 18) to the Observatory. Verified live: all 8 real settlements correctly retained `SoilHealth =
  100` after the migration fix.

---

## Week 18 (2026-07-13, complete) — Fauna & Animal Behavior: Aggregate Wildlife Population (Increment 6 of Volume IV)

Scoped from `RFC/RFC-012-fauna-aggregate-wildlife.md` — the first RFC in the Life Sciences series that
cannot reuse `Citizen`; introduces a single aggregate `Settlement.WildlifePopulation`, driven by real
`Forest`-terrain habitat within the territory, per `TG-230`'s own Performance Considerations
("aggregate ecological models," not individual animal agents).

| Day | Task | Status |
|---|---|---|
| 87 | Write `RFC/RFC-012-fauna-aggregate-wildlife.md` | Done |
| 88 | `Settlement.WildlifePopulation` field (shared EF migration) + `FaunaSystem` skeleton | Done |
| 89 | Habitat-capacity mechanic + `SpeciesExpanded`/`AnimalDied` detection, `HistorySystem` wired at introduction time | Done |
| 90 | Unit tests for `FaunaSystem` | Done |
| 91 | Observatory surfacing (shared Ecology section) + close-out: live verification, changelog, commit/push | Done |

### Days 87-91 actuals (2026-07-13)

- **Day 87**: `RFC-012` written — explicitly scoped to `TG-230`'s own Performance Considerations
  sentence ("Most animal populations should be simulated using aggregate ecological models"), not the
  document's vivid individual-animal prose.
- **Day 88**: `Settlement.WildlifePopulation` added (default `0.0`); `FaunaSystem` skeleton (monthly
  cadence) computing habitat capacity from `Forest`-tile count within `TerritoryRadius`.
- **Day 89**: Population moves a fraction toward habitat capacity each month; `SpeciesExpandedEvent`
  fires on meaningful growth while under capacity, `AnimalDiedEvent` fires on a meaningful die-off
  (reinterpreted at the aggregate level, documented in `RFC-012`). Both subscribed to `HistorySystem`
  at introduction time.
- **Day 90**: 4 new `FaunaSystemTests`, including a dedicated test confirming deforesting an entire
  territory collapses habitat capacity and triggers a real `AnimalDiedEvent`.
- **Day 91**: Verified live against a resumed simulation run to Year 1: `WildlifePopulation` diverged
  meaningfully across all 8 real settlements by real forest cover (0 to 56), 37 organic
  `SpeciesExpandedEvent` records archived. Observatory's "Ecology" section rendered correctly with no
  console errors. Full verification: build clean, 208/208 unit tests, 3/3 fast integration tests,
  `tsc --noEmit` clean. Both `RFC-011` and `RFC-012` marked Implemented. **Volume IV (Biological
  Sciences) now has a first increment shipped for all six of its foundational documents** (Flora,
  Population Ecology, Disease & Health, Evolution & Adaptation, Decomposers & Microbiology, Fauna &
  Animal Behavior) — the Backlog table's "Life Sciences foundation" row is retired.

**Week 17-18 combined final tally:** 208 unit tests (up from 193), 3 fast integration tests, full
solution build clean (0 warnings/0 errors).

---

## Week 19 (2026-07-13, complete) — Leftover Sweep + Warfare: Dispute Escalation (Part 1)

Per direct user request to proceed with Warfare & Military Organization while ensuring no leftovers
remain from prior weeks. Opens with a full leftover-consolidation sweep (comparing every
`CivilizationEvent`-derived record type against `HistorySystem`'s subscriptions, the same audit shape
Week 12 Day 61 established), then scopes and begins `RFC-013`.

| Day | Task | Status |
|---|---|---|
| 92 | Leftover sweep: audit every event type against `HistorySystem` subscriptions | Done |
| 93 | Write `RFC/RFC-013-warfare-dispute-escalation.md` | Done |
| 94 | `Settlement.MilitaryStrength` field (EF migration) + `War` entity + `WarfareSystem` skeleton | Done |
| 95 | War declaration mechanic (escalates `TerritorySystem.ActiveDisputes` + `Hostile` `DiplomaticRelation`), `HistorySystem` wired at introduction time | Done |
| 96 | Unit tests for war declaration | Done |

### Days 92-96 actuals (2026-07-13)

- **Day 92**: Full sweep found **four real, active events publishing since Week 8 (`ApprenticeshipStartedEvent`/`CompletedEvent`, `RFC-004`) and Week 9 (`CaseResolvedEvent`/`JusticeFailureEvent`, `RFC-005`) with zero `HistorySystem` subscribers** — the same TG-001 Law IV violation found and fixed four times before (Week 1, Week 7, Week 10, Week 12 Day 61), just never caught for these specific events until this sweep. All four confirmed as real, active publishers (not dead code, unlike the other unsubscribed-but-never-published events checked in the same pass — `Drought`/`Rain`/`River`/`Lake` events remain genuinely dead, `CitizenAte`/`Drank`/`Rested`/`Moved` and `ResourceRegenerated` remain deliberately excluded for frequency, matching Week 12 Day 61's findings). Fixed by subscribing all four (`HistoryCategories.Discovery` for Apprenticeship, reusing `CitizenAged`'s precedent for personal-milestone events; `HistoryCategories.Politics` for the Law events, reusing `LeaderElected`'s precedent), with 4 new regression tests.
- **Day 93**: `RFC-013` written — the cleanest hook of any RFC in this series: `TerritorySystem.ActiveDisputes` (`RFC-007`, Week 11) already detects genuine territorial disputes but explicitly deferred any consequence; `DiplomaticRelation.CurrentRelation == Hostile` (pre-existing) is the literal "trust fails" signal `TG-640`'s own Design Philosophy names. This RFC escalates the two into war rather than building military organization from nothing.
- **Day 94**: `Settlement.MilitaryStrength` added (EF migration `AddSettlementMilitaryStrength`) + pure in-memory `War` entity (`LegalCase`/`Apprenticeship` pattern) + `WarfareSystem` skeleton (yearly cadence, constructor-injecting `TerritorySystem` directly for `ActiveDisputes`).
- **Day 95**: War declaration reads `TerritorySystem.ActiveDisputes` and `WorldState.DiplomaticRelations`; `WarDeclaredEvent` (reusing the `"WarDeclared"` string already whitelisted in `SignificanceEvaluator` since Week 1 but never previously published) subscribed to `HistorySystem` under the never-before-used `HistoryCategories.War`.
- **Day 96**: Unit tests confirmed war declaration fires only when both a real dispute and a Hostile relation co-occur, and does not redeclare while already active.

---

## Week 20 (2026-07-13, complete) — Warfare: Battle Attrition & Peace (Part 2)

| Day | Task | Status |
|---|---|---|
| 97 | Battle-resolution mechanic (population/legitimacy damage to the loser) | Done |
| 98 | Peace mechanic (max-battles or critical-population threshold, partial `DiplomaticRelation` restoration) | Done |
| 99 | Unit tests for battle resolution and peace | Done |
| 100 | Observatory surfacing (settlement Military section) | Done |
| 101 | Close-out: live verification, changelog, commit/push | Done |

### Days 97-101 actuals (2026-07-13)

- **Day 97**: Battle resolution weighs each side's win probability by relative `MilitaryStrength`; the loser takes real `Population`/`Legitimacy` damage — a genuine consequence, not fabricated. `BattleFoughtEvent` subscribed to `HistorySystem`.
- **Day 98**: Peace triggers on max battles fought or the loser's population dropping critically low; restores some (not all) of the pair's `DiplomaticRelation.RelationScore` — the second RFC in this series (after `RFC-011`) to write back into an earlier system's state, justified because `TG-640` explicitly frames peace as reshaping future politics. `PeaceNegotiatedEvent` subscribed to `HistorySystem`.
- **Day 99**: 7 new `WarfareSystemTests` total. **Two tests originally used a deliberately lopsided population pair to force a one-battle peace, but this violated `TerritorySystem`'s own comparable-influence requirement for dispute detection in the first place** — caught by the tests failing, not a design flaw; fixed by driving both through the max-battles path with comparable populations instead. 3 more regression tests for the `HistorySystem` wiring.
- **Day 100**: Added a settlement "Military" section (Strength number + active-war badges) to the Observatory.
- **Day 101**: **A real deployment bug was caught during live verification**: the EF migration for `Settlement.MilitaryStrength` had never been generated, and the API crashed on startup (`column s.MilitaryStrength does not exist`) — the preview tool itself misleadingly reported "started successfully" even after the process had already crashed; diagnosed directly via `dotnet run`, fixed by generating and applying the missing migration before re-verifying. Verified live against a resumed simulation run to Year 2: `MilitaryStrength` computed correctly from real data across all 8 settlements (36.5-64.5); no `WarDeclared` occurred organically within the ~2-year window — a legitimate non-finding (war requires a rare co-occurrence of an active dispute *and* a Hostile relation), not a bug, consistent with established precedent. Observatory's "Military" section rendered correctly with no console errors. Full verification: build clean, 222/222 unit tests, 3/3 fast integration tests, `tsc --noEmit` clean. `RFC-013` marked Implemented.

**Week 19-20 combined final tally:** 222 unit tests (up from 208 — includes 4 tests for the Day 92
leftover-sweep fix plus `WarfareSystem`'s own coverage), 3 fast integration tests, full solution build
clean (0 warnings/0 errors).

---

## Week 21 (2026-07-14, complete) — Infrastructure: ADR + Route Quality Mechanic

Per direct user request to proceed with the Infrastructure-as-network Backlog item, which had sat
unresolved since Week 10 pending a philosophy-vs-implementation decision (see the Backlog table
above).

| Day | Task | Status |
|---|---|---|
| 102 | Write `ADR-003-infrastructure-network-disposition.md` (resolve `TG-660` vs. `ConstructionSystem`/`Building.cs`) | Done |
| 103 | Write `RFC/RFC-014-infrastructure-route-quality.md` | Done |
| 104 | `TradeRoute.InfrastructureQuality`/`EstablishedTick`/`LastTripTick` fields + `InfrastructureSystem` skeleton | Done |
| 105 | Quality growth/decay mechanic + `TradeRouteService.ExecuteTrip` feedback + `RoadConstructed`/`InfrastructureFailure` events, `HistorySystem` wired at introduction time | Done |
| 106 | Unit tests for `InfrastructureSystem` | Done |

### Days 102-106 actuals (2026-07-14)

- **Day 102**: `ADR-003` decided to extend the already-existing `TradeRoute` entity rather than rewrite `Building`/`ConstructionSystem` into a graph, or invent a parallel `Road`/`Network` entity — `TG-660`'s "network, not isolated structures" framing is satisfied by treating the inter-settlement connection that already exists as the network, continuing this series' "reuse an existing field/entity" discipline (RFC-004 onward).
- **Day 103**: `RFC-014` written, scoping route-level `InfrastructureQuality` growth/decay feeding back into `TradeRouteService.ExecuteTrip`'s transported volume — the third RFC in this series (after `RFC-011`, `RFC-013`) to write into an earlier system's mechanic, not just observe it.
- **Day 104**: `TradeRoute` gained `InfrastructureQuality`/`EstablishedTick`/`LastTripTick`. Confirmed by reading `TradeRoute.cs` that the entity is pure in-memory (no EF mapping) before concluding no migration was needed — deliberately checked given the Week 17/Week 19-20 EF migration near-misses/incidents.
- **Day 105**: `InfrastructureSystem` (monthly cadence) grows quality with trips gained since the last evaluation, decays it for inactive routes, and publishes `RoadConstructedEvent`/`InfrastructureFailureEvent` on threshold crossings with hysteresis (become road at quality ≥50, revert only below 10) to avoid flapping. A first-draft hysteresis implementation was caught as overly convoluted during self-review and rewritten before any test ran against it. Both events subscribed to `HistorySystem` at introduction time (`HistoryCategories.Trade`, severity 5.0 — avoiding the Week 15 "severity 4.0 silently dropped" bug class).
- **Day 106**: 7 new `InfrastructureSystemTests` covering growth, decay, floor-at-zero, and event firing/non-refiring in both directions.

---

## Week 22 (2026-07-14, complete) — Infrastructure: Observatory Surfacing + Close-out

| Day | Task | Status |
|---|---|---|
| 107 | Confirm `TradeRouteServiceTests` regression-free after the `ExecuteTrip` multiplier change | Done |
| 108 | Observatory surfacing: `SettlementsController` `TradeRoutes` list (replacing the `TradeRelationships` placeholder), `SettlementsPage.tsx` Infrastructure section | Done |
| 109 | Full verification: build, unit tests, fast integration tests, `tsc --noEmit` | Done |
| 110 | Live verification via Browser pane preview servers | Done |
| 111 | Close-out: `RFC-014` implementation notes, `DEVELOPMENT_PLAN.md`/`SPEC_INDEX.md`/Backlog/timeline updates, commit/push | Done |

### Days 107-111 actuals (2026-07-14)

- **Day 107**: Confirmed by running `TradeRouteServiceTests` directly — all 3 pre-existing tests pass unchanged, since `InfrastructureQuality` defaults to `0.0` and the multiplier is an exact no-op at that value.
- **Day 108**: `SettlementsController.GetById` gained a `TradeRoutes` list (counterpart settlement, primary good, active status, `InfrastructureQuality`, trip count, total volume), replacing the `TradeRelationships: null` placeholder that had sat there since Week 19-20 — this is route-level data, not a settlement scalar, so it's a list rather than a single field, unlike every prior week's single-field settlement additions. `SettlementsPage.tsx` gained an "Infrastructure" section with a per-route quality progress bar.
- **Day 109**: Full solution build clean (0 warnings/0 errors). Unit tests: 231 passing (up from 222 — 7 `InfrastructureSystemTests` + 2 `HistorySystem` regression tests). Fast integration tests: 3/3 passing. `tsc --noEmit` clean.
- **Day 110**: Live-verified via the Browser pane at 1000x speed. **Legitimate non-finding**: this run's four settlements are all >25 tiles apart (closest pair 46 tiles, farthest 116) — `TradeRouteService`'s pre-existing (Week 6) distance cap — so no trade routes, and therefore no organic `InfrastructureQuality` growth, occurred. The mechanism itself is verified correct via the 7 unit tests; the end-to-end data path (API → Observatory detail fetch → empty-state render) was confirmed by inspecting the real `/settlements/{id}` response and the network request the UI made when opening a settlement's detail panel. No console errors.
- **Day 111**: `RFC-014` marked Accepted with implementation notes filled in. Close-out documentation and commit.

**Week 21-22 combined final tally:** 231 unit tests (up from 222 — 7 `InfrastructureSystemTests` + 2
`HistorySystem` regression tests), 3 fast integration tests, full solution build clean (0
warnings/0 errors), `tsc --noEmit` clean.

---

## Week 23 (2026-07-14, complete) — Leftover Sweep + Science & Technology: Independent Discovery

Per direct user request to proceed with Weeks 23-24 while consolidating leftovers from prior weeks.
Opens with a full leftover-consolidation sweep (the same audit shape Week 12 Day 61/Week 19 Day 92
established, this time comparing *every* `DomainEvent`-derived record type across all four event
files, not just `CivilizationEvents.cs`), then resolves the Week 10 Backlog item on Science &
Technology via `ADR-004`/`RFC-015`.

| Day | Task | Status |
|---|---|---|
| 112 | Leftover sweep: audit every event type (all four event files) against `HistorySystem` subscriptions | Done |
| 113 | Write `ADR-004-science-technology-disposition.md` + `RFC/RFC-015-technology-independent-discovery.md` | Done |
| 114 | `SettlementTechnology` entity + rewrite `TechnologyService.EvaluateTechnology` around per-settlement rows | Done |
| 115 | Intelligence-driven research contribution + `TechnologicalDivergenceEvent`, `HistorySystem` wired at introduction time | Done |
| 116 | Unit tests + Observatory/controller updates for the new per-settlement shape | Done |

### Days 112-116 actuals (2026-07-14)

- **Day 112**: Full sweep across `CitizenEvents.cs`/`CivilizationEvents.cs`/`EnvironmentalEvents.cs`/`SettlementEvents.cs` (68 total event types, vs. the prior sweeps' `CivilizationEvents.cs`-only scope of 37) found one real, active, previously-unnoticed gap: `BuildingPlannedEvent` has been published by `ConstructionSystem.PlanBuilding` since before this development cycle began, and `"BuildingPlanned"` already sat in `SignificanceEvaluator`'s always-Medium whitelist (implying it was always meant to be archived) — but nothing had ever subscribed it in `HistorySystem`. The tenth instance of this exact TG-001 Law IV violation. Fixed with a new `OnBuildingPlanned` handler + 1 regression test. Every other unsubscribed event confirmed to match established precedent: deliberately excluded for tick-level frequency (`CitizenAte`/`Drank`/`Rested`/`Moved`, `ResourceRegenerated`) or genuinely dead code (`Drought`/`Rain`/`River`/`Lake` events, all 0 publish sites confirmed via grep).
- **Day 113**: `ADR-004` identified the real defect underneath `TG-670`'s "predefined technology tree" disclaimer: `WorldState.Technologies` held one shared `Technology.CurrentProgress`/`IsDiscovered` pair per named technology across *every* settlement, making Independent Discovery/Parallel Inventions/Technological Divergence (all named in `TG-670`'s Edge Cases) structurally impossible, not just unmodeled — the first settlement to cross a threshold permanently locked every other settlement out. `RFC-015` scopes the fix: per-settlement state via a new `SettlementTechnology` join entity, plus an Intelligence-driven research-contribution factor implementing `TG-670`'s previously-ignored "Education increases research capacity" rule.
- **Day 114**: `SettlementTechnology` (new pure in-memory entity) replaces the shared `Technology.CurrentProgress`/`IsDiscovered` fields; `Technology.AllTechnologies` remains the static read-only catalog. `TechnologyService.EvaluateTechnology` rewritten around `GetOrCreate(settlementId, technologyName)`.
- **Day 115**: Research contribution now multiplies by `1.0 + (averageIntelligence - 5.0) / 20.0`, reusing the same average-`Intelligence` aggregate `EvolutionSystem` (Week 16) already computes. `TechnologicalDivergenceEvent` fires once per settlement pair whose discovered-technology sets first differ by 3+, subscribed to `HistorySystem` at introduction time.
- **Day 116**: 2 new `TechnologyServiceTests` (independent discovery doesn't lock out another settlement; divergence event fires) plus 1 `HistorySystem` regression test. `CivilizationController.GetTechnology` gained an optional `settlementId` query parameter with a real aggregate fallback for the no-argument case — **a real bug caught before shipping**: the first draft returned `InProgress: null` when no settlement was specified, which would have thrown on the existing Observatory frontend's `data?.inProgress.length` call; fixed with `GetUndiscoveredTechnologiesAggregate()` (furthest-along settlement's progress per technology). 242 unit tests total (up from 235 — includes the Day 112 fix's test).

---

## Week 24 (2026-07-14, complete) — Legends & Myths: First Increment

| Day | Task | Status |
|---|---|---|
| 117 | Write `RFC/RFC-016-legends-myth-formation.md` | Done |
| 118 | `Legend` entity + `LegendSystem` skeleton (Historical Distance gating) | Done |
| 119 | Category-keyed distortion templates + `LegendaryStatus` growth + `LegendFormedEvent`, `HistorySystem` wired at introduction time | Done |
| 120 | Unit tests + Observatory surfacing (`CivilizationPage` Legends tab) | Done |
| 121 | Close-out: live verification, `DEVELOPMENT_PLAN.md`/`SPEC_INDEX.md` updates, commit/push | Done |

### Days 117-121 actuals (2026-07-14)

- **Day 117**: `RFC-016` scopes myth formation as age-gated distortion of already-High-importance `HistoricalRecord`s — reusing `HistoricalArchive`'s existing `Importance`/`Tick` fields as the trigger (the "reuse an existing field" discipline every RFC since RFC-004 has used) rather than inventing a new significance model for what "deserves" a legend.
- **Day 118**: `Legend` (new pure in-memory entity, following `Story`'s own shape) + `LegendSystem` (yearly cadence): a record becomes eligible once `Importance == "High"` and at least 3 in-game years old (invented Historical Distance threshold).
- **Day 119**: Category-keyed `GenerateDistortion` templates directly implement `TG-STRY-040`'s named transformations (a death becomes "passed into legend," a disaster becomes "the will of unseen forces," a discovery becomes "whispered by the world itself"). `LegendaryStatus` grows +4/year, capped at 100. `LegendFormedEvent` subscribed to `HistorySystem` at introduction time (`HistoryCategories.Culture`).
- **Day 120**: 6 new `LegendSystemTests` (age-gating in both directions, importance-gating, no-duplicate-per-record, event publication, status growth/cap) plus 1 `HistorySystem` regression test. `CivilizationController` gained `GET /civilization/legends` (joining each `Legend` with its source record via `HistoricalArchive.GetById`); `CivilizationPage.tsx` gained a "Legends" tab.
- **Day 121**: Live-verified against a resumed Year-4, 8-settlement world (the same one Week 23 verified against). **46 organic `LegendFormedEvent` records had archived by Year 5**, with real category-appropriate distortions confirmed via `/civilization/legends` — e.g. "The Legend of Ulric Fernwood Has Passed" ("They say Ulric Fernwood did not truly die, but passed into legend...") generated from a real `CitizenDied` record, correctly paired with its source record's original title/description via the join. **A minor phrasing bug was caught and fixed during this check, not by a unit test**: the `Building`-category template originally interpolated the record's raw title directly, producing "people came to believe house completed was raised overnight" — fixed to drop the redundant title interpolation. No organic `TechnologicalDivergenceEvent` occurred in the same window — a legitimate non-finding, since this world's population had collapsed to 1 living citizen from a pre-existing, unrelated famine bug (first observed Week 21-22), leaving `technologiesDiscovered` at 0 throughout. Full verification: build clean, 242/242 unit tests, 3/3 fast integration tests, `tsc --noEmit` clean. Both `RFC-015` and `RFC-016` marked Implemented.

**Week 23-24 combined final tally:** 242 unit tests (up from 231 — 3 leftover-sweep/`TechnologyService`
tests + 6 `LegendSystemTests` + 2 `HistorySystem` regression tests), 3 fast integration tests, full
solution build clean (0 warnings/0 errors), `tsc --noEmit` clean.

---

## Week 25 (2026-07-14, complete) — Save/Load Fidelity + Timeline Branching

Per direct user request to proceed with Weeks 25-26 while consolidating leftovers. The leftover sweep
(comparing every event type across all four event files against `HistorySystem`'s subscriptions, same
audit shape as Week 23) found **zero new gaps** this time — every event introduced since Week 23
(`TechnologicalDivergenceEvent`, `LegendFormedEvent`) was already wired at introduction time, and the
remaining unsubscribed events all match established precedent (deliberately excluded for frequency, or
genuinely dead code).

| Day | Task | Status |
|---|---|---|
| 122 | Leftover sweep (zero new gaps found) + write `RFC/RFC-017-save-load-timeline-branching.md` | Done |
| 123 | Fix `SaveLoadService.LoadAsync`: restore `WorldState.CurrentTime` + every missing civilization-level collection | Done |
| 124 | `WorldSnapshot.Id`/`ParentSaveId` + lineage tracking (`GetTimeline()`) | Done |
| 125 | Unit tests for `SaveLoadService` (previously zero) | Done |
| 126 | Observatory Timeline surfacing + close-out: live verification, `DEVELOPMENT_PLAN.md`/`SPEC_INDEX.md` updates | Done |

### Days 122-126 actuals (2026-07-14)

- **Day 122**: The leftover sweep found nothing new to fix — a first for this recurring audit, since
  every RFC since Week 19 has subscribed its own events at introduction time. `RFC-017` scopes two real
  defects found while reading `SaveLoadService.cs` in full: `LoadAsync` never restored
  `WorldState.CurrentTime`, and only `Citizens`/`Settlements`/`HistoryRecords` round-tripped through a
  save — every other civilization-level collection (`Kingdoms`, `TradeRoutes`, `Wars`, etc.) was
  silently left at whatever the live world state was, contradicting `TG-OBS-007`'s "the restored world
  should behave exactly as it did."
- **Day 123**: Both defects fixed — `LoadAsync` now sets `WorldState.CurrentTime` from the snapshot's
  tick and clears+repopulates every civilization-level collection.
- **Day 124**: `WorldSnapshot` gained `Id`/`ParentSaveId`; `SaveLoadService` tracks the most recently
  loaded save in-process and stamps it as the next save's parent — the exact "load an earlier save,
  continue, and it becomes a new branch" mechanic `TG-OBS-007`'s own example describes. A naming
  collision with an existing unrelated `TimelineEntry` type was caught immediately at build time and
  resolved by renaming to `SaveTimelineEntry`.
- **Day 125**: 6 new `SaveLoadServiceTests` — the first direct test coverage this service has ever had,
  despite existing since Week 1.
- **Day 126**: `GET /system/timeline` + `ProductionDashboardPage.tsx`'s new Timeline section (rendering
  the branch tree client-side from `parentSaveId` pointers). Live-verified via direct REST calls against
  a resumed 8-settlement world: confirmed a branch save's `parentSaveId` correctly pointed to its root
  save, and confirmed the `CurrentTime` fix specifically by advancing the sim, saving, advancing
  further, then loading and observing the clock reset to the saved tick rather than whatever was live.
  Full verification: build clean, 248/248 unit tests, 3/3 fast integration tests, `tsc --noEmit` clean.
  `RFC-017` marked Implemented.

---

## Week 26 (2026-07-14, complete) — Modding & Extensibility Disposition + Leftover Cleanup

| Day | Task | Status |
|---|---|---|
| 127 | Read `TG-OBS-009` in full; write `ADR/ADR-005-modding-extensibility-disposition.md` | Done |
| 128 | Fix `EconomySystem`: wire the never-published `GoodsCraftedEvent` | Done |
| 129 | Unit tests for the `GoodsCraftedEvent` fix | Done |
| 130 | Fix stale `DEVELOPMENT_PLAN.md` Week 27 backlog line (already-fixed `totalRecords` bug still listed) | Done |
| 131 | Close-out: full verification, `SPEC_INDEX.md` updates, commit/push | Done |

### Days 127-131 actuals (2026-07-14)

- **Day 127**: `TG-OBS-009`, read in full, contains no concrete data model, formula, or named event —
  unlike every other TG-### document this project has built an increment against, it is exclusively
  phrased as *"Future modules may include..."*. `ADR-005` formally defers it (the same disposition
  `ADR-001` gave three genuinely-empty placeholder projects), confirming its one testable claim ("no
  duplicate world states") is already satisfied by the existing single-`WorldState`-singleton
  architecture.
- **Day 128**: `EconomySystem.ProcessProduction` had tracked `_totalGoodsCrafted` since before this
  cycle began, and `"GoodsCrafted"` already sat in `SignificanceEvaluator`'s always-Medium whitelist,
  but `GoodsCraftedEvent` was never actually published anywhere — `HistorySystem`'s `OnGoodsCrafted`
  subscriber existed with nothing to ever call it. Fixed by publishing the event from
  `ProcessProduction`, the same "tracked but never connected" bug class as Week 23's `BuildingPlanned`
  fix.
- **Day 129**: 2 new `EconomySystemTests` (publishes on successful production, does not publish when
  wood is insufficient). `TradeCompletedEvent` (dead code since Week 3 Day 13) was deliberately **not**
  wired up alongside it — it models a different mechanic (citizen-to-citizen barter) this codebase has
  never built, and inventing that mechanic just to give the event a publisher would be scope creep, not
  a genuine leftover fix (documented in `ADR-005`).
- **Day 130**: `DEVELOPMENT_PLAN.md`'s Week 27 timeline row still listed *"`History/search`
  `totalRecords` bug"* as outstanding, but that bug was fixed Week 12 Day 61 — a stale planning-document
  leftover, corrected.
- **Day 131**: Full verification: build clean, 250/250 unit tests (up from 248 with Days 128-129's
  `EconomySystemTests`), 3/3 fast integration tests, `tsc --noEmit` clean.

**Week 25-26 combined final tally:** 250 unit tests (up from 242 — 6 `SaveLoadServiceTests` + 2
`EconomySystemTests`), 3 fast integration tests, full solution build clean (0 warnings/0 errors),
`tsc --noEmit` clean.

---

## Week 27 (2026-07-14, complete) — Real AI Narrator + API Hardening (Final Pass)

The leftover sweep found zero new gaps this time (every event since Week 19 has been wired at
introduction time; the remaining unsubscribed events all match established precedent). This week
closes `TG-DEV-009`'s own "Known Technical Debt" list, which already named exactly these three items:
AI narration being template-only, no rate limiting, no API versioning.

| Day | Task | Status |
|---|---|---|
| 132 | Leftover sweep (zero new gaps) + write `RFC/RFC-018-ai-narrator-api-hardening.md` | Done |
| 133 | `IAiNarrator`/`NullAiNarrator`/`AnthropicNarrator` + `NarrationService.GenerateSummaryAsync` | Done |
| 134 | Rate limiting (`Microsoft.AspNetCore.RateLimiting`, per-IP fixed window) | Done |
| 135 | API versioning (`RoutePrefixConvention`) + `nginx.conf`/`Garden.Observatory` production-routing fixes | Done |
| 136 | Unit tests + live verification + close-out: `DEVELOPMENT_PLAN.md`/`SPEC_INDEX.md`/`TG-DEV-009.md` updates | Done |

### Days 132-136 actuals (2026-07-14)

- **Day 132**: The leftover sweep (all four event files vs. `HistorySystem`) found nothing new — every
  event introduced since Week 19 has been wired at introduction time, and this sweep double-checked
  every remaining unsubscribed event is still either genuinely dead code (0 publish sites, confirmed
  via grep) or deliberately excluded for tick-level frequency, matching every prior sweep's findings
  exactly. `RFC-018` scopes the three `TG-DEV-009` Known Technical Debt items into one final week.
- **Day 133**: `IAiNarrator` (the "pluggable AI provider" seam `TG-DEV-009` asked for) + `NullAiNarrator`
  (default, used in every environment without a configured `AI:ApiKey`) + `AnthropicNarrator` (real HTTP
  call to Anthropic's Messages API, grounded strictly in the same facts `NarrationService` already
  computes deterministically, per `TG-DEV-009`'s "AI shall never... generate simulation logic"). Falls
  back to the unchanged template narrative on any failure or absence of a key.
- **Day 134**: A global per-IP `FixedWindowLimiter` (300 req/min, invented threshold) via ASP.NET Core's
  built-in `RateLimiting` middleware — no new NuGet package needed.
- **Day 135**: `RoutePrefixConvention` (a single `IApplicationModelConvention`) applies a `v1` route
  prefix globally, rather than editing all 16 controllers. **Two real production bugs were found and
  fixed alongside this, not deferred**: `nginx.conf`'s `/api/` proxy strips its prefix before
  forwarding, but `Garden.Observatory`'s production build had no `VITE_API_URL` configured anywhere in
  `docker-compose.yml`, so API calls would have hit the SPA fallback instead of the backend in a real
  dockerized deployment; and `nginx.conf` had no proxy location at all for `/simulationHub`, the one
  SignalR hub the frontend actually connects to. Both fixed.
- **Day 136**: 3 new `NarrationServiceTests` + 4 new `AnthropicNarratorTests` (stubbed `HttpMessageHandler`,
  no real network calls) — required adding a `Garden.Infrastructure` project reference to
  `Garden.UnitTests` for the first time (257 total, up from 250). Live-verified: the versioned route
  works and the old bare route 404s; the rate limiter rejected 22 of 310 rapid requests with `429`;
  `/v1/assistant/summary` returned the correct template narrative (no AI key configured, as expected);
  the Observatory Dashboard and Civilization pages still render real data through the new `/v1` routes
  with no console errors. `TG-DEV-009`'s Known Technical Debt list updated with resolution notes for
  all three items, following the same annotation convention already used there. Full verification:
  build clean, 257/257 unit tests, 3/3 fast integration tests, `tsc --noEmit` clean. `RFC-018` marked
  Implemented.

**Week 27 final tally:** 257 unit tests (up from 250 — 3 `NarrationServiceTests` + 4
`AnthropicNarratorTests`), 3 fast integration tests, full solution build clean (0 warnings/0 errors),
`tsc --noEmit` clean.

---

## Week 28 (2026-07-15, in progress) — Post-Completion Rebalancing: Making the World Actually Live

Week 27 closed out the planned 27-week cycle, but closing the plan is not the same as the
simulation actually behaving like a living world - live multi-year test runs after Week 27
surfaced that population was structurally incapable of surviving, let alone growing, across
several independent, previously-unnoticed bugs. This week (still open) is the response: found
via direct observation of a running simulation, not code review alone, and each fix
re-verified live rather than assumed correct from reading the diff. Unlike every prior week,
this one wasn't scoped from `TG-###` spec gaps - it was scoped from the simulation itself
refusing to cooperate with its own design intent (`TG-DEV-009`'s "a living, emergent world").

**Round 1 - full-system rebalancing audit** (disease, economy, forest, AI priorities, UI):

- **Disease/health**: `DiseaseSystem`'s "overcrowding" check read `Settlement.CarryingCapacity`
  (`PopulationEcologySystem`'s `Min(HousingCapacity, Food / 3.0)`), which stayed near-zero
  during any food shortage - making the "overcrowded" branch fire almost every day regardless of
  real crowding. It was a disguised food-scarcity flag, not a density signal. Fixed to read
  `HousingCapacity` directly, with food stress folded in as a separate, bounded multiplier. Added
  `Citizen.DiseaseResistance` (grows on recovery - the "civilization adapts" mechanic that didn't
  exist) and a `Healer` building (flat recovery-chance bonus) - the missing primitive-healthcare
  lever `TG-260` never got past its first RFC increment.
- **Economy**: `EconomySystem.ProcessConsumption` drained the same settlement Food/Water storage
  independently of, and unaware of, `CitizenSystem.Eat()`/`Drink()` - two uncoordinated systems
  taxing one resource, silently doubling consumption. Removed; `CitizenSystem`'s need-driven model
  is now the sole consumption path. Added Planks as a real House-building cost so the
  Wood → Workshop → Planks pipeline has a consumer instead of piling up goods with zero economic
  activity.
- **Forest ecosystem**: spread/conversion chances had no upper bound and no density feedback -
  any eligible tile rolled the same flat chance regardless of total map coverage. Added a global
  eligible-land forest-fraction damping term (a real carrying-capacity curve) and
  `WorldTile.HarvestDepletedWeeks`-driven reversion, so sustained wood-harvesting pressure can
  revert an over-logged Forest tile back to Grassland - closing the previously nonexistent
  harvesting → density feedback loop.
- **Responsive nav**: the Observatory sidebar was `hidden` below the `md` breakpoint with zero
  mobile alternative - every route past the first became unreachable on a phone or narrow window.
  Added a hamburger-triggered drawer.
- **Map persistence**: view settings (layer toggles, viewport) moved from plain `useState` to a
  `localStorage`-backed hook.
- **Family relationships**: added real `Citizen.ParentAId`/`ParentBId` genealogy (set at birth)
  and a `GET /citizens/{id}/family` endpoint labeling Father/Mother/Son/Daughter/Sibling/
  Grandparent/Grandchild/Husband/Wife, replacing the undifferentiated interaction-strength
  `Relationship` list for this specific purpose.
- **AI priorities**: added a bounded `CareForFamily` goal - a citizen with a sick immediate
  relative now travels to and tends them once/day, closing the "heal the sick" gap that had no
  citizen behavior at all.

Live-verified over a 10-year run (fresh 50-citizen spawn): population stabilized at 39/50 (78%)
by Year 5 and held flat through Year 10 with zero further deaths - a fundamental change from
every prior run's near-total collapse by Year 2-3. Disease became genuinely survivable (1394 of
1403 infections recovered, 99.4%) instead of a death sentence.

**Round 2 - chasing "why zero births after 10 years" to its actual root causes:**

Population stabilized but never grew. Four further, deeper bugs were found by direct
investigation of the running simulation's numbers, each one masking the next:

1. **`DecomposerSystem`'s `SoilHealth` was a one-way ratchet to zero.** Its only income was
   organic matter from `CitizenDied`/`ForestDeclined` events - both rare, one-off events - while
   `FarmHarvested` (this same system's own subscription) depleted it on every single daily
   harvest. Since `AgricultureSystem`'s yield is multiplied by `SoilHealth/100`, farms produced
   almost nothing within a settlement's first year or two, forever. Fixing disease elsewhere
   *reduced* deaths, ironically starving this income further. Fixed with a direct monthly
   natural-regeneration term (baseline + forest-tile litter bonus), so soil recovers without
   requiring something to die.
2. **`CitizenSystem.GetNextNeededBuilding` capped every settlement at exactly one Farm, ever** -
   a 22-person settlement had the identical food ceiling as a 7-person one. Farm count now scales
   with population, gated on healthy soil so a struggling settlement can't compound its own
   depletion by piling on simultaneous harvests before recovering.
3. **`ReproductionSystem` required a 3-months-per-capita Food stockpile before any birth** -
   unreachable at any realistic subsistence yield even with (1) and (2) fixed. Lowered to a real,
   reachable bar; `AgricultureSystem`'s yield multiplier raised so restored soil health translates
   into an actual surplus instead of merely matching consumption.
4. **`Settlement.MemberIds` never removed a dead citizen.** `HousingCapacity`/
   `HasAvailableHousing` and `ReproductionSystem`'s food-per-capita divisor both read
   `MemberIds.Count` directly - any settlement with real mortality history (nearly all of them)
   reported "housing at capacity" and a diluted food-per-capita forever, independent of actual
   food or space. Live-confirmed severe: reconciling the dev database on load revealed **4 of 8
   settlements had zero actually-living residents while still reporting 17-19 "population"** from
   historical dead members never removed. `CitizenSystem` now subscribes to `CitizenDiedEvent`
   centrally (covers every death path, not just its own); `Program.cs` reconciles existing saved
   worlds once on load.

Fixing all four surfaced the *actual* remaining bottleneck directly, not by inference: three
settlements sat at excellent soil health (100, 100, 69) with zero living residents, while the
settlements with real population (21 and 13 members) were stuck on genuinely poor soil with no
way to migrate toward the fertile, empty land next door.

**RFC-019 - Settlement Migration & Resettlement** (this round's fix): `CitizenSystem.MakeDecision`
already had a full settlement-seeking path, but only for a citizen with `HomeSettlementId ==
null` - an already-settled citizen had no way to ever leave, no matter how much better conditions
were elsewhere. Added bounded, gradual relocation: a settled citizen whose home settlement is in
chronic food hardship (`< 0.5` food-per-capita), with a real reachable alternative (`>= 2.0`
food-per-capita and available housing) within 70 tiles, has a small daily chance (3%, matching
`ReproductionSystem`'s `DailyConceptionChance` order of magnitude) to relocate - removed from the
old settlement's `MemberIds`, joined to the new one on arrival via the same `JoinSettlement` logic
a homeless citizen already uses. Full RFC at `RFC/RFC-019-settlement-migration-resettlement.md`.

**Documentation discipline note**: Round 1 and Round 2's findings were originally reported only
as chat-facing Artifact documents, not logged here or as their own RFC - a real process gap this
week's entry (and `RFC-019`) exists specifically to correct. Going forward, any live-testing
finding that changes simulation behavior gets an RFC (if it's a new mechanic) or a dated entry in
this plan (if it's a bug fix to existing behavior) before or alongside the code, not instead of
it, per this plan's own "How to use `ADR/`, `RFC/`, and `Archive/`" section above.

**Status**: code changes for all of the above are shipped and unit-tested (`AgricultureSystemTests`
updated for the new yield constants; `DiseaseSystemTests` updated for the `HousingCapacity`-based
overcrowding gate). A live 100-year verification run with the migration mechanic in place is the
open item closing this week.

### Long-horizon live verification (2026-07-15) — three more bugs found and fixed by actually running it

Per direct user instruction after Round 2 shipped: stop reporting findings only in chat, log them
here, and actually run the simulation for a long horizon (aiming for a century) rather than
stopping at the first clean-looking number. This surfaced three further real bugs, each one only
visible by watching the simulation run for hours of real time, not from reading the code:

1. **`RFC-019`'s relocation check was reachable in code but functionally dead in practice.** It
   sat *after* the `needsPlanting`/FarmWork check in `MakeDecision`, but "food-stressed enough to
   relocate" and "farm needs seeds" are almost perfectly correlated in a struggling settlement (a
   farm that can't keep its Seeds above the replant threshold *is* what hardship looks like here)
   - so `needsPlanting` was true on nearly every tick a relocation-worthy citizen would otherwise
   be evaluated, and FarmWork won every time. Moved the relocation check before it.
2. **Even after being reachable, "Relocate" didn't persist.** Every goal in `MakeDecision` is
   re-decided from scratch each tick, with nothing to carry intent across ticks except the
   `Last*Day` throttles that gate re-triggering the *same* decision. The relocation check's own
   throttle meant it only fired once, but the very next tick's un-throttled FarmWork/Gather checks
   simply overwrote "Relocate" before the citizen took a single step - confirmed live (a citizen
   showed `CurrentGoal: "Relocate"` one query, back to `"FarmWork"` the next). Made "Relocate"
   sticky: a citizen already mid-relocation now skips re-evaluation (short of a genuine critical
   need) until arrival.
3. **Raising `AgricultureSystem`'s yield multiplier (Round 2, item 3) proportionally raised soil
   depletion too**, since `DecomposerSystem`'s depletion is yield-scaled - completely swamping the
   natural-regeneration fix from Round 2, item 1. Live testing showed every real settlement's
   `SoilHealth` crashing back to near-zero (0.07-2.3) despite that fix. Worked through the full
   loop's actual numbers this time (typical planting size, average seasonal modifier, harvests per
   month) instead of iterating by trial and error: the old depletion constant was roughly 14x too
   aggressive for the new yield. Lowered so a zero-forest settlement settles near a modest but real
   ~40 equilibrium instead of zero.
4. **A genuine deadlock**: Round 1's economy fix added Planks to House's cost (giving Workshop's
   output a consumer), but `GetNextNeededBuilding` still queued Workshop strictly *after* House -
   a settlement could never satisfy House's Planks requirement because it would never build the
   one building that produces Planks until its housing need (permanently unsatisfiable without
   Planks) was already met. Live-confirmed severe: one settlement sat at exactly 5 completed
   buildings for 5+ real-time-observed in-game years, citizens endlessly "Gathering Materials" for
   a House that could never complete. Compounding this, `GetNeededMaterial` could select "Planks"
   itself as the material to go gather, but Planks has no entry in `MaterialToResource` (it's a
   crafted good, not a tile deposit) - a citizen sent to gather it found nothing, forever. Fixed
   both: Workshop now queues before House, and `GetNeededMaterial` only ever returns genuinely
   gatherable materials.

**Result, confirmed live**: within 3 in-game years of the fourth fix, a settlement that had been
structurally incapable of completing a single building beyond its starting five now had 8
completed buildings and **the simulation's first real, organic birth** (`totalBirths: 1`),
population rising from 5 to 6 in that same settlement. This is the first time in this project's
history that population growth (not just survival) has been confirmed live rather than assumed
from code review. A full multi-century run remains future verification work - this session's real-
time budget reached a confirmed, working growth loop at the multi-year scale, not a full century -
but every structural blocker identified through Round 1, Round 2, and this pass now has a shipped,
live-tested fix.

---

## Project Complete (as of 2026-07-14)

All 27 weeks of this development cycle are shipped. Every Backlog item that was ever scoped now has
either a shipped first increment (with its own RFC) or a formal, documented deferral (with its own
ADR) — nothing was silently dropped. The leftover-consolidation discipline this plan adopted from
Week 1 onward held for the entire cycle: every week's sweep either found and fixed a real,
previously-unnoticed gap, or confirmed zero new gaps, right through to the final week.

Total unit tests grew from 50 (at `TG-DEV-009`'s original "Version 1.0" completion) to 257 across this
cycle's 27 weeks — every single new mechanic shipped with direct test coverage, and every leftover fix
found along the way (ten separate instances of "event published but never archived," among others) got
its own regression test. Fast integration tests held steady at 3/3 the entire cycle once split from the
slow suite (Week 1 Day 7).

Remaining known future work (deliberately deferred, not forgotten):

- Deeper Volume IV work (individual species, predator-prey, genetics) — Life Sciences foundation.
- Military organization proper (armies, logistics, command structure) beyond dispute-escalation warfare.
- Scientific Institutions, Knowledge Base/Research Capacity as distinct tracked state — Science & Tech.
- Lost/suppressed/monopolized knowledge, per-settlement divergent legends, folklore merging/fading.
- Full Replay/scrubbing playback, Comparative Replay, bookmarks/annotations — Save/Load & Replay.
- Modding & Extensibility's entire feature set — formally deferred (`ADR-005`) until a genuine
  extension point exists to build against.
- Multiple simultaneous AI providers, tiered/authenticated rate limiting, a full versioning framework.

## Project-Wide Timeline Estimate (as of 2026-07-13)

Asked directly: *how many weeks to finish everything?* Answered honestly, with the same
caveat this plan opened with — large greenfield items are estimates, not commitments, and
each has historically grown once it actually got an RFC (Communication and Language were
both originally sketched as "roughly a week" and landed exactly there, but every week so far
has also surfaced 1-3 real bugs that weren't part of the original estimate). "Complete" here
means the same thing it has meant since Week 5: one real, tested first increment per system,
matching TG-### spec at the level of RFC-001/002/003 — not full lifetime-of-the-project
parity (Language's own RFC defers Vocabulary/Grammar/Writing indefinitely, for example).

| Weeks | Scope | Basis for the estimate |
|---|---|---|
| 1-27 (done) | Stabilization, Test/CI, Emotion+Relationships, Observatory polish, Communication, Language, Anomaly cleanup, Education, Law & Justice, Flora History (Volume IV increment 1), Borders & Territorial Dynamics, Anomaly Cleanup 2, the `AgricultureSystem` ADR, Population Ecology (Volume IV increment 2), Disease & Health (Volume IV increment 3), Evolution & Adaptation (Volume IV increment 4), Decomposers & Microbiology (Volume IV increment 5), Fauna & Animal Behavior (Volume IV increment 6), Anomaly Cleanup 3 (Week 19 Day 92), Warfare & Military Organization (dispute escalation), Infrastructure-as-network (route quality), Anomaly Cleanup 4 (Week 23 Day 112), Science & Technology (independent per-settlement discovery), Legends & Myths (first increment), Save/Load Fidelity + Timeline Branching, Modding & Extensibility disposition, Real AI Narrator + API Hardening (rate limiting, versioning) | Actuals |

**Total projection: ~27 weeks end-to-end (about 6.5 months) — all 27 are done.** Every week
landed within its planned window; the true final count matched the original estimate exactly,
though (as this section warned throughout) the *content* of several weeks shifted from what was
originally sketched — Week 12 was an unplanned extra cleanup week inserted mid-course, and Weeks
19-27 each opened with a leftover-consolidation sweep that occasionally found and fixed a real bug
before that week's headline feature even started. See "Project Complete" above for the final
summary and remaining deliberately-deferred future work.

---

## How to use `ADR/`, `RFC/`, and `Archive/` going forward

This plan and `SPEC_INDEX.md` both assume these three folders are used, not just present:

- **`ADR/`** — one file per non-obvious, hard-to-reverse decision (e.g. Day 3's empty-project
  disposition, the Technology-system philosophy conflict). Format: context → decision →
  consequences. Write it *before* the code changes, not after, so the reasoning survives even
  if the decision is later reversed. Name files `ADR-NNN-short-title.md`, incrementing.
- **`RFC/`** — one file per backlog item above before it gets a day-to-day plan of its own.
  An RFC's job is specifically to invent the missing numbers/thresholds/formulas the design
  docs never gave, and get them reviewed before implementation starts. Once accepted, link the
  RFC from the relevant `TG-###` doc's "Depends On"/related-docs section and from this plan.
  Name files `RFC-NNN-short-title.md`, incrementing.
- **`Archive/`** — anything superseded moves here with its original filename intact, not
  deleted, so history stays inspectable (consistent with the project's own Constitutional Law
  that history is permanent). The first candidate once `ADR-001` lands is likely
  `00_Master/The_Garden_Project_Blueprint.md`, if its remaining content gets fully absorbed
  into the TG-### series.

Update `SPEC_INDEX.md`'s Change Log every time a document in this library is added, edited,
renamed, retired to `Archive/`, or promoted out of `RFC/` — that log is the project's memory
of *why* the library looks the way it does, which nothing else captures.
