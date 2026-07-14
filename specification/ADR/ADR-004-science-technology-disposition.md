# ADR-004: Disposition of `TechnologyService`'s Global, Tree-Shaped Technology Model

**Status:** Accepted
**Date:** 2026-07-14
**Related:** `DEVELOPMENT_PLAN.md` Week 23, Backlog ("Science & Technology redesign"), `03_Sciences/05_Civilization/TG-670_Science_Technology.md`, `RFC/RFC-015-technology-independent-discovery.md`

---

## Context

`TG-670` opens with an explicit disclaimer: *"The Garden models discovery as an emergent process rather than a predefined technology tree."* Its Edge Cases section separately lists, as things "the simulation should support": Independent discovery, Lost technologies, Scientific stagnation, Knowledge monopolies, Suppressed research, Technological divergence, Parallel inventions, Rediscovery of forgotten knowledge, Unexpected breakthroughs — closing with *"Civilizations need not advance at the same pace or in the same direction."*

Read in full, `src/Garden.World/Entities/Technology.cs` and `src/Garden.Engine/Services/TechnologyService.cs` are exactly the thing the spec disclaims, plus one deeper structural problem the "tree" framing doesn't even fully capture:

1. `Technology.AllTechnologies` is a single static list of 19 named technologies, each with a fixed category and a fixed `ProgressRequired` threshold (e.g. Agriculture: Basic Farming 50 → Crop Rotation 80 → Irrigation 120 → Advanced Plowing 180) — a linear, ascending-cost catalog per category, matching "predefined technology tree" plainly.
2. **More seriously**: `WorldState.Technologies` holds one shared, mutable `Technology` instance per named technology, populated once via `InitializeTechnologies()`. `EvaluateTechnology()` then loops over *every* settlement in the world and has each one push progress onto the *same* shared `Technology.CurrentProgress`/`IsDiscovered` fields. The first settlement whose contribution crosses the threshold sets `IsDiscovered = true` globally — permanently locking every other settlement out of ever discovering that technology themselves. Confirmed by reading `EvaluateTechnology()`'s loop structure directly (no per-settlement dictionary, no settlement-keyed lookup — just one `foreach (var settlement ...)` mutating the same objects every settlement shares).

This second point means "Independent discovery," "Parallel inventions," "Technological divergence," and "advance at their own pace" are not just unmodeled — they are *structurally impossible* under the current design, since there is only one copy of each technology's progress/discovery state in the entire simulation, not one per settlement.

`TG-670`'s own richer state variables (Knowledge Base, Research Capacity, Innovation Rate, Educational Quality, Scientific Institutions, Technological Complexity, Knowledge Diffusion, Engineering Capability, Scientific Culture, Discovery Momentum) describe a far larger system than one week can build from nothing — consistent with the Backlog's own estimate ("likely resolves to a doc/reality reconciliation rather than a full rebuild").

## Decision

**Keep the named-technology catalog (`Technology.AllTechnologies`) as a knowledge catalog, but make progress and discovery per-settlement instead of globally shared. Tie research contribution to a signal `TG-670` explicitly names ("Education increases research capacity") that the current formula ignores entirely.**

Specifically:

1. The catalog of named technologies/categories/`ProgressRequired` thresholds is **not** the part of "predefined technology tree" this ADR treats as a real problem — a list of what *can* be discovered is a reasonable knowledge domain, not the disqualifying issue. The disqualifying issue is that `IsDiscovered`/`CurrentProgress` currently live on one shared object per technology instead of one per settlement, which is what makes independent/parallel/divergent discovery impossible. `RFC-015` fixes that specifically, following this series' established "new pure in-memory join entity for per-relationship/per-instance state" pattern (`LegalCase`, `Apprenticeship`, `War`, `LanguageDivergence`): a new `SettlementTechnology` entity keyed by `(SettlementId, TechnologyName)` replaces the single shared `Technology.CurrentProgress`/`IsDiscovered` fields for live simulation state.
2. `TechnologyService.CalculateProgress` currently scales contribution only by raw population count — `TG-670`'s Simulation Rules explicitly state *"Education increases research capacity"*, a rule the current formula doesn't implement at all despite `EducationSystem`/`Apprenticeship` (`RFC-004`) already existing. `RFC-015` reuses the settlement's average `Citizen.Attributes.Intelligence` (the same aggregate signal `EvolutionSystem`, Week 16, already computes and archives) as a contribution multiplier — the same "reuse an existing field" discipline this whole series has used since `RFC-004`, this time finally implementing a named Simulation Rule the codebase has ignored since Week 3.
3. Everything else `TG-670` names (Knowledge Base, Research Capacity, Innovation Rate as distinct tracked state; Scientific Institutions as buildable entities; Lost technologies/Knowledge monopolies/Suppressed research/rediscovery mechanics) remains explicitly out of scope for this increment — the same "first real, tested increment, not full lifetime parity" posture every RFC in this series has taken.

## Consequences

- Each settlement can now genuinely discover a technology independently of every other settlement's progress — `TechnologicalDivergenceEvent`-shaped observability becomes possible (RFC-015 names the concrete event), where it was structurally unreachable before.
- Two settlements can each discover the same technology on their own timeline — real "parallel invention," not two settlements racing for one shared unlock.
- `CivilizationController.GetTechnology()` and the Observatory's technology surfacing move from a single world-wide list to a per-settlement view — a genuine (if modest) API/UI shape change, not just an internal refactor, since the old global list is being retired.
- `Settlement.TechnologyProgress` (the separate accumulator `CultureService`'s `> 50` "Knowledge" trait-weighting threshold already reads) is untouched — it was already per-settlement and doesn't need to change.
- Knowledge Base/Research Capacity/Innovation Rate/Discovery Momentum as their own tracked state variables, Scientific Institutions as buildable entities, and the "lost/suppressed/monopolized knowledge" edge cases all remain unaddressed — tracked as future increments, not silently dropped.

## Alternatives considered

- **Build the full `TG-670` state-variable model (Knowledge Base, Research Capacity, Innovation Rate, Scientific Institutions, etc.) in one pass.** Rejected: `TG-670` gives no formulas for any of these (consistent with every other TG-6xx document), so this would mean inventing an entire new subsystem from nothing in a single week — far outside this project's established "one real increment per cycle" discipline, and outside the Backlog's own 1-week budget for this item.
- **Leave the global shared-technology model as-is and only change the documentation to match reality (declare `TG-670`'s "not a tree" framing aspirational).** Rejected: the global-sharing bug is worse than "it's a tree" — it actively prevents Independent Discovery/Parallel Inventions/Technological Divergence from ever being *possible*, not just unmodeled. Documenting around a structural bug rather than fixing it would leave a real defect in place.
- **Add a full "Scientific Institution" entity (schools/universities/laboratories) this cycle.** Rejected: `EducationSystem`'s `Apprenticeship` mechanic already models the closest thing to an institution this codebase has, and no spec formula exists for how a University vs. an Apprenticeship should differently affect research — deferred alongside the other state-variable work.
