# ADR-002: Disposition of `AgricultureSystem`'s Crop-Growth Hardcoding

**Status:** Accepted
**Date:** 2026-07-13
**Related:** `DEVELOPMENT_PLAN.md` Week 13, Backlog ("Life Sciences foundation"), `RFC/RFC-008-population-ecology-carrying-capacity.md`

---

## Context

Since Week 10's Day 49 assessment, the Backlog table has carried this note: *"`AgricultureSystem`/`CitizenSystem` still hardcode biology this volume is supposed to own — untangling that is itself a design question, and blocks Population Ecology specifically since it needs food/population dynamics `AgricultureSystem` currently owns outright."* This ADR resolves that question before Week 14 can scope a real day-to-day plan for Population Ecology (`TG-240`).

Read in full, `src/Garden.Engine/Systems/AgricultureSystem.cs` (28 lines of actual logic) computes a farm's yield as:

```
growthModifier = season switch (Spring: 1.5, Summer: 1.2, Autumn: 0.8, Winter: 0.1)
if tile.Moisture < 0.2: plantedCrops *= 0.5
yield = plantedCrops * growthModifier * 2.0
```

`TG-210_Flora.md` lists "Agricultural Crops" once, as a single entry in a 12-item plant-category list (line 108) — no species model, no growth-rate table, no soil/nutrient model. `TG-240_Population_Ecology.md` (read in full for this ADR) is the more relevant document: it names `Agriculture` as one of nine things Population Ecology "directly influences" (line 367), and separately requires a `CarryingCapacity` state variable derived from "Biome, Climate, Water, Food, Habitat, Season, Environmental health" — but, consistent with every other TG-2xx document, gives no formula for either.

Separately, `ReproductionSystem.cs` already implements a real, working, de facto carrying-capacity gate: reproduction requires both `settlement.HasAvailableHousing` and `foodPerCapita >= 3.0` (`settlement.Storage.GetQuantity("Food") / memberCount`), with an explicit comment explaining this is deliberately conservative so population growth can't outpace food production. This is not a stub — it is a real, tested, already-live mechanic (`SurvivalSimulationTests.Population_RemainsViable_AcrossThreeYears` exercises it over 3 simulated years).

So the actual question is narrower than "fix biology hardcoding" suggests: **should Population Ecology's `CarryingCapacity` concept be built by reusing `AgricultureSystem`'s existing `Food` output and `ReproductionSystem`'s existing housing/food-per-capita gate, or does `AgricultureSystem` need to be rearchitected around a real Flora species/soil model first?**

## Decision

**Reuse the existing `AgricultureSystem`/`ReproductionSystem` outputs as Population Ecology's carrying-capacity inputs. Do not rearchitect `AgricultureSystem` this cycle.**

Specifically:

1. `AgricultureSystem`'s season/moisture-modified yield formula stays exactly as-is. It has no TG-210 formula to conform to (the spec gives none), and it already produces the one signal Population Ecology actually needs: real, live `Settlement.Storage["Food"]` numbers that respond to season and moisture. **A real gap found while writing this ADR**: `AgricultureSystem` has zero dedicated unit tests (confirmed via grep — only exercised indirectly through `SurvivalSimulationTests`'s multi-year integration runs) despite existing since early in this project. Adding direct coverage is scheduled as Week 13's Day 62 (see `DEVELOPMENT_PLAN.md`) — a contained gap-fill, not a design change, since this ADR is choosing to keep the formula, not alter it.
2. `RFC-008` (Population Ecology, Increment 1 — see that document) will derive `Settlement.CarryingCapacity` from the same two inputs `ReproductionSystem` already gates on: food-per-capita and housing availability — the identical "reuse an existing field rather than invent new supporting systems" posture RFC-004 used for `Intelligence`, RFC-005 used for `Legitimacy`, and RFC-007 used for `Population`/`Legitimacy`.
3. A real Flora species/soil/nutrient model (the thing that would make `AgricultureSystem`'s formula "not hardcoded") remains explicitly out of scope, tracked as its own future increment alongside Fauna and Disease — this ADR does not schedule it, it only removes it as a blocker for Population Ecology.

## Consequences

- Week 14's `RFC-008` can proceed without first resolving a Flora/soil design question that TG-210 doesn't even specify — the same reasoning RFC-002/003/004/005 already used to avoid blocking on Groups/Social Norms.
- `AgricultureSystem`'s formula remains an invented placeholder, exactly as candid as every other invented formula in this codebase (`TerritorialInfluence`, `LanguageDivergence`'s convergence rate, `EducationSystem`'s Intelligence-gap threshold, etc.) — this ADR makes that explicit rather than leaving it as an unstated assumption.
- The Backlog table's "Life Sciences foundation" row is narrowed: Population Ecology Increment 1 is unblocked and scheduled (Week 14); Fauna, Disease, Evolution, and a real Flora species model remain future increments with their own RFCs.
- If a future RFC does redesign `AgricultureSystem` around real Flora species, it must re-examine whatever `RFC-008` builds on top of `Food`/housing, since that RFC's `CarryingCapacity` formula would need to be revisited too — this ADR's reuse decision is a deliberate, documented dependency, not an accident.

## Alternatives considered

- **Design a real Flora-driven crop system first, then build Population Ecology on top of it.** Rejected: `TG-210` gives no species/soil/nutrient model to design against (confirmed by reading the document in full — "Agricultural Crops" is a single list entry, not a subsection), so this would mean *inventing* a whole new biology layer before Population Ecology can even start, expanding this decision into open-ended greenfield design work far larger than a single week, with no spec basis to constrain the invention.
- **Block Population Ecology entirely until a dedicated Agriculture RFC ships.** Rejected: this is what has already happened since Week 10 (Days 49-50) — the Backlog row has sat unscheduled for two cycles specifically because of this framing. Every week this stays blocked is a week TG-240's foundational population dynamics (already partially real via `ReproductionSystem`) go unrecognized and un-surfaced.
- **Rewrite `AgricultureSystem`'s formula now, without a real Flora model, just to remove the word "hardcoded."** Rejected: replacing one invented formula with another invented formula produces no actual improvement in fidelity to spec (there is no spec formula either version could match) — pure churn.
