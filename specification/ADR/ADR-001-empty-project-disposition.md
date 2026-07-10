# ADR-001: Disposition of Empty Placeholder Projects

**Status:** Accepted
**Date:** 2026-07-09
**Related:** `DEVELOPMENT_PLAN.md` Week 1 Day 3, `SPEC_INDEX.md` Architecture Overview

---

## Context

`TheGarden.slnx` declares 10 `src/` projects. `TG-DEV-001`/`TG-DEV-002` assigned each a
concrete responsibility (e.g. `Garden.Contracts` = DTOs/event contracts, `Garden.Shared` =
math/random/collection utilities, `Garden.Story` = story engine/narrative templates). In
practice, five of these projects contain **only a `.csproj` file — zero `.cs` source files**:

| Project | Referenced by other projects? | Orphaned logic exists elsewhere? |
|---|---|---|
| `Garden.Contracts` | **Yes** — `Garden.Engine`, `Garden.Infrastructure`, `Garden.World`, `Garden.Api` all carry a live `<ProjectReference>` to it | DTO/event-contract responsibility was absorbed into `Garden.Core/Events` and `Garden.Core/Interfaces` instead |
| `Garden.Shared` | **Yes** — same four projects reference it | Math/random/collection utility responsibility was absorbed into `Garden.Engine/Random` (e.g. `SimulationRandom.cs`) and ad hoc helpers scattered across `Garden.Engine` |
| `Garden.Story` | **No** — nothing references it | `StoryEngine.cs`, `NarrationService.cs` etc. live in `Garden.Engine/Services` and are fully implemented there |
| `Garden.Simulation` | **No** — nothing references it | No orphaned logic exists anywhere for it either |
| `Garden.Tools` | **No** — nothing references it | No orphaned logic exists anywhere for it either |

Verified by grepping every `.csproj` under `src/` and `tests/` for `<ProjectReference>` to
each of the five (2026-07-09) — the table above is not an inference, it's a direct search
result.

`TG-DEV-000`'s "Project Completion Standard" requires v1.0 to have "every Blueprint document
implemented" and a clean architecture with no dead weight. Five hollow projects sitting in the
solution — three of them entirely disconnected from the build graph — work against that
standard and actively mislead anyone reading the solution structure as documentation.

## Decision

Split the five into two different dispositions rather than treating them uniformly:

1. **Retain `Garden.Contracts` and `Garden.Shared`** in the solution, empty, for now.
   They are live nodes in the build graph — four other projects already reference them.
   Removing them would require editing every consuming `.csproj` for no functional gain today,
   and their originally-assigned responsibilities (DTOs, math/random/collection utilities) are
   real, valid future extraction targets once `Garden.Core`/`Garden.Engine` grow large enough
   that the split earns its keep. Treat them as **reserved namespaces**, not dead weight — but
   do not let them sit unexplained: this ADR is the record of why they're empty and what they're
   reserved for, until an RFC scopes the actual extraction.

2. **Remove `Garden.Simulation`, `Garden.Story`, and `Garden.Tools`** from `TheGarden.slnx` and
   delete their folders. They are true dead weight: referenced by nothing, containing nothing,
   with their intended responsibilities either already implemented elsewhere (`Garden.Story` →
   `Garden.Engine/Services/StoryEngine.cs`) or never assigned a concrete scope at all
   (`Garden.Simulation`, `Garden.Tools`). Keeping them costs nothing to compile but actively
   costs clarity: `SPEC_INDEX.md`'s Architecture Overview table had to explain, three separate
   times, why a reader shouldn't expect to find anything there. Deleting them is fully
   reversible via git history if a future increment wants to resurrect the split (e.g. if
   `Garden.Engine` becomes large enough that `StoryEngine` genuinely warrants its own project) —
   that would be a new, deliberate decision made with real content in hand, not a placeholder
   kept "just in case."

## Consequences

- `TheGarden.slnx` shrinks from 10 to 7 `src/` projects. No `ProjectReference` edits are needed
  anywhere else, since the three removed projects were never referenced.
- `dotnet build TheGarden.slnx` and `dotnet test` must be re-verified after this change (see
  `DEVELOPMENT_PLAN.md` Day 3 exit criteria) — done, see Change Log entry in `SPEC_INDEX.md`.
- `TG-DEV-001`/`TG-DEV-002`'s original project-responsibility tables now describe a superseded
  layout for `Garden.Simulation`/`Story`/`Tools`. Those documents are **not** edited by this ADR
  (per `SPEC_INDEX.md` reading order, `06_Development` docs are historical implementation
  records, not living architecture specs) — this ADR is the authoritative note that the
  Story/Simulation/Tools split described there was abandoned in favor of consolidating logic in
  `Garden.Engine`.
- If a future RFC proposes actually populating `Garden.Contracts`/`Garden.Shared`, it should
  reference this ADR for context on why they were kept empty rather than also removed.

## Alternatives considered

- **Remove all five.** Rejected: `Contracts` and `Shared` are live build-graph nodes; removing
  them is a larger, multi-file change for zero immediate benefit, and their reserved purpose is
  still plausible future work, unlike `Simulation`/`Story`/`Tools`.
- **Migrate real logic into all five to match the original `TG-DEV-002` design exactly.**
  Rejected for now: this is a substantial refactor (e.g. moving every DTO/event-contract type
  out of `Garden.Core` into `Garden.Contracts`) with no functional payoff — nothing is broken
  today by the consolidated layout. This remains a valid future RFC if the consolidation in
  `Garden.Engine`/`Garden.Core` ever becomes a genuine maintainability problem.
- **Do nothing, leave all five as-is.** Rejected: this is what produced the confusion this ADR
  exists to resolve — `TG-DEV-000`'s own completion standard treats unexplained dead weight as
  a compliance gap, and three fully-orphaned projects with zero justification fail that bar.
