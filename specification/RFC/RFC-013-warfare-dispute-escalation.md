# RFC-013: Warfare & Military Organization — Dispute Escalation (First Increment)

**Status:** Implemented (Weeks 19-20, 2026-07-13 - see DEVELOPMENT_PLAN.md Days 92-101)
**Date:** 2026-07-13
**Author:** `DEVELOPMENT_PLAN.md` Weeks 19-20
**Governing spec:** `03_Sciences/05_Civilization/TG-640_Warfare_Military_Organization.md`

---

## Why this needs an RFC before a day-to-day plan

`TG-640` is, by its own priority marking, the largest single unimplemented system in the whole
library: military organization (militias, professional armies, navies, mercenaries), logistics,
command structure, strategy, intelligence, morale, occupation, and peace negotiation, spanning 10
named events (`ArmyMobilized`, `BorderIncident`, `WarDeclared`, `CampaignBegins`, `BattleFought`,
`CityBesieged`, `SupplyLineBroken`, `PeaceNegotiated`, `VeteransReturn`, `MilitaryReform`) — and gives
no formula for any of it. Its own Performance Considerations are explicit: *"Routine troop movement
should be aggregated whenever possible. Only strategically significant campaigns, battles, reforms,
and peace agreements require long-term historical persistence... emphasize logistics, command, and
strategic objectives over individual combat calculations."*

## Why Warfare, and why now — the exact hook this codebase already built

`TG-640`'s Design Philosophy states outright: *"Wars do not begin when armies move. They begin when
trust fails."* Its Causes of War list includes "Territorial disputes" first. This codebase already has
**both signals, unresolved, waiting**:

1. `TerritorySystem.ActiveDisputes` (`RFC-007`, Week 11) detects genuine territorial disputes between
   overlapping settlements of comparable strength — but `RFC-007` explicitly deferred *any consequence*:
   *"Actually resolving a dispute - no consequence, no winner, no territory changes hands as a result...
   TG-620 itself separates 'disputes may arise' from 'disputes may resolve peacefully or violently.'"*
2. `DiplomaticRelation.CurrentRelation == Hostile` (score < 20, `DiplomacyService`, pre-existing) is the
   literal, already-computed "trust fails" signal `TG-640`'s own framing calls for.

No RFC in this series has had a cleaner, more direct hook: two real signals, already tracked, already
named by the spec as war's causes, sitting unresolved since Week 11. This increment closes that gap
rather than building military organization from nothing.

## Scope decision: dispute escalation to war, battle attrition, and peace — no army/logistics layer

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| `Settlement.MilitaryStrength` (a single aggregate number derived from existing `Population`/`Legitimacy` — no army, unit, or force-structure entity) | Military organization (militias, professional armies, navies, mercenaries), command structure (officers, generals), logistics networks, military technology — all named in `TG-640` but requiring entities this increment doesn't add |
| A new pure in-memory `War` entity (`Garden.World/Entities/`, following the `LegalCase`/`Apprenticeship` pattern — not EF-persisted) tracking two settlements, active status, and intensity | Strategy, intelligence/reconnaissance, morale as a distinct tracked variable, occupation administration — all named but requiring concepts this increment doesn't model |
| `WarfareSystem`: yearly evaluation. **Declaration**: an active `TerritorySystem` dispute between two settlements whose `DiplomaticRelation` is `Hostile`, with no existing active `War` between them, declares war. **Battle**: each active war resolves one battle per year, weighted probabilistically by relative `MilitaryStrength`; the loser takes real `Population`/`Legitimacy` damage (a genuine consequence, not a fabricated one — `TG-STRY-050`'s "consequences, not spectacle"). **Peace**: a war ends when either side's population falls critically low or after an invented maximum number of battles, restoring some `DiplomaticRelation` score (a peace treaty, per `TG-640`'s "Peace reshapes future politics") | Any war that doesn't originate from an already-detected border dispute — this increment doesn't add a second war-trigger path (e.g. religious/ideological conflict, succession crises) |
| 3 of `TG-640`'s 10 named events: `WarDeclaredEvent` (reusing the existing `WarDeclared` event type already whitelisted in `SignificanceEvaluator`, never previously published anywhere), `BattleFoughtEvent`, `PeaceNegotiatedEvent` | `ArmyMobilized`, `BorderIncident`, `CampaignBegins`, `CityBesieged`, `SupplyLineBroken`, `VeteransReturn`, `MilitaryReform` — the other 7, each requiring concepts (mobilization, sieges, supply lines, veteran status, military reform) this increment doesn't add |
| **A real, deliberate write-back into `DiplomaticRelation`** on peace — the second RFC in this series (after `RFC-011`) to feed back into an earlier system's state rather than staying strictly read-only, justified because `TG-640` explicitly frames peace as reshaping future politics | Any other diplomatic consequence of war (alliances forming against an aggressor, trade embargoes) |

## Why `Population`/`Legitimacy` for `MilitaryStrength` specifically

`TG-640` names economic strength, education, and technology as factors sustaining armies. Of these,
only `Population` and `Legitimacy` are already real, tracked, per-settlement numbers with no
supporting system this increment would need to invent — the same reasoning every RFC since RFC-004
has used. A larger, more populous, more legitimate settlement can field and sustain more defenders,
which is both intuitive and requires zero new state.

## Mechanism

A new `WarfareSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, yearly cadence
(`IntervalTicks = SimulationTime.TicksPerYear`, matching the civilization-milestone systems'
established cadence — war and peace are not daily/monthly-scale events).

1. `Settlement.MilitaryStrength = Population * 2.0 + Legitimacy * 0.5` (invented — no `TG-640` formula
   given), recomputed each evaluation.
2. **Declaration**: for each pair in `TerritorySystem.ActiveDisputes`, if the pair's `DiplomaticRelation`
   is `Hostile` and no `War` between them is currently active, create a `War` (`IsActive = true`,
   `Intensity` starts at an invented baseline) and publish `WarDeclaredEvent`.
3. **Battle**: for each active `War`, once per yearly evaluation, compute each side's win probability
   as their share of the pair's combined `MilitaryStrength` (invented — no formula given), roll a
   winner, and apply real damage to the loser's `Population`/`Legitimacy` (invented percentages).
   Increment `Intensity` and `BattlesFought`. Publish `BattleFoughtEvent` naming the winner/loser.
4. **Peace**: after battle resolution, if the loser's `Population` has fallen below an invented
   critical threshold, or `BattlesFought` has reached an invented maximum, mark the `War` inactive and
   publish `PeaceNegotiatedEvent`; restore some `DiplomaticRelation.RelationScore` (invented amount,
   reflecting exhaustion-driven reconciliation, not full trust restoration).
5. All three events subscribed to `HistorySystem` **at introduction time** (`HistoryCategories.War`,
   the one existing category that has never had a real publisher until now — confirmed via grep,
   `"WarDeclared"` sits in `SignificanceEvaluator`'s always-High whitelist since Week 1 but nothing has
   ever published it), continuing the practice reinforced Week 12 Day 61.

## Explicitly out of scope for the next cycle

- Any army/unit/force-structure entity, command hierarchy, logistics network, military technology.
- Any second war-trigger path beyond an escalated territorial dispute.
- Occupation, sieges, guerrilla/naval/civil war variants — all named in `TG-640`'s Edge Cases but
  requiring concepts this increment doesn't model.
- Individual battle mechanics (troop positioning, tactics) — `TG-640`'s own Performance Considerations
  explicitly reject this in favor of aggregate, strategic-level resolution.

## Open questions for review before implementation starts

1. Should `MilitaryStrength` also factor in `TerritorialInfluence` (`RFC-007`) or `CarryingCapacity`
   (`RFC-008`)? (Recommendation: no — `Population`/`Legitimacy` alone already captures "a bigger,
   more legitimate settlement fields more/better defenders" without importing unrelated concepts;
   `TerritorialInfluence` is itself partly derived from `Population`, so including both would
   double-count the same signal, the same anti-double-counting reasoning `RFC-007` used for its own
   thresholds.)
2. Should peace fully reset `DiplomaticRelation.RelationScore` to neutral, or only partially restore
   it? (Recommendation: partial restoration — a war that just ended shouldn't instantly reset two
   settlements to full neutrality; `TG-640`'s own "Trauma endures" framing argues for a slower
   recovery, leaving room for the relationship to still read as `Suspicious` immediately after peace.)

## Implementation notes (Weeks 19-20, added at close-out)

- Implemented as designed: `Settlement.MilitaryStrength` (new EF migration
  `AddSettlementMilitaryStrength`, applied live) + a pure in-memory `War` entity (following the
  `LegalCase`/`Apprenticeship` pattern) + `WarfareSystem` (yearly cadence), reading
  `TerritorySystem.ActiveDisputes` directly (constructor-injected, the same DI pattern
  `SettlementsController` already uses) and `WorldState.DiplomaticRelations` to decide escalation.
  Both open questions resolved as recommended.
- **A real deployment bug was caught and fixed immediately during live verification**: the EF
  migration for `Settlement.MilitaryStrength` was never generated before this RFC's first live check
  — the API crashed on startup with `column s.MilitaryStrength does not exist` the moment it queried
  `Settlements`. Diagnosed directly via `dotnet run` (bypassing the preview tool, which reported
  "started successfully" even though the process had already crashed), fixed by generating and
  applying `AddSettlementMilitaryStrength` before re-verifying. Unlike Week 17's near-miss, this one
  wasn't caught before running — a reminder to generate the migration in the same step as adding a new
  EF-mapped field, not as an afterthought once implementation is otherwise "done."
- 7 new `WarfareSystemTests`. Two tests originally used a deliberately lopsided
  population/legitimacy pair (6 vs. 200) to force a "loser drops below critical population" peace
  path in one battle, but this violates `TerritorySystem`'s own dispute-detection requirement
  (comparable influence within 20%) — with populations that mismatched, `TerritorySystem` never even
  detects the dispute `WarfareSystem` needs as its trigger, so no war was ever declared. Caught by the
  tests themselves failing, not a design flaw in `WarfareSystem`; fixed by driving both tests through
  the max-battles-before-peace path instead, using comparable populations that keep the dispute
  detectable across several yearly evaluations.
- 3 new `HistorySystem` regression tests. Minimal Observatory surfacing added: a settlement's
  "Military" section (Strength number + active-war badges).
- Verified live against a resumed simulation run to Year 2: `MilitaryStrength` computed correctly
  from real `Population`/`Legitimacy` data across all 8 settlements (36.5-64.5). No `WarDeclared`
  organically occurred within the verification window — consistent with the established precedent for
  probability/threshold-gated mechanics: war requires both an active border dispute *and* a Hostile
  diplomatic relation to co-occur, a genuinely rare combination this particular ~2-year run didn't
  produce, not a bug — the mechanism itself is directly unit-tested end-to-end (declaration, battle
  attrition, and peace). Observatory's "Military" section rendered correctly with no console errors.
  Full verification: build clean, 222/222 unit tests, 3/3 fast integration tests, `tsc --noEmit`
  clean.
