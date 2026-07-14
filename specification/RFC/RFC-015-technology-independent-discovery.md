# RFC-015: Science & Technology — Independent Per-Settlement Discovery (First Increment)

**Status:** Implemented (Week 23, 2026-07-14 - see DEVELOPMENT_PLAN.md Days 112-116)
**Date:** 2026-07-14
**Author:** `DEVELOPMENT_PLAN.md` Week 23
**Governing spec:** `03_Sciences/05_Civilization/TG-670_Science_Technology.md`

---

## Why this needs an RFC before a day-to-day plan

`ADR-004` (`specification/ADR/ADR-004-science-technology-disposition.md`) resolved the Week 10
Backlog question ("change the doc to match reality, or redesign the system to match the doc") by
identifying the real defect: `TechnologyService` doesn't merely resemble a "predefined technology
tree" (which `TG-670` explicitly disclaims) — it shares one `Technology.CurrentProgress`/`IsDiscovered`
pair per named technology across every settlement in the world, making Independent Discovery,
Parallel Inventions, and Technological Divergence (all named in `TG-670`'s Edge Cases) structurally
impossible, not just unmodeled. This RFC is still needed because `TG-670` gives no formula for
research contribution, discovery thresholds, or its 10 named events (`MajorDiscovery`,
`EngineeringBreakthrough`, `MedicalAdvance`, `AgriculturalInnovation`, `ScientificTheoryProposed`,
`UniversityFounded`, `ResearchExpedition`, `TechnologyAdopted`, `KnowledgeExchange`,
`ScientificRevolution`) — same as every prior RFC in this series.

## Why per-settlement state, and why now

`TG-670`'s Edge Cases close with: *"Civilizations need not advance at the same pace or in the same
direction."* The current implementation cannot honor this even in principle — `WorldState.Technologies`
holds exactly one mutable object per named technology, and `TechnologyService.EvaluateTechnology()`
lets every settlement in the simulation push progress onto that same shared object. The first
settlement to cross the threshold sets `IsDiscovered = true` globally, permanently locking every other
settlement out. This RFC replaces that shared state with per-settlement state, using the same "new pure
in-memory join entity" pattern this series has used for every other per-relationship/per-instance
concept (`LegalCase`, `Apprenticeship`, `War`, `LanguageDivergence`).

## Scope decision: per-settlement progress/discovery, Intelligence-driven research

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| A new pure in-memory `SettlementTechnology` entity (`Garden.World/Entities/`, following the `LegalCase`/`Apprenticeship`/`War` pattern — not EF-persisted) keyed by `(SettlementId, TechnologyName)`, replacing `Technology.CurrentProgress`/`IsDiscovered` as the live simulation state | Knowledge Base/Research Capacity/Innovation Rate/Discovery Momentum as their own distinct tracked state variables — `TG-670` names these but gives no formula, and building them from nothing is explicitly out of this increment's budget (`ADR-004`) |
| Research contribution scaled by the settlement's average `Citizen.Attributes.Intelligence` (the same aggregate `EvolutionSystem`, Week 16, already computes) — implementing `TG-670`'s explicitly-named Simulation Rule *"Education increases research capacity"* for the first time since this codebase existed | Scientific Institutions (schools, universities, laboratories) as buildable entities — `EducationSystem`'s `Apprenticeship` is the closest existing analogue and isn't extended into a new building type this increment |
| `Technology.AllTechnologies` (the named catalog itself: category, `ProgressRequired`) is kept unchanged — `ADR-004` treats the catalog as a reasonable knowledge domain, not the disqualifying issue | Lost technologies, knowledge monopolies, suppressed research, rediscovery of forgotten knowledge — all named in `TG-670`'s Edge Cases but requiring a "forgetting"/"loss" mechanic this increment doesn't add |
| 1 of `TG-670`'s 10 named events: `TechnologicalDivergenceEvent` (two settlements' discovered-technology sets meaningfully diverge — a genuinely new observation this increment makes *possible* for the first time, since under the old shared-state model no two settlements could ever hold different technology sets to diverge in the first place) | `MajorDiscovery`, `EngineeringBreakthrough`, `MedicalAdvance`, `AgriculturalInnovation`, `ScientificTheoryProposed`, `UniversityFounded`, `ResearchExpedition`, `TechnologyAdopted`, `KnowledgeExchange`, `ScientificRevolution` — the other 9, each requiring concepts (research expeditions, institutions, knowledge exchange between settlements as a distinct diffusion event) this increment doesn't model |
| — | Technology diffusion via trade/migration/education as an accelerant for a settlement that hasn't discovered a technology another settlement already has — a real, valuable follow-up (`TG-670` explicitly names "Ideas often travel faster than people"), deferred since it would require reading `TradeRoute`/`MigrationService` data this increment doesn't yet wire in |

## Why Intelligence specifically

`TG-670`'s Simulation Rules name "Education increases research capacity" as a first-class rule, but
the current `CalculateProgress` scales only by raw population count — no education/intelligence signal
factors in at all, despite `EducationSystem`'s `Apprenticeship` mechanic (`RFC-004`, Week 8) already
transferring `Intelligence` between citizens, and `EvolutionSystem` (`RFC-010`, Week 16) already
computing each settlement's average `Citizen.Attributes.Intelligence` for its own drift-detection
purposes. Reusing that exact average as a research multiplier is the same "reuse an existing field"
discipline every RFC since `RFC-004` has used, and it is the most direct way to make a named-but-ignored
Simulation Rule actually real.

## Mechanism

`TechnologyService` (existing class, extended — not a new `IScheduledSystem`, since it is already
invoked from `CivilizationSystem`'s yearly evaluation).

1. `SettlementTechnology` (new entity): `SettlementId`, `TechnologyName`, `CurrentProgress`,
   `IsDiscovered`, `DiscoveredTick`, `DiscoveredByCitizenId`/`Name`. `WorldState.SettlementTechnologies`
   (new `List<SettlementTechnology>`, pure in-memory).
2. `EvaluateTechnology()` is rewritten so each settlement looks up (or lazily creates) its own
   `SettlementTechnology` row per named technology in `Technology.AllTechnologies`, instead of mutating
   a shared `Technology` object. All existing category-based contribution multipliers (Agriculture +50%
   with a completed Farm, Construction +30% with completed buildings) are preserved unchanged.
3. `CalculateProgress` gains an Intelligence factor: `contribution *= 1.0 + (averageIntelligence - 5.0) / 20.0`
   (invented scaling — no `TG-670` formula given; a settlement with average Intelligence 5.0, the
   existing `CitizenGenerator` baseline, sees no change, an above-average settlement researches faster,
   a below-average one slower), clamped so it never goes negative.
4. Discovery crossing behaves exactly as before, just now scoped to one `SettlementTechnology` row
   instead of one shared `Technology` — the same settlement can now independently discover a technology
   another settlement already has, or one no other settlement has touched yet.
5. `TechnologicalDivergenceEvent` (new): once per yearly evaluation, if two settlements' discovered-technology
   name sets differ by at least an invented threshold count (e.g. 3), and this is the first time that
   pair has crossed the threshold, publish the event — reusing the same "state-transition, not
   level-crossing repeated every tick" discipline every prior RFC's event-firing logic has used.
6. `TechnologicalDivergenceEvent` subscribed to `HistorySystem` **at introduction time**
   (`HistoryCategories.Discovery`, reusing the category `TechnologyDiscovered` already uses), continuing
   the practice reinforced Week 12 Day 61.
7. `CivilizationController.GetTechnology()` changes from a single world-wide discovered/in-progress list
   to a per-settlement breakdown (`?settlementId=` query parameter, defaulting to an aggregate summary
   view for backward compatibility with the existing Civilization dashboard) — a genuine API shape
   change `ADR-004` calls out as a real consequence, not an internal-only refactor.

## Explicitly out of scope for the next cycle

- Knowledge Base/Research Capacity/Innovation Rate/Discovery Momentum as distinct tracked state.
- Scientific Institutions (schools, universities, laboratories) as buildable entities.
- Lost/suppressed/monopolized knowledge, rediscovery mechanics.
- Technology diffusion via trade/migration (a settlement "catching up" faster because a neighbor
  already discovered something) — deferred as a named follow-up, not silently dropped.
- The other 9 of `TG-670`'s 10 named events.

## Open questions for review before implementation starts

1. Should a settlement's `SettlementTechnology` rows be lazily created (only when that settlement first
   contributes progress) or eagerly seeded for every settlement × every technology at settlement
   founding? (Recommendation: lazily created — eager seeding would mean thousands of near-zero-progress
   rows for settlements that will never research some categories at all, e.g. a landlocked settlement
   and Water Travel; lazy creation is also the simpler migration path from the existing shared-list
   code.)
2. Should the Intelligence factor apply multiplicatively to the whole `CalculateProgress` output, or
   only to the base population term (leaving the Agriculture/Construction category bonuses
   Intelligence-independent)? (Recommendation: multiply the whole output — a more intelligent
   population researches faster across every category, not just the population-driven base; the
   category bonuses represent *opportunity* (having a Farm to experiment on), Intelligence represents
   *capability*, and both should compound.)

## Implementation notes (Week 23, added at close-out)

Shipped as specified, with both open questions resolved as recommended (lazy row creation, the
Intelligence factor multiplying the whole `CalculateProgress` output).

- `SettlementTechnology` (new pure in-memory entity, `WorldState.SettlementTechnologies`) replaces
  `Technology.CurrentProgress`/`IsDiscovered` as live state; `Technology.AllTechnologies` remains the
  static, read-only catalog. `TechnologyService.EvaluateTechnology` was rewritten around
  `GetOrCreate(settlementId, technologyName)` instead of mutating shared `Technology` objects.
- `CalculateProgress` now multiplies by `1.0 + (averageIntelligence - 5.0) / 20.0` (clamped at 0),
  reusing living members' average `Attributes.Intelligence` — the same aggregate `EvolutionSystem`
  already computes — implementing `TG-670`'s named-but-previously-ignored "Education increases
  research capacity" rule for the first time.
- `TechnologicalDivergenceEvent` fires once per settlement pair when their discovered-technology name
  sets first differ by 3 or more, subscribed to `HistorySystem` at introduction time
  (`HistoryCategories.Discovery`, severity 5.0).
- `CivilizationController.GetTechnology` gained an optional `settlementId` query parameter; omitted, it
  returns the world-wide aggregate discovered list plus an aggregate "in progress" view (the
  furthest-along settlement's progress per technology) rather than `null` — a real bug caught before
  shipping: the frontend's existing no-argument `fetchTechnology()` call would have thrown on
  `data.inProgress.length` if the endpoint returned `null` for "no settlement selected," which the
  first draft did.
- `TechnologyView` gained `SettlementName` (resolved via a settlement-id lookup) after noticing the
  existing Observatory `TechnologyTab` displays `t.settlementName` per discovered technology — the
  original per-settlement-scoped view only carried `SettlementId`.
- Tests: 2 new `TechnologyServiceTests` (`OneSettlementDiscovering_DoesNotLockOutAnother`,
  `TechnologicalDivergenceEvent_FiresOncePairsDivergeEnough`) plus 1 `HistorySystem` regression test.
  The pre-existing `DiscoveredByCitizenId`-based assertion was updated to check
  `DiscoveredByCitizenName` instead, since `TechnologyView` (the new join type) doesn't carry the raw
  citizen ID.
- Live verification: resumed a saved 8-settlement, Year-4 world. The aggregate "in progress" endpoint
  showed genuinely different `CurrentProgress` values across technology categories (2.03 vs. 1.76 vs.
  1.35), confirming independent per-settlement tracking is real, not shared. No organic
  `TechnologicalDivergenceEvent` occurred in this run — a legitimate non-finding: this particular
  world's population had collapsed to 1 living citizen from a pre-existing, unrelated famine bug (first
  observed Week 21-22), leaving `technologiesDiscovered` at 0 throughout the verification window, so no
  settlement pair ever accumulated the 3+ technology gap the event requires. The mechanism itself is
  directly unit-tested end-to-end. Full verification: build clean, 242/242 unit tests, 3/3 fast
  integration tests, `tsc --noEmit` clean.
