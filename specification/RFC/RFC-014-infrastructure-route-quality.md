# RFC-014: Infrastructure — Route Quality (First Increment)

**Status:** Accepted (shipped Weeks 21-22)
**Date:** 2026-07-13
**Author:** `DEVELOPMENT_PLAN.md` Weeks 21-22
**Governing spec:** `03_Sciences/05_Civilization/TG-660_Infrastructure.md`

---

## Why this needs an RFC before a day-to-day plan

`ADR-003` (`specification/ADR/ADR-003-infrastructure-network-disposition.md`) resolved the philosophy
question that had blocked this Backlog item since Week 10: `TG-660`'s "network, not isolated
structures" framing is satisfied by extending the already-existing `TradeRoute` entity, not by
rewriting `Building`/`ConstructionSystem`. This RFC is still needed because `TG-660` gives no formula
for infrastructure quality, growth, decay, or the 10 named events (`RoadConstructed`,
`BridgeCompleted`, `CanalOpened`, `PortExpanded`, `AqueductBuilt`, `RailwayConnected`,
`InfrastructureFailure`, `ReconstructionBegins`, `CommunicationNetworkEstablished`,
`NationalHighwayCompleted`) — same as every prior RFC in this series.

## Why Route Quality, and why now

`TradeRoute` already tracks real usage history (`TotalVolumeTransported`, `TripCount`,
`LastTripTick`) but its `EconomicValue` is set once at establishment
(`Math.Max(1, 10 - distance * 0.3)`) and never revisited — a route that gets used for years has
exactly the same value as one that was established yesterday. `TG-660`'s Design Philosophy states
"Infrastructure Efficiency" and "Infrastructure Maintenance" as core dynamics: well-used connections
should become more valuable, neglected ones should decay. This RFC makes that real, reusing every
input `TradeRouteService` already computes rather than inventing new supporting state.

## Scope decision: route-level quality growth/decay, feeding back into trade volume

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| `TradeRoute.InfrastructureQuality` (0-100, invented — no `TG-660` formula given), starting low and growing with sustained trade activity, decaying when a route goes inactive | Named strategic-asset structures (bridges, ports, mountain passes, canals) as their own entities — `TG-660` names these but this increment doesn't model individual infrastructure objects, only route-level connectivity quality |
| `InfrastructureSystem`: monthly evaluation. Quality rises with recent `TripCount`/`TotalVolumeTransported` growth since the last evaluation (reusing fields `TradeRouteService` already maintains); quality decays for routes that are `IsActive: false` (already tracked by `TradeRouteService`'s existing abandonment logic) | Transportation networks in the fuller sense (footpaths vs. highways as distinct upgrade tiers with different costs) — this increment tracks a single continuous quality number, not discrete infrastructure tiers |
| A real, deliberate feedback into `TradeRouteService.ExecuteTrip`: the amount transported per trip scales up with `InfrastructureQuality` (a well-developed connection moves more goods per trip) — the third RFC in this series (after `RFC-011`, `RFC-013`) to write into an earlier system's mechanic | Utilities (water distribution, sanitation, power) — `TG-660` names these as a separate concept from transportation; this increment is transportation-network-only |
| 2 of `TG-660`'s 10 named events: `RoadConstructedEvent` (a route's quality crosses a meaningful upward threshold — reinterpreting "a road being built" as "a connection becoming road-worthy," not a literal new structure) and `InfrastructureFailureEvent` (a route's quality collapses through sustained neglect) | `BridgeCompleted`, `CanalOpened`, `PortExpanded`, `AqueductBuilt`, `RailwayConnected`, `ReconstructionBegins`, `CommunicationNetworkEstablished`, `NationalHighwayCompleted` — the other 8, each requiring named strategic-asset structures this increment doesn't model |
| — | Kingdom/region-level network analysis (`TG-660`'s "Network Efficiency," "Connectivity" as aggregate state variables) — this increment is pairwise (per-`TradeRoute`), not a settlement- or kingdom-wide network graph |

## Why trade volume/trip count specifically

`TG-660` names population growth, trade demand, military necessity, and technological progress as
infrastructure-expansion drivers. Of these, trade demand is the only one with a real, already-tracked,
per-connection number (`TradeRoute.TripCount`/`TotalVolumeTransported`) — the same "reuse an existing
field" reasoning every RFC since RFC-004 has used, now applied at the route level specifically.

## Mechanism

A new `InfrastructureSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, monthly cadence
(`IntervalTicks = 24 * 30`, matching `DecomposerSystem`'s/`FaunaSystem`'s granularity for
slow-moving, usage-driven state).

1. For each `TradeRoute`, track the trip count observed at the previous evaluation (in-memory on the
   system, the same "avoid EF migration surface for a recomputable value" posture prior RFCs used,
   though here it tracks a delta rather than the quality itself, which lives on `TradeRoute` directly
   since the Observatory needs to surface it).
2. If the route is active and gained trips since the last evaluation, `InfrastructureQuality`
   increases proportionally to trips gained (invented rate), capped at 100.
3. If the route is inactive (`IsActive: false`, already set by `TradeRouteService`'s existing
   abandonment logic), `InfrastructureQuality` decays by an invented fixed amount each month, floored
   at 0.
4. A rise crossing an invented "road-worthy" threshold (e.g. 50) publishes `RoadConstructedEvent`
   once per crossing (the same crossing-detection shape every prior RFC's system uses). A fall
   crossing back below a lower invented threshold (e.g. 10, "reverted to footpath") after having been
   road-worthy publishes `InfrastructureFailureEvent`.
5. `TradeRouteService.ExecuteTrip`'s transported amount is multiplied by
   `1.0 + InfrastructureQuality / 100.0` (a well-developed route moves up to 2x the base amount at
   maximum quality) — a genuine, bounded consequence, not a fabricated one.
6. Both events subscribed to `HistorySystem` **at introduction time** (`HistoryCategories.Trade`,
   reusing the category `TradeRouteEstablished`/`Abandoned` already use, since route quality is a
   trade-network concept, not a separate one), continuing the practice reinforced Week 12 Day 61.

## Explicitly out of scope for the next cycle

- Named strategic-asset structures (bridges, ports, canals) as their own entities.
- Utilities (water, sanitation, power) — a distinct `TG-660` concept this increment doesn't touch.
- Settlement- or kingdom-wide network-efficiency aggregation — this increment is pairwise only.
- Any change to `Building`/`ConstructionSystem` — `ADR-003` explicitly keeps that system untouched.

## Open questions for review before implementation starts

1. Should `InfrastructureQuality` decay apply to *any* inactive route, or only ones that were once
   road-worthy? (Recommendation: any inactive route — a route that never grew past a footpath has
   nothing meaningful to decay from, so the floor-at-0 clamp already makes this a no-op for routes
   that never accumulated quality; no special-casing needed.)
2. Should the trade-volume multiplier apply symmetrically to both directions of a route, or favor the
   direction with more historical volume? (Recommendation: symmetric — `TradeRoute` doesn't track
   directional volume separately, and inventing that asymmetry would require new state this RFC
   doesn't otherwise need.)

## Implementation notes (Weeks 21-22, added at close-out)

Shipped as specified, with both open questions resolved as recommended (symmetric multiplier,
no special-casing for routes that never became road-worthy).

- `TradeRoute` gained `EstablishedTick`, `LastTripTick`, `InfrastructureQuality` (0-100, default 0) —
  confirmed via reading `TradeRoute.cs` that the entity is pure in-memory (`WorldState.TradeRoutes`,
  no EF mapping), so no migration was needed for the new fields. This was confirmed deliberately,
  given the Week 17 (`SoilHealth` default mismatch) and Week 19-20 (`MilitaryStrength` migration
  forgotten entirely, real startup crash) near-misses/incidents earlier this cycle.
- `InfrastructureSystem` (monthly cadence) implements the mechanism exactly as specified: invented
  rates `QualityGainPerTrip = 2.0`, `QualityDecayPerMonthWhenInactive = 5.0`,
  `RoadWorthyThreshold = 50`, `FootpathThreshold = 10`, with hysteresis between the two thresholds
  (tracked via a single `_isRoad` dictionary per route) so a route sitting near 50 doesn't fire
  `RoadConstructed`/`InfrastructureFailure` repeatedly. A first-draft hysteresis implementation using
  a more convoluted single-line boolean expression was caught and rewritten during self-review, before
  any test was run against it.
- `TradeRouteService.ExecuteTrip` multiplies the transported amount by
  `1.0 + InfrastructureQuality / 100.0` — verified by inspection (and then empirically, via
  `TradeRouteServiceTests`) to be a no-op for all pre-existing tests, since `InfrastructureQuality`
  defaults to `0.0`.
- Both new events (`RoadConstructedEvent`, `InfrastructureFailureEvent`) were subscribed in
  `HistorySystem` at introduction time (`HistoryCategories.Trade`, severity `5.0` — learning from the
  Week 15 near-miss where severity `4.0` was silently dropped as "Low" by `SignificanceEvaluator`),
  avoiding a ninth instance of the recurring "new event never wired to history" class of bug.
- Tests: 7 new `InfrastructureSystemTests` (quality growth/decay, floor-at-zero, event firing on
  threshold crossing in both directions, no re-fire while still in the same state) plus 2 new
  `HistorySystemCivilizationEventTests` regression tests. Full suite: 231 passing (up from 222).
- Observatory: `SettlementsController.GetById` gained `TradeRoutes` (replacing the
  `TradeRelationships` placeholder), listing each route's counterpart settlement, primary good,
  active/inactive status, `InfrastructureQuality`, trip count, and total volume — following the same
  shape as `ActiveWars`/`BorderDisputes` since this is route-level data, not a settlement scalar.
  `SettlementsPage.tsx` gained an "Infrastructure" section with a quality progress bar per route.
- Live verification: this run's four settlements are all >25 tiles apart (closest pair 46 tiles,
  farthest 116), which is `TradeRouteService`'s pre-existing (Week 6) distance cap — so no trade
  routes, and therefore no organic `InfrastructureQuality` growth, occurred in this particular world.
  This is a legitimate non-finding, the same shape as Week 19-20's "no organic war": the mechanism is
  verified correct via the 7 unit tests, and the end-to-end data path (API → Observatory detail fetch
  → empty-state render) was confirmed live by inspecting the actual `/settlements/{id}` response and
  network request the UI made when opening a settlement's detail panel.
