# RFC-007: Borders & Territorial Dynamics — Territorial Influence (First Increment)

**Status:** Implemented (Week 11, 2026-07-10 - see DEVELOPMENT_PLAN.md Days 51-55)
**Date:** 2026-07-10
**Author:** DEVELOPMENT_PLAN.md (Week 10 close-out backlog triage)
**Governing spec:** `03_Sciences/05_Civilization/TG-620_Borders_Territorial_Dynamics.md`

---

## Why this needs an RFC before a day-to-day plan

Same reasoning as every prior RFC: TG-620 is thorough on vocabulary (Territorial Influence,
Administrative Reach, Border Stability, Border Disputes, Frontier Regions) and names 10
events (`FrontierEstablished`, `BorderExpanded`, `BorderContracted`, `TerritoryIntegrated`,
`RegionSecedes`, `BorderDisputeBegins`, `BoundaryAgreementSigned`,
`FrontierSettlementFounded`, `AdministrativeRegionCreated`, `TerritorialReorganization`) but
gives no formula for influence, decay, or dispute resolution. Confirmed via grep:
`Settlement.TerritoryRadius` is a flat `int` that only ever increments
(`SettlementManager.ExpandTerritory`) - exactly the gap the backlog names: "no decay function
specified anywhere - needs to be invented."

## Why Borders, and why now

Week 10's Day 49 assessment recommended Borders & Territorial Dynamics over continuing Life
Sciences, specifically because it doesn't depend on resolving `AgricultureSystem`'s
hardcoding question. TG-620 depends on `TG-600` Civilization Emergence, `TG-610` Kingdoms &
States, `TG-570` Economics, and `TG-580` Politics & Governance - all four already have real
code (`CivilizationSystem`, `KingdomService`, `EconomySystem`, `GovernanceService`), so this
RFC doesn't need to defer any precondition, unlike Education/Law & Justice deferring Groups/
Social Norms.

## Scope decision: influence-driven expansion/contraction, plus dispute detection only

TG-620 spans natural boundaries, border zones, frontier settlements, secession, and full
territorial reorganization. First increment covers exactly two things: **territory that can
now shrink as well as grow**, driven by a real, existing signal (population and legitimacy),
and **detecting** (not resolving) genuine territorial disputes between overlapping
settlements of comparable strength.

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| `Settlement.TerritorialInfluence`, computed from existing `Population`/`Legitimacy` fields | Natural boundaries (rivers/mountains reducing reach) - no terrain-based reach model exists yet |
| `TerritorySystem`: yearly recompute; meaningful influence growth expands territory (reuses the existing `ExpandTerritory`/`SettlementExpandedEvent` path), meaningful decline contracts it (new `BorderContractedEvent`, one of TG-620's 10 named events) | Border zones/borderlands with mixed culture, migration corridors, buffer states - all need concepts this RFC doesn't add |
| `BorderDisputeBeginsEvent` (another of TG-620's 10) fired once per overlapping settlement pair of comparable influence, tracked in-memory on the system (same pattern `CommunicationSystem.EventTitles` uses) so it fires exactly once, not every year the overlap persists | Actually *resolving* a dispute - no consequence, no winner, no territory changes hands as a result. TG-620 itself separates "disputes may arise" from "disputes may resolve peacefully or violently," and resolution needs a real mechanic this RFC doesn't invent |
| — | `FrontierEstablished`, `TerritoryIntegrated`, `RegionSecedes`, `BoundaryAgreementSigned`, `FrontierSettlementFounded`, `AdministrativeRegionCreated`, `TerritorialReorganization` - the other 7 named events, each requiring state (frontiers, administrative regions, secession) this RFC doesn't model |

## Why Population and Legitimacy specifically

TG-620 names Population, Infrastructure, Administrative capacity, Economic activity,
Military security, and Communication as influence inputs. Of these, only Population
(`Settlement.Population`) and Legitimacy (`Settlement.Legitimacy`, Week 2 Day 9,
`GovernanceService`) are already real, tracked, per-settlement numbers - the rest
(Infrastructure connectivity, Military security) have no equivalent state anywhere yet.
Reusing exactly these two, the same way RFC-004 reused `Intelligence` and RFC-005 reused
`Legitimacy`, keeps this increment additive rather than requiring new supporting systems.

## Mechanism

A new `TerritorySystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, yearly cadence
(`IntervalTicks = 336`, matching the established `CivilizationSystem` convention - the Week
6 Day 27 cadence-naming finding applies here too, scheduled for its own fix in Week 12).

1. `TerritorialInfluence = Math.Min(100, Population * 0.5 + Legitimacy * 0.3)` (invented - no
   TG-620 formula given). Stored on `Settlement` so the Observatory can surface it directly.
2. Each yearly evaluation, compare influence to the value recorded at the previous
   evaluation (stored on the system, not the settlement, to avoid adding EF-migration
   surface for a value that's fully recomputable): a rise of more than 10 calls the existing
   `SettlementManager.ExpandTerritory` (unchanged, already archived by `HistorySystem`); a
   fall of more than 10 contracts `TerritoryRadius` by 1 (floor of 1) and publishes
   `BorderContractedEvent`.
3. For every settlement pair within `TerritoryRadiusA + TerritoryRadiusB` tiles of each
   other (a real geometric overlap) where influence is within 20% of each other (neither
   settlement has a clear administrative advantage - a genuine dispute, not an obvious
   claim), publish `BorderDisputeBeginsEvent` once per pair, tracked via an in-memory
   `HashSet` on the system so it never re-fires for the same still-overlapping pair.

## Explicitly out of scope for the next cycle

- Any consequence of a border dispute - no territory changes hands, no war, no negotiation.
- Natural boundaries, border zones, frontier settlements, secession, administrative regions.
- Writing back into `Legitimacy`/`Population` based on territorial state - one-directional
  read only, same posture RFC-004/005 established with `intelligenceFactor`/`Legitimacy`.
- Kingdom-level territorial aggregation (a Kingdom's total territory as the sum of its
  member settlements') - this increment is settlement-level only.

## Open questions for review before implementation starts

1. Is `Population * 0.5 + Legitimacy * 0.3` the right influence formula, or should Population
   dominate more heavily (TG-620 lists it first among influence inputs)? (Recommendation:
   keep as proposed - Population is already the single largest number involved (settlements
   run 7-26+ members) and already dominates the sum in practice; Legitimacy (0-100 scale)
   contributes meaningfully without needing a larger coefficient.)
2. Should the ±10 expand/contract threshold and the 20%-comparable-influence dispute
   threshold be the same invented "significant change" concept, or independently tuned?
   (Recommendation: independently tuned - they answer different questions (has *this*
   settlement changed enough to act? vs. are *these two* settlements comparably matched?)
   and conflating them risks the same kind of accidental coupling Week 6's Language RFC
   avoided by not literally importing `DiplomacyService`'s bands.)

## Implementation notes (Week 11, added at close-out)

- Implemented as designed: `Settlement.TerritorialInfluence` (with a real EF migration,
  since `Settlement` is EF-persisted), `TerritorySystem` (yearly cadence, matching the
  established convention), `BorderContractedEvent`/`BorderDisputeBeginsEvent`, and a
  minimal per-settlement influence/dispute surfacing in the Observatory. Both open
  questions resolved as recommended.
- **Subscribed both new events to `HistorySystem` at introduction time**, rather than
  discovering the gap later - the same TG-001 Law IV pattern that had to be fixed after
  the fact for `DialectFormedEvent` (Week 7) and `ForestExpanded`/`Declined` (Week 10).
  Caught during this week's own live verification before it became a third instance of
  the same mistake.
- 8 new unit tests for `TerritorySystem` plus 2 more for the `HistorySystem` wiring (153
  total). Verified live: `TerritorialInfluence` computed correctly from real Population/
  Legitimacy (23.5 for one settlement), and the Observatory's Territory section rendered
  the value correctly (24, rounded) with no console errors. No `BorderContracted`/
  `BorderDisputeBeginsEvent` occurred organically within the verification window - neither
  is a bug, both are gated by real thresholds that simply weren't crossed in this run;
  the mechanism itself is directly unit-tested.
