# RFC-017: Save/Load Fidelity + Timeline Branching (First Increment)

**Status:** Implemented (Week 25, 2026-07-14 - see DEVELOPMENT_PLAN.md Days 122-126)
**Date:** 2026-07-14
**Author:** `DEVELOPMENT_PLAN.md` Week 25
**Governing spec:** `05_Observatory/TG-OBS-007_Save_Load_Replay.md`

---

## Why this needs an RFC before a day-to-day plan

`TG-OBS-007`'s Core Principle is *"Every save is a historical snapshot of an evolving world"* and its
World Persistence section names a specific list of what a save must preserve: Geography, Climate,
Ecosystems, Citizens, Families, Civilizations, Infrastructure, Scientific progress, Historical records,
World Memory — *"The restored world should behave exactly as it did when the snapshot was created."*
Its Timeline Branching section is concrete and testable: *"Loading an earlier save creates the
possibility of divergent histories... Each resulting timeline becomes its own valid historical
branch... No branch is considered the 'correct' one."* This RFC is needed because the existing
`SaveLoadService` (Week 1-era) doesn't satisfy either of these two concrete requirements yet, and
gives no formula for the many advanced Replay features (`TG-OBS-007` also names frame-by-frame
scrubbing, comparative replay, bookmarks, annotations) this increment doesn't attempt.

## Why now, and what's actually broken today

Reading `SaveLoadService.cs` in full surfaced two real, confirmed defects, not just missing features:

1. **`LoadAsync` doesn't restore `WorldState.CurrentTime`** — after loading a save, the simulation
   clock keeps whatever tick it was at *before* the load, not the tick the save was taken at. A
   restored world's citizens/settlements would report as existing at the wrong point in time relative
   to the clock driving every `IScheduledSystem`.
2. **`SaveAsync`/`LoadAsync` only round-trip `Citizens`, `Settlements`, and `HistoryRecords`** — every
   other `WorldState` collection (`Kingdoms`, `DiplomaticRelations`, `TradeRoutes`,
   `LanguageDivergences`, `Apprenticeships`, `LegalCases`, `Infections`, `Wars`,
   `SettlementTechnologies`, `Legends`, `Religions`) is silently left at whatever the *live* world
   state was, not reset to the saved snapshot. This directly contradicts `TG-OBS-007`'s "restored world
   should behave exactly as it did" — loading an older save today produces a Frankenstein world: old
   citizens/settlements next to whatever civilization-level state happened to exist at load time.

Timeline Branching is entirely unimplemented: there is no concept of save lineage at all — every save
is an independent, disconnected file with no record of which save (if any) it continued from.

## Scope decision: fix save/load fidelity, add lineage tracking — no replay UI

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| Fix `LoadAsync` to restore `WorldState.CurrentTime` and every civilization-level `WorldState` collection listed above, not just `Citizens`/`Settlements`/`HistoryRecords` | Historical Playback (frame-by-frame scrubbing, jump-to-event, slow motion) — `TG-OBS-007` names these as Temporal Controls, but they require a fundamentally different capability (replaying *forward* from an arbitrary point without re-simulating) this increment doesn't build |
| `WorldSnapshot` gains `Id` (new GUID) and `ParentSaveId` (nullable) — `SaveLoadService` tracks which save (if any) was most recently loaded, and stamps that as the new save's parent, giving every save a real position in a branch tree | Comparative Replay (side-by-side comparison of two saves/civilizations) — a real, valuable `TG-OBS-007`-named feature, but a separate UI/analysis capability on top of the lineage data this RFC establishes |
| `GetTimeline()`: returns every save with its `Id`/`ParentSaveId`/`Name`/`Tick`/`SavedAt`, so the Observatory can render "this save continued from that save" | Save Management's "Player annotations, Bookmarks, Custom labels" — real `TG-OBS-007` features, deferred as their own increment since they need new save metadata this RFC doesn't add |
| Minimal Observatory surfacing: a Timeline view listing saves with their parent linkage (which save each one branched from) | Replay exports, research mode, community world sharing — all explicitly "Future Integration" in `TG-OBS-007` itself |
| Unit tests for `SaveLoadService` (currently has zero, despite existing since Week 1) | — |

## Why `ParentSaveId` (a single-parent tree), not a full DAG or explicit "fork" action

`TG-OBS-007`'s own example (Year 1200 → Save → Timeline A / reload Year 1200 → different
interventions → Timeline B) describes branching as an implicit consequence of loading an old save and
continuing, not a deliberate "create branch" action the player takes. The simplest model that captures
this exactly is: each save records which save (if any) was loaded immediately before it was created.
Since a running world can only ever have continued from one most-recently-loaded save at a time, a
single nullable `ParentSaveId` per save is sufficient to reconstruct the full branch tree — no new
"branch" entity needed, following this series' "don't invent new supporting state you don't need" logic.

## Mechanism

Extending the existing `SaveLoadService` (not a new system — this is a request/response service the
API already calls, not something `IScheduledSystem` drives).

1. `WorldSnapshot` gains `Id` (`Guid`, new) and `ParentSaveId` (`Guid?`, new).
2. `SaveLoadService` tracks `_currentParentSaveId` (nullable, in-memory, resets to `null` on process
   start — a save made before any load in this session has no parent).
3. `SaveAsync`: the new snapshot's `Id` is a fresh GUID; its `ParentSaveId` is whatever
   `_currentParentSaveId` currently holds.
4. `LoadAsync`: after successfully restoring state, sets `_currentParentSaveId = snapshot.Id` — any
   *next* save made in this session will record this load as its parent, capturing the branch point
   `TG-OBS-007`'s example describes.
5. `LoadAsync` is fixed to also restore `WorldState.CurrentTime` and every civilization-level
   collection (`Kingdoms`, `DiplomaticRelations`, `TradeRoutes`, `LanguageDivergences`,
   `Apprenticeships`, `LegalCases`, `Infections`, `Wars`, `SettlementTechnologies`, `Legends`,
   `Religions`) — each cleared and re-populated from the snapshot, mirroring the existing
   `Citizens`/`Settlements` pattern exactly.
6. `GetTimeline()` (new): returns every save's `Id`/`ParentSaveId`/`Name`/`Tick`/`SavedAt` as a flat
   list — the Observatory builds the tree client-side from parent pointers, the same "server returns
   flat data, client renders structure" pattern already used for `TerritorySystem.ActiveDisputes`, etc.

## Explicitly out of scope for the next cycle

- Frame-by-frame/scrubbing replay playback — this increment only fixes save/load fidelity and adds
  lineage tracking, it does not add a way to "watch" a past period without re-simulating forward.
- Comparative Replay, bookmarks, player annotations, custom labels.
- Replay exports, research mode, community world sharing.

## Open questions for review before implementation starts

1. Should `ParentSaveId` reset to `null` after a fresh world reset (`SystemController.ResetWorld`), or
   persist across resets? (Recommendation: reset to `null` — a reset world has no history at all, so
   treating it as a fresh, parentless lineage root is the only coherent choice.)
2. Should the civilization-level collections that are currently missing from `WorldSnapshot`
   (`Kingdoms`, `TradeRoutes`, etc.) all be added in one pass, or incrementally? (Recommendation: one
   pass — they're all the same fix (add to snapshot, clear + repopulate on load) applied to a list of
   collections, not independent design decisions; splitting them across weeks would leave the "restored
   world should behave exactly as it did" bug only partially fixed.)

## Implementation notes (Week 25, added at close-out)

Shipped as specified, with both open questions resolved as recommended (`ParentSaveId` resets on
world reset; all missing collections added in one pass).

- `WorldSnapshot` gained `Id`/`ParentSaveId`, plus every civilization-level collection previously
  missing (`Kingdoms`, `DiplomaticRelations`, `TradeRoutes`, `LanguageDivergences`, `Apprenticeships`,
  `LegalCases`, `Infections`, `Wars`, `SettlementTechnologies`, `Legends`, `Religions`).
  `LoadAsync` now restores `WorldState.CurrentTime` and clears+repopulates every one of these,
  matching the existing `Citizens`/`Settlements` pattern exactly.
- `SaveLoadService._currentParentSaveId` tracks the most recently loaded save in-process;
  `SystemController.ResetWorld` now calls the new `ResetLineage()` so a fresh world starts a
  parentless lineage.
- `GetTimeline()` returns a flat list of every save's `Id`/`ParentSaveId`/`Name`/`Tick`/`SavedAt`; a
  new `GET /system/timeline` endpoint exposes it. `ProductionDashboardPage.tsx` gained a "Timeline"
  section rendering the branch tree client-side from `parentSaveId` pointers.
- A naming collision was caught immediately at build time: `TimelineEntry` already existed in
  `TimelineService.cs` for an unrelated History Explorer concept — renamed to `SaveTimelineEntry`.
- Tests: 6 new `SaveLoadServiceTests` (currently-zero test coverage for a service that's existed since
  Week 1) covering `CurrentTime` restoration, civilization-level collection restoration, first-save-has-
  no-parent, save-after-load-records-parent, lineage reset, and load-of-nonexistent-save.
- Live verification: exercised save → load → save directly via REST against a resumed 8-settlement
  world. Confirmed `ParentSaveId` lineage end-to-end (a branch save's `parentSaveId` correctly pointed
  to the root save's `id`), and confirmed the `CurrentTime` fix specifically — advanced the sim from
  tick 0 to tick 52 while running, saved, ran further, then loaded the original save and confirmed the
  clock reset to tick 0, not whatever tick was live at load time. Observatory's Timeline section
  rendered a real save with correct tick/timestamp, no console errors. Full verification: build clean,
  250/250 unit tests, 3/3 fast integration tests, `tsc --noEmit` clean.
