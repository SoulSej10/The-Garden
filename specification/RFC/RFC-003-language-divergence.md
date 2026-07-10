# RFC-003: Language — Settlement Divergence (First Increment)

**Status:** Implemented (Week 6, 2026-07-10 - see DEVELOPMENT_PLAN.md Days 26-30)
**Date:** 2026-07-10
**Author:** DEVELOPMENT_PLAN.md (Week 5 close-out backlog triage)
**Governing spec:** `03_Sciences/04_Social/TG-510_Language.md`

---

## Why this needs an RFC before a day-to-day plan

Same reasoning as RFC-001 and RFC-002: TG-510 is thorough on vocabulary (Language Family,
Vocabulary Size, Grammar Complexity, Writing Availability, Dialect Structure, Prestige,
Stability, Translation Links) and lists 10 named events (`NewWordCreated`, `LanguageDiverged`,
`DialectFormed`, `WritingInvented`, `TranslationCompleted`, `LoanwordAdopted`, `GrammarShifted`,
`LanguageMerged`, `LanguageExtinct`, `LanguageRevived`) but gives no formula, no starting
condition (does the simulation begin with one language or many?), and no numeric threshold
for anything. `Language` has **zero code footprint** anywhere in `src/` (confirmed by grep).
This RFC picks one small, real, buildable slice rather than attempting TG-510 in full.

## Why Language, and why now

`DEVELOPMENT_PLAN.md`'s backlog lists Language/Education/Law & Justice together, each
blocked on Communication (TG-500) landing first - TG-510 itself names TG-500 as a dependency.
Communication shipped in Week 5. Of the three, Language is the most tractable next step:
TG-510's own "Simulation Rules" section names dynamics that already have a real trigger in
this codebase - "Isolation increases divergence" and "Trade encourages borrowing" map
directly onto `TradeRoute`/`DiplomaticRelation`, which already track settlement-pair contact.
Education and Law & Justice both list Language as a dependency in their own specs, so
Language should land before either.

## Scope decision: pairwise settlement divergence, one event

TG-510 describes Language Family, Vocabulary Size, Grammar Complexity, Writing, dialects,
naming systems, abstract language, translation, and five kinds of language change, all
before even reaching its 10 named events. Building all of it in one increment would repeat
the mistake RFC-001 explicitly avoided. First increment covers exactly one thing: **how far
apart two settlements' language has drifted**, driven by whether they have real, ongoing
contact (an active `TradeRoute` or a positive `DiplomaticRelation`) or none. No vocabulary,
no grammar, no writing, no naming, no translation - just a single 0-100 "Divergence" score
per settlement pair that moves toward 0 (converging) with contact and toward 100 (diverging)
without it, crossing a threshold to fire `DialectFormed` - the smallest of TG-510's 10 named
events, and the one this increment can make real without inventing five other subsystems
first.

| In scope | Deferred (needs its own RFC later) |
|---|---|
| A `LanguageDivergence` score (0-100) per settlement pair, decaying toward convergence with active trade/diplomacy contact, drifting toward divergence with isolation | Vocabulary Size, Grammar Complexity, Writing Availability, Language Prestige, Language Stability - none of these have a real trigger anywhere in this codebase yet |
| `DialectFormed` event when a pair's Divergence crosses a threshold | `LanguageDiverged`/`LanguageMerged`/`LanguageExtinct`/`LanguageRevived` - these require an actual named-Language entity with an identity that can be born, merged, or die, which this increment deliberately doesn't model |
| Settlement-pair mechanic only (mirrors `Relationship`'s pairwise pattern, just at settlement scale) | Naming systems, abstract language, translation between named languages - all require the named-Language entity this increment defers |
| A minimal Observatory surfacing: each settlement's most-diverged and most-converged neighbor | `NewWordCreated`/`WritingInvented`/`LoanwordAdopted`/`GrammarShifted` - each needs Vocabulary/Grammar/Writing state this increment doesn't add |

## Why "no named Language entity yet" is a deliberate, defensible cut

TG-510 assumes languages are first-class things that can merge, go extinct, or be revived -
that only makes sense once more than one settlement can plausibly *share* a language (e.g. a
newly founded settlement inheriting its founders' language) or a language can meaningfully
outlive its origin settlement. Nothing in this codebase currently models settlement founding
as inheriting anything from a parent settlement (`SettlementManager`/`MigrationService` both
confirmed via grep to found new settlements without any linkage back to an origin), so a
first-class Language entity would have no real "who speaks this" boundary to enforce. Divergence-as-a-pairwise-score sidesteps that gap entirely and is still a real, observable,
testable piece of TG-510's core claim ("regional separation naturally produces dialects") -
consistent with RFC-002's approach of picking the smallest slice that's still real rather
than building supporting infrastructure the spec doesn't strictly require yet.

## Data model

```csharp
// New file: Garden.World/Entities/LanguageDivergence.cs
public class LanguageDivergence
{
    public GameEntityId Id { get; init; } = GameEntityId.New();

    // Canonically ordered by LanguageSystem (lower GUID first), same
    // pattern as Relationship.EntityAId/EntityBId.
    public GameEntityId SettlementAId { get; init; }
    public GameEntityId SettlementBId { get; init; }

    // 0 = mutually intelligible, 100 = a fully distinct dialect has formed.
    // Starts at 0 (settlements assumed to start from a shared common tongue,
    // since nothing in this codebase currently models multiple starting
    // language groups) and only ever grows from isolation or shrinks from
    // contact - never reset outright, matching TG-510's framing of gradual
    // generational drift rather than sudden change.
    public double Divergence { get; set; }

    public bool DialectFormed { get; set; }
    public long EstablishedTick { get; set; }
    public long LastEvaluatedTick { get; set; }
}
```

Not EF-persisted - follows the same pure in-memory `WorldState` list pattern as
`Relationship`/`DiplomaticRelation`/`TradeRoute` (only `Citizen` and `Settlement` are
actually EF-persisted in this codebase).

## Mechanism

A new `LanguageSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, yearly cadence
(`IntervalTicks = 336`, matching `TechnologyService`/`ReligionService`/`KingdomService`'s
own cadence in `CivilizationSystem`) - TG-510's own "Performance Considerations" section
explicitly says language should change "gradually over generations rather than daily."

1. For every pair of settlements with an active `TradeRoute` (`IsActive == true`) or a
   `DiplomaticRelation` with `RelationScore > 50` (an invented threshold - "positive enough
   contact to matter"), decay `Divergence` toward 0 by a small fixed rate.
2. For every other settlement pair that already has a `LanguageDivergence` row (i.e. they
   had contact once, per TG-380/`Relationship`'s "a row only exists after a real
   interaction" convention, adapted here to "a row only exists once two settlements have
   been evaluated together at least once"), grow `Divergence` toward 100 by a smaller fixed
   rate if they currently have *no* active contact.
3. When `Divergence` crosses 70 (invented threshold, no TG-510 number given) and
   `DialectFormed` is still `false`, publish a `DialectFormedEvent` and set `DialectFormed =
   true` so the event fires exactly once per pair, not every tick past the threshold.
4. Rows are only created for settlement pairs that have ever had a real interaction
   (an active `TradeRoute` or a `DiplomaticRelation` record already exists between them) -
   mirroring `RelationshipSystem.GetOrCreate`'s "no background noise between parties that
   have never interacted" rule. Settlements with literally zero contact simply have no row
   and no divergence tracked, rather than inventing one from nothing.

## Explicitly out of scope for the next cycle

- Any named-Language entity, Vocabulary/Grammar/Writing state, or the other 9 TG-510 events
  - see "Why no named Language entity yet" above.
- Kingdom-level or citizen-level language (this increment is settlement-pair only).
- Any UI beyond a minimal per-settlement "closest/most diverged neighbor" surfacing, matching
  RFC-002's "read-only, concept over presentation" practice.
- Migration/founding-based language inheritance (would require `SettlementManager`/
  `MigrationService` changes outside this RFC's scope).

## Open questions for review before implementation starts

1. Should `Divergence` decay/growth rates differ, or be symmetric? (Recommendation:
   asymmetric and slower to converge than to diverge - TG-510 frames divergence as the
   default drift and convergence as something contact actively fights against, so
   convergence should be the "uphill" direction. Concretely: diverge at 2x the rate contact
   causes it to converge, so isolation dominates in the absence of any interaction.)
2. Is `RelationScore > 50` / `Divergence` crossing 70 the right threshold shape, or should
   this reuse `DiplomacyService`'s existing 80/60/40/20 score bands? (Recommendation: same
   answer as RFC-002 open question 2 - loosely mirror the band shape, don't literally import
   settlement-level diplomacy bands into a different mechanic; per-system invented
   thresholds are expected given TG-### specs never provide numbers.)

## Implementation notes (Week 6, added at close-out)

- Implemented largely as designed: `LanguageDivergence` entity, `LanguageSystem`
  (`IScheduledSystem`, yearly cadence matching `TechnologyService`/`ReligionService`/
  `KingdomService`), `DialectFormedEvent`, and a minimal per-settlement "most-diverged/
  most-converged neighbor" surfacing in the Observatory settlement detail panel.
- Open question 1 resolved as recommended: divergence grows at 2x the rate contact
  reverses it (`DivergenceRatePerYear = 6.0` vs `ConvergenceRatePerYear = 3.0`).
- **Live organic verification was not possible within this session**, for reasons unrelated
  to this RFC's own logic: diplomatic relations require settlements within 15 tiles (none
  qualify in this world's geography - not a bug), and trade routes - the other contact
  path - turned out to never form at all despite their own stated conditions being clearly
  met (confirmed live: a settlement with Food=74 next to one with Food=0, well within the
  distance and surplus thresholds `TradeRouteService.EvaluateTradeRoutes` itself checks).
  That is a real, separate, pre-existing bug in `TradeRouteService`, flagged via
  `spawn_task` (`task_b82147bd`) rather than fixed here, since it's out of this RFC's scope
  and affects the Economy/Trade domain independent of Language.
- Verified instead via `LanguageSystemTests.cs` (6 tests: no-row-by-default, trade-route
  contact converging, isolation diverging, `DialectFormed` firing exactly once, positive
  `DiplomaticRelation` counting as contact, canonical pair ordering) - the same fallback
  used for Week 5's Kingdom/Religion/Technology triggers when the organic path is rare or
  broken. Backend endpoint and frontend rendering were both confirmed live: the settlement
  detail panel renders correctly (including the correct absence of the Language section)
  for a settlement with no tracked divergence yet, with no console errors.
- **Separate finding, also out of scope:** `CivilizationSystem`'s internal "yearly"
  threshold (336 ticks) does not match `SimulationTime`'s actual year length (24 * 30 * 12
  = 8640 ticks) - it's really closer to a 14-day cadence. `LanguageSystem` deliberately
  matches this existing (mislabeled) convention for consistency with
  `TechnologyService`/`ReligionService`/`KingdomService`/`CultureService`, all of which
  share the same `_lastYearlyTick >= 336` pattern in `CivilizationSystem.Execute`. Renaming
  or correcting this affects five systems at once and deserves its own dedicated look
  rather than a fix folded into this RFC.
