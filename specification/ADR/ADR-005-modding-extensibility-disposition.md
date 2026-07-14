# ADR-005: Disposition of Modding & Extensibility

**Status:** Accepted
**Date:** 2026-07-14
**Related:** `DEVELOPMENT_PLAN.md` Week 26, Backlog ("Modding & Extensibility"), `05_Observatory/TG-OBS-009_Modding_Extensibility.md`

---

## Context

`TG-OBS-009` was read in full to scope Week 26's day-to-day plan. Unlike every other Living Document
this project has built an increment against (`TG-670` Science & Technology, `TG-STRY-040` Legends &
Myths, `TG-660` Infrastructure), `TG-OBS-009` contains **no concrete, implementable content at all**:

- Its "State Variables" section (Installed Modules, Visualization Extensions, Custom Layers, Community
  Packages, Research Templates, Localization Resources, Accessibility Modules, Integration Settings)
  describes a plugin-loading system with zero data model, zero formula, zero named event — every
  other TG-### document's State Variables section is a concrete list of things this project's
  `WorldState`/entities actually track or grow; here it is a wishlist of future subsystems.
- Its "Extensible Architecture," "Community Contributions," "Educational Applications," and "Research
  Support" sections are exclusively phrased as *"Future modules may include..."* / *"Future versions
  may allow..."* — every example given (biodiversity modules, community map overlays, research
  templates, cloud-based collaboration) is explicitly hypothetical, not a mechanic to build now.
- Its one testable, present-tense claim — *"No duplicate world states should exist. All Observatory
  modules observe the same underlying simulation"* — is already true of the current architecture:
  `WorldState` is a single DI-registered singleton (`Garden.Infrastructure/Configuration/DependencyInjection.cs`),
  and every Observatory page/controller reads from it or the shared `HistoricalArchive`/`SaveLoadService`
  — there is no duplicate state to eliminate, and no code change is needed to satisfy this principle.

The Backlog table already anticipated this outcome: *"Explicitly deferred by its own spec until core
Observatory work is done — likely to move later, not sooner."* This ADR formalizes that anticipation
into a decision, following `ADR-001`'s precedent for a spec/backlog item with no real content to build
against (there, five placeholder projects with no source; here, a document with no implementable
scope).

## Decision

**Do not attempt a "first increment" for Modding & Extensibility this cycle. Formally defer it, and
use Week 26's freed time on genuine, already-identified leftover fixes instead.**

Specifically:

1. `TG-OBS-009` remains exactly as written — a Living Document describing a future plugin architecture.
   No new entity, event, or endpoint is added in its name, since doing so would mean inventing scope
   this project has no spec basis for (the same reasoning `ADR-004` used to reject building `TG-670`'s
   full state-variable model in one pass, applied here to an entire document rather than one section).
2. The one testable claim it does make ("no duplicate world states") is confirmed already satisfied by
   the existing single-`WorldState`-singleton architecture — recorded here as a verification, not a
   fix, so a future reader doesn't wonder whether it was silently skipped.
3. Week 26's remaining time is spent on real, previously-identified leftovers surfaced while
   investigating this ADR and while closing out Week 25: `EconomySystem.ProcessProduction` tracked
   `_totalGoodsCrafted` but never published `GoodsCraftedEvent` despite `HistorySystem` already
   subscribing to it and `"GoodsCrafted"` already sitting in `SignificanceEvaluator`'s whitelist — the
   same "tracked but never connected" bug class as Week 23's `BuildingPlannedEvent` fix. Fixed with a
   regression test.
4. A stale planning-document leftover was also found: `DEVELOPMENT_PLAN.md`'s Week 27 timeline row
   still listed *"`History/search`'s `totalRecords` bug"* as an outstanding item, but that bug was
   already fixed Week 12 Day 61 (confirmed via the Backlog table's own strikethrough entry for it).
   Corrected as part of this week's close-out.

## Consequences

- `TG-OBS-009`'s eventual real first increment (if one is ever scoped) will need its own future ADR
  once the Observatory has a genuine extension point to build against — e.g. a real plugin-registration
  API, a first community-contributed module, or a concrete "Custom Layer" data model. Nothing in this
  ADR blocks that; it only declines to invent one prematurely.
- The Backlog table's "Modding & Extensibility" row is retired with this disposition, matching the
  pattern every other resolved Backlog row uses (strikethrough + one-line resolution note).
- `TradeCompletedEvent` (flagged as dead code since Week 3 Day 13, confirmed still unpublished as of
  this ADR) is deliberately **not** wired up alongside `GoodsCraftedEvent` — it models citizen-to-citizen
  barter (`FromCitizenId`/`ToCitizenId`), a different mechanic than anything `TradeRouteService`
  (settlement-to-settlement) or `EconomySystem` (settlement-to-storage) currently implements, and
  inventing a citizen-barter mechanic just to give this event a publisher would be scope creep beyond
  "fix a leftover," not a genuine leftover fix itself. It remains a documented, candid known gap.

## Alternatives considered

- **Build a minimal "plugin registration" API for the Observatory anyway, to have *something* to show
  for Week 26.** Rejected: with no data model or named event in `TG-OBS-009` to build against, any such
  API would be pure invention with no spec basis — exactly the kind of speculative architecture this
  project's discipline (build against real, grounded specs) exists to avoid.
- **Skip Week 26 entirely and roll straight into Week 27.** Rejected: this would waste real, valuable
  time — the `GoodsCraftedEvent` leftover and the stale Week 27 backlog line are genuine, confirmed
  issues worth fixing regardless of what Week 26's headline feature is.
- **Treat "no duplicate world states" as needing an explicit test or refactor to "prove."** Rejected:
  there is nothing to refactor (the architecture already satisfies this), and writing a test that
  asserts "there is only one `WorldState` registered in DI" would test the DI container's own
  singleton lifetime guarantee, not any code this project wrote.
