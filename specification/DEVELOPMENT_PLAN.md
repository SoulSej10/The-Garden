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
| ~~Communication~~ / Language / Education / Law & Justice | `TG-500` (scoped, shipped), `TG-510`, `TG-550`, `TG-590` | **Communication's first increment shipped in Week 5 (2026-07-10) — see `RFC/RFC-002-communication-knowledge-diffusion.md`.** Language/Education/Law & Justice still need their own RFCs and now depend on Communication (landed) rather than Emotion/Relationships directly. |
| `TechnologyService` progress-scaling bug | Week 5 Day 22 finding | `EvaluateTechnology()` accumulates each individual `Technology.CurrentProgress` at `settlementProgress * 0.1`, but nothing else in the codebase treats `settlement.TechnologyProgress` as 10x the per-tech scale. Confirmed live: after 55+ simulated years with a 26-member settlement, zero technologies had discovered (needed ~385 years at the current rate). Flagged via `spawn_task` (`task_7f002f15`) — makes the entire technology tree practically unreachable until fixed. |
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
