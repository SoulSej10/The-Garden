# RFC-019: Settlement Migration & Resettlement

## Status
Accepted (this pass) — first increment.

## Context

The growth-rebalancing work logged in `DEVELOPMENT_PLAN.md` Week 28 fixed four
structural bugs that were silently capping population at zero net growth
(SoilHealth's one-way depletion ratchet, the single-Farm-per-settlement cap,
an unreachable reproduction food-per-capita threshold, and `Settlement.MemberIds`
never removing dead citizens). Live verification after all four fixes surfaced
the actual remaining bottleneck directly, not by inference: reconciling a
long-running dev world revealed **3 of 8 settlements sitting at excellent soil
health (100, 100, 69) with zero living residents**, while the settlements that
still had real population (21 and 13 members) were stuck on genuinely poor
soil, unable to reach a reproductive food surplus.

`CitizenSystem.MakeDecision` already has a full settlement-seeking code path —
but it only runs for a citizen whose `HomeSettlementId == null`. A citizen who
already belongs to a settlement has no way to ever leave it, no matter how
much better conditions are one valley over. Nothing in the simulation lets a
population redistribute itself toward carrying capacity; every settlement's
population is permanently fixed at whoever happened to found or join it,
which converts any settlement's local misfortune (poor soil, disease,
overcrowding) into permanent stagnation instead of the population naturally
seeking equilibrium the way real subsistence societies do.

## Decision

Add bounded, gradual **relocation** for already-settled citizens experiencing
sustained hardship at home, reusing the existing settlement-discovery/travel
machinery rather than inventing a parallel system.

**Trigger conditions** (all must hold, checked at most once per citizen per
day — same throttle pattern as `LastFarmWorkDay`/`LastCareForFamilyDay`):
- Citizen has a home settlement (`HomeSettlementId != null`).
- Home settlement's food-per-capita is chronically low (`< 0.5`) — a genuine
  hardship signal, not a momentary dip.
- A reachable settlement exists (within `TravelSearchRadius`, defaulted to the
  same 70-tile bound `TravelToSettlement`'s pathfinding already uses) whose
  food-per-capita is meaningfully better (`>= 2.0`) **and** has available
  housing — otherwise a citizen would just be trading one hardship for
  another, or displacing someone else.
- A small daily probability (`DailyRelocationChance = 0.03`) gates the actual
  decision, so hardship alone doesn't empty a settlement overnight — the same
  "gradual, not a boom/bust event" principle `ReproductionSystem`'s
  `DailyConceptionChance` already established for population growth.

**On relocation:** the citizen is removed from the old settlement's
`MemberIds` (mirroring the cleanup `CitizenSystem`'s `OnCitizenDied` handler
already does), given a new `CurrentGoal = "Relocate"`, and paths toward the
target settlement. Arrival (within the target's territory) completes the
move: `HomeSettlementId` updates and the citizen joins the new settlement's
`MemberIds`, exactly mirroring `JoinSettlement`'s existing logic for a
newly-arrived homeless citizen.

**Deliberately out of scope this increment** (would need its own RFC):
- Whole-family relocation (a lone citizen relocates alone; a partner or child
  independently evaluates the same trigger on their own schedule — no
  coordinated household migration yet).
- Migration driven by disease/war/politics rather than food scarcity alone.
- A citizen actively founding a *new* settlement on abandoned, fertile land
  rather than joining an existing one (the existing "found a settlement"
  path already exists for homeless citizens and is untouched here).

## Consequences

- A population can now redistribute itself away from a hardship pocket
  toward carrying capacity elsewhere, which is the mechanism the growth-
  rebalancing work's live testing showed was structurally missing — soil-poor
  settlements were correctly production-bottlenecked, but had no way to
  equalize against nearby fertile, empty land.
- The 3% daily chance and 70-tile search bound are invented numbers (no
  design doc gives any); they follow the same order-of-magnitude reasoning
  `ReproductionSystem`'s `DailyConceptionChance` used — gradual and
  observable over weeks/months, not an instant reshuffle.
- Risk: a settlement that legitimately has no fixable local hardship (e.g.
  it's the only settlement in the world) will never trigger this, since the
  "reachable, meaningfully better settlement" condition requires an actual
  alternative to exist — this is intentional, not a bug to fix later.
