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
| Life Sciences foundation (Flora, Fauna, Decomposers, Population Ecology) | `TG-200`–`TG-240` | Entire Volume IV is greenfield; `AgricultureSystem`/`CitizenSystem` currently hardcode biology this volume is supposed to own — untangling that is itself a design question. |
| Borders & Territorial Dynamics | `TG-620_Borders_Territorial_Dynamics.md` | Spec explicitly calls for a regional-influence-field model; current code is a flat `TerritoryRadius` int. No decay function specified anywhere — needs to be invented. |
| Warfare & Military Organization | `TG-640_Warfare_Military_Organization.md` | Largest single unimplemented system in the whole library; spec gives no combat-resolution, morale, or logistics-attrition formulas at all. |
| Infrastructure-as-network | `TG-660_Infrastructure.md` | Spec explicitly rejects the building-centric model the current `ConstructionSystem`/`Building.cs` uses — this is a philosophy-vs-implementation conflict that needs a decision, not just new code. |
| Science & Technology redesign | `TG-670_Science_Technology.md` | Spec explicitly disclaims "a predefined technology tree"; current `Technology.cs` is exactly that. Needs an ADR: change the doc to match reality, or redesign the system to match the doc. |
| ~~Communication~~ / ~~Language~~ / ~~Education~~ / Law & Justice | `TG-500` (scoped, shipped), `TG-510` (scoped, shipped), `TG-550` (scoped, shipped), `TG-590` | **Communication shipped Week 5, Language shipped Week 6, Education shipped Week 8 — see `RFC/RFC-002`/`RFC/RFC-003`/`RFC/RFC-004`.** Law & Justice still needs its own RFC. |
| `RelationshipSystem` never bonds parent and child | Week 8 Day 39 finding | `RelationshipSystem`'s only live trigger (`CitizenBornEvent`) bonds a newborn's two *parents*, never the parent and the child. This makes `EducationSystem`'s mentor/student pairing (Adult/Elder ↔ Child/Teen, gated on an *existing* `Relationship`) structurally unreachable — not rare, impossible — until a cross-generation `Relationship` trigger exists. Natural follow-up to `RelationshipSystem` itself, not an `EducationSystem` bug. |
| ~~`TechnologyService` progress-scaling bug~~ | Week 5 Day 22 finding, **fixed 2026-07-10** | `EvaluateTechnology()` accumulated each individual `Technology.CurrentProgress` at `settlementProgress * 0.1`, but nothing else in the codebase treated `settlement.TechnologyProgress` as 10x the per-tech scale — confirmed live, zero technologies discovered after 55+ simulated years. Fixed by removing the scale-down (category multipliers for Agriculture/Construction retained). Verified: 3 new unit tests, and live — a fresh run discovered 10 technologies across 2 settlements within Year 1 alone. |
| ~~`TradeRouteService` never creates routes~~ | Week 6 Day 29 finding, **fixed 2026-07-10** | Root cause: once a route existed for a settlement pair (active or not), `EvaluateTradeRoutes()`'s `existing != null` check unconditionally skipped re-evaluating that pair forever — so a route that went quiet once (an ordinary occurrence) permanently locked that pair out of trading again, even when a fresh surplus/scarcity later appeared. A secondary bug was found alongside it: goods always flowed a fixed direction regardless of which settlement actually held the surplus. Fixed by letting an inactive route reactivate against a newly-found trade good, and by determining flow direction from the actual surplus holder. Verified: 3 new unit tests (124 total) including an exact reproduction of the live numbers reported (Food 74 vs 0, 23 tiles apart) and a reactivation-after-abandonment test. **Live re-verification was inconclusive** — a fresh run's settlements repeatedly sat exactly at the FindTradeGood boundary (Food = 10, needs strictly <10) rather than crossing it, a separate equilibrium detail worth noting but not chased further here. |
| `CivilizationSystem`'s "yearly" cadence isn't a year | Week 6 Day 27 finding | `_lastYearlyTick >= 336` (used by `TechnologyService`/`ReligionService`/`KingdomService`/`CultureService`/`LanguageSystem`) is ~14 days at `SimulationTime`'s actual scale (1 year = 24 × 30 × 12 = 8640 ticks), not a year. Affects five systems' cadence naming at once — needs its own look (rename to reflect reality, or fix the threshold to 8640) rather than a fix folded into whichever RFC happens to notice it next. |
| ~~`DialectFormedEvent` never archived by `HistorySystem`~~ | 2026-07-10 audit finding, **scheduled Week 7 Day 31** | `HistorySystem` subscribes to all 12 of Week 1's original `CivilizationEvent` types, but `DialectFormedEvent` (added Week 6) was never added alongside them — reproducing the exact TG-001 Law IV ("History Is Permanent") violation Week 1 Day 1 was created to close, this time on new code rather than old. |
| ~~`EmotionalState` never surfaced in the Observatory~~ | 2026-07-10 audit finding, **scheduled Week 7 Day 32** | `Citizen.Emotions` (6 emotions, Week 3 Days 11-12) is returned by `CitizensController.GetCitizen` but `CitizenDetail`/`CitizensPage.tsx` never expose or render it — confirmed via grep, zero references anywhere in `Garden.Observatory`. Week 4 Day 16 surfaced Settlement tier/Governance but nothing ever covered surfacing Emotion on the Citizen page itself. |
| ~~`Relationship` data never surfaced in the Observatory~~ | 2026-07-10 audit finding, **scheduled Week 7 Day 33** | `CitizensController` has a dedicated `GET /citizens/{id}/relationships` endpoint (Trust/Affection/SocialDistance per pair, Week 3 Day 13) that the frontend never calls — confirmed via grep, no "relationship" reference in `CitizensPage.tsx` or anywhere else in the Observatory beyond an unrelated `tradeRelationships: unknown` placeholder field. |
| Legends & Myths generation | `TG-STRY-040_Legends_Myths.md` | Needs Character Stories + Civilization Stories + Historical Narrative all functioning first; currently the deepest dependency chain in `04_Story`. |
| Replay & Timeline Branching | `TG-OBS-007_Save_Load_Replay.md` | TG-DEV-009 shipped save/load/backup, but not branching timelines or playback controls — a real architecture addition, not a UI feature. |
| Modding & Extensibility | `TG-OBS-009_Modding_Extensibility.md` | Explicitly deferred by its own spec until core Observatory work is done. |
| Real LLM-backed AI narrator | `TG-DEV-009` Known Limitations | Current AI is template/pattern-matched. Needs a provider-integration ADR (cost, latency, determinism-safety — the AI must never be allowed to invent facts per `TG-001`). |
| API rate limiting & versioning | `TG-DEV-009` Known Limitations | Small, well-understood scope — could be pulled forward into a future week without an RFC if prioritized. |
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

---

## Project-Wide Timeline Estimate (as of 2026-07-10)

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
| 1-6 (done) | Stabilization, Test/CI, Emotion+Relationships, Observatory polish, Communication, Language | Actuals |
| 7 (in progress) | Anomaly cleanup (this week) | Actuals so far |
| 8 | RFC-004 + Education (`TG-550`) | Same size as Communication/Language (1 week each) |
| 9 | RFC-005 + Law & Justice (`TG-590`) | Same size as Communication/Language |
| 10-11 | Life Sciences foundation (`TG-200`-`TG-240`) | Explicitly the largest single greenfield volume; existing hardcoded biology in `AgricultureSystem`/`CitizenSystem` needs untangling first — budgeted 2 weeks |
| 12 | Borders & Territorial Dynamics | A single invented decay function + one entity, similar shape to Language |
| 13-14 | Warfare & Military Organization | Explicitly "the largest single unimplemented system in the whole library" — budgeted 2 weeks |
| 15-16 | Infrastructure-as-network | Needs an ADR first (philosophy conflict with current `ConstructionSystem`), then whatever that ADR decides — budgeted 2 weeks |
| 17 | Science & Technology redesign | ADR-first, likely resolves to a doc/reality reconciliation rather than a full rebuild — budgeted 1 week, could shrink |
| 18 | Legends & Myths generation | Its own dependencies (Character/Civilization Stories, Historical Narrative) already exist, so this is closer to Communication/Language in size |
| 19 | Replay & Timeline Branching | A real architecture addition on top of existing save/load — budgeted 1 week, could grow once scoped |
| 20 | Modding & Extensibility | Explicitly deferred by its own spec until core Observatory work is done — likely to move later, not sooner |
| 21 | Real LLM-backed AI narrator | Needs a provider-integration ADR (cost/latency/determinism) before implementation — budgeted 1 week for a first real integration |
| 22 | API rate limiting/versioning + `TradeCompletedEvent` cleanup + final pass | Both explicitly called "small, well-understood scope" in the original backlog |

**Total projection: ~22 weeks end-to-end (about 5 months), of which 7 are done or in
progress — roughly 15 more weeks from here.** Treat this as a planning band, not a
commitment: past estimate accuracy on the *already-completed* weeks has been good (every
week landed in its planned 5 days), but every week has also found something the plan didn't
predict, so the true number is more likely 15-20 remaining weeks than exactly 15. This
section should be re-forecast at the end of each week, the same way `SPEC_INDEX.md`'s Change
Log gets updated after every day.

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
