# ADR-003: Disposition of the Infrastructure-as-Network Philosophy Conflict

**Status:** Accepted
**Date:** 2026-07-13
**Related:** `DEVELOPMENT_PLAN.md` Week 21, `RFC/RFC-014-infrastructure-route-quality.md`

---

## Context

`TG-660_Infrastructure.md`'s Scientific Basis section states outright: *"The Garden models
infrastructure as a dynamic network whose value lies in connectivity rather than individual
structures."* Its Performance Considerations reinforce this: *"Infrastructure should be modeled as
connected regional networks rather than isolated structures whenever possible."*

The current codebase's only infrastructure-adjacent system is `ConstructionSystem`/`Building.cs`
(Week 1-era): a `Building` is a single structure (`House`, `Farm`, `Well`, `Shelter`, `Storage`,
`Workshop`) at one tile, belonging to one settlement, with **zero inter-settlement connectivity
concept anywhere** — confirmed by reading the full entity and its supporting system: no road, no
network edge, no connection state between any two settlements exists in `Building.cs` or
`ConstructionSystem.cs`. This is the exact "isolated structures" model `TG-660` explicitly rejects.

This is the philosophy conflict the Backlog table has carried since Week 10: *"Spec explicitly
rejects the building-centric model the current `ConstructionSystem`/`Building.cs` uses — this is a
philosophy-vs-implementation conflict that needs a decision, not just new code."*

## Decision

**Do not touch `Building`/`ConstructionSystem`. Introduce infrastructure-as-network as an entirely
separate, additive layer on top of the existing `TradeRoute` entity, which is already exactly the
inter-settlement connection `TG-660` describes.**

Specifically:

1. `Building`/`ConstructionSystem` are not in conflict with `TG-660` in the way the Backlog framing
   implied. `TG-660` never says individual structures shouldn't exist — houses, farms, and wells are
   real, local, single-settlement concerns `TG-660` doesn't govern at all (it depends on `TG-650`
   Cities & Urbanization for that layer, not the other way around). The actual conflict is narrower:
   this codebase has never modeled the *network* `TG-660` is about — the connections *between*
   settlements — at all. Rewriting `Building.cs` to somehow "become a network" would solve nothing,
   since `Building` was never meant to represent inter-settlement infrastructure in the first place.
2. `TradeRoute` (`Garden.World/Entities/TradeRoute.cs`, live since Week 6 via `TradeRouteService`) is
   **already** a real, tracked, per-pair connection between two settlements, with real usage history
   (`TotalVolumeTransported`, `TripCount`, `LastTripTick`) and a static `EconomicValue` set once at
   establishment and never revisited. This is the closest thing to a "road" already in the codebase —
   it just has no notion of the connection itself improving or decaying over time. `RFC-014` extends
   it with exactly that: an `InfrastructureQuality` field that grows through sustained use and decays
   through neglect, the same "increasing returns to connectivity" `TG-660`'s Design Philosophy names.
3. This reuses an existing, already-tested entity and system rather than inventing a parallel `Road`/
   `Network` concept — the same "reuse an existing field/entity" posture every RFC since RFC-004 has
   used, now applied to an entire pre-existing system rather than just a field.

## Consequences

- `Building`/`ConstructionSystem` remain exactly as they are — no risk introduced to the existing
  construction/economy pipeline that every prior week's live verification has depended on.
- `TradeRouteService.ExecuteTrip` gains one new read (the route's `InfrastructureQuality` scaling the
  transported amount) — a small, contained change to an already-existing method, not a rewrite.
- The Backlog table's "Infrastructure-as-network" row is resolved without the "philosophy conflict"
  framing it originally carried — the conflict was a scoping question (what does "network" mean given
  what already exists?), not an implementation defect requiring `Building.cs` to change.
- Genuine transportation infrastructure in `TG-660`'s fuller sense (roads as a `Building`-adjacent
  structure, bridges, ports, mountain passes as named strategic assets) remains future scope — this
  ADR resolves only the philosophy question blocking a first increment, it doesn't claim to implement
  every concept `TG-660` names.

## Alternatives considered

- **Rewrite `Building.cs` into a graph/network model.** Rejected: `Building` was never intended to
  represent inter-settlement connectivity — it's a per-settlement structure, exactly the layer
  `TG-650` Cities & Urbanization (not `TG-660`) governs. Rewriting it to "become a network" would
  conflate two different specs' concerns and risk breaking `ConstructionSystem`'s entire existing
  pipeline (worker assignment, build progress, resource costs) for no actual gain — `TG-660`'s
  "connectivity" concept has nothing to do with how a single house gets built.
- **Invent a new `Road`/`Network` entity from scratch, parallel to `TradeRoute`.** Rejected: this
  would duplicate state that already exists. `TradeRoute` already tracks exactly the pair-of-
  settlements-plus-usage-history shape a road-as-infrastructure needs; a second, parallel entity
  would create two sources of truth for what is functionally the same connection.
- **Defer Infrastructure entirely until a later ADR resolves it more thoroughly.** Rejected: this is
  what has already happened since Week 10 — the Backlog row has sat unscheduled for over ten cycles
  specifically because of the "philosophy conflict" framing. The actual resolution (reuse `TradeRoute`)
  is available now and doesn't require solving every open question in `TG-660` first.
