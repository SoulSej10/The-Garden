# RFC-002: Communication — Knowledge Diffusion (First Increment)

**Status:** Implemented (Week 5, 2026-07-10 - see DEVELOPMENT_PLAN.md Days 21-25)
**Date:** 2026-07-09
**Author:** DEVELOPMENT_PLAN.md Week 4 Day 20 (sprint close / backlog triage)
**Governing spec:** `03_Sciences/04_Social/TG-500_Communication.md`

---

## Why this needs an RFC before a day-to-day plan

Same reasoning as RFC-001: TG-500 is thorough on vocabulary (Sender/Receiver/Message/Medium/
Context, Information Fidelity, Credibility, Communication Networks) but gives no formula, no
data type, no decay/spread rate. `Communication` has **zero code footprint** anywhere in
`src/` (confirmed by grep). This RFC picks one small, real, buildable slice rather than
attempting TG-500 in full.

## Why Communication, and why now

`DEVELOPMENT_PLAN.md`'s original backlog listed "Communication / Language / Education / Law &
Justice" together, each blocked on "Emotion/Relationships (Week 3) being real first." That
precondition shipped in Week 3 (`EmotionalState`, `Relationship` with Trust/Affection/
SocialDistance). Of the four, Communication is the most tractable next step: TG-500 itself
names Trust, Relationship, and Reputation as Credibility inputs, and a `Relationship` graph to
propagate information across already exists. Language/Education/Law & Justice all list
Communication as a dependency in their own "Relationships" sections - Communication should
come first.

## Scope decision: Knowledge Diffusion of discovery-class events only

TG-500 names ~10 event types (ConversationStarted, RumorCreated, StoryTold, NegotiationFailed,
etc.) and several transformation modes (simplification, distortion, exaggeration, omission).
Building all of it in one increment would repeat the mistake RFC-001 explicitly avoided.
First increment covers exactly one thing: **whether a citizen has heard about a real event
that already happened**, propagated through the existing `Relationship` graph. No message
content, no distortion/fidelity loss, no networks-of-institutions modeling - just a
boolean "have they heard" per (citizen, event) pair, which is still a real, observable,
testable piece of TG-500's Core Principle ("Communication transforms isolated intelligence
into collective intelligence").

| In scope | Deferred (needs its own RFC later) |
|---|---|
| Knowledge Diffusion of `TechnologyDiscovered`, `KingdomFounded`, `ReligionEstablished` (the same civilization milestones `HistorySystem` already archives as High importance - see Day 1/Week 1) | Information Fidelity (simplification/distortion/exaggeration/omission) - "having heard" vs. "hearing an altered version" is a materially different, much harder problem |
| Propagation via the existing `Relationship` graph (Week 3) - closer `SocialDistance` spreads faster | Communication Networks as first-class entities (Families/Guilds/Trade routes as propagation channels, not just citizen-to-citizen) |
| Credibility gate using `Relationship.Trust` and `EmotionalState.Trust` (Week 3) | Full Credibility model (past accuracy, expertise, evidence - none of which exist as trackable state anywhere yet) |
| A citizen's set of "known events" exposed read-only via the Observatory | Rumors, propaganda, false testimony, and all of TG-500's "Edge Cases" section - these require content divergence, which this increment deliberately excludes |

## Data model

```csharp
// Garden.World/Entities/Citizen.cs - a new field, same pattern as Memories.
public List<string> KnownEventIds { get; init; } = []; // HistoricalRecord.Id.ToString() values
```

Chosen as a flat list of known record IDs (not a richer "belief" object) because this
increment tracks *whether* knowledge spread, not *what shape* it took once it arrived -
consistent with deferring Information Fidelity.

## Propagation model

A new `CommunicationSystem` (`Garden.Engine/Systems/`), `IScheduledSystem`, running on a daily
cadence (`IntervalTicks = 24`, matching `HistorySystem`'s own cadence for consistency):

1. Subscribe to the same civilization milestone events `HistorySystem` already archives
   (`TechnologyDiscoveredEvent`, `KingdomFoundedEvent`, `ReligionEstablishedEvent`). On each,
   mark the *discoverer/founder* citizen as already knowing it (`KnownEventIds.Add(...)`).
2. Each `Execute()`, for every citizen who already knows a "diffusible" event, check their
   `Relationship`s (Week 3): for each relationship where `SocialDistance < 40` (an invented
   threshold - "close" per `RelationshipSystem`'s own default of 50 for a first interaction),
   roll a spread chance proportional to `(100 - SocialDistance) / 100` and gated by
   `min(Trust, EmotionalState.Trust) > 30` (a credulous-enough listener). On success, the
   other citizen learns the event too and becomes a new potential spreader next cycle.
3. This produces an organic, Relationship-graph-shaped diffusion curve without needing a
   separate "network" data structure - the `Relationship` list already *is* the network TG-500
   asks for, at least for citizen-to-citizen spread.

## Explicitly out of scope for the next cycle

- Language differences reducing spread (TG-510 Language has its own zero-footprint gap and
  needs its own RFC first).
- Any UI beyond a read-only "What they know" list in the citizen detail panel (matching Day
  8/9/16's practice of always surfacing new domain concepts, kept minimal here since the
  concept itself is the priority, not its presentation).
- Settlement-to-settlement diffusion via trade routes/diplomacy (TradeRouteEstablished,
  DiplomaticRelationChanged) - a natural Phase 2 once citizen-to-citizen diffusion is proven,
  since `TradeCompletedEvent`'s dormancy (see Day 18 Change Log entry) means the most obvious
  settlement-level channel doesn't actually fire yet either.

## Open questions for review before implementation starts

1. Should "already knows" citizens (settlement founders, tech discoverers) re-share on every
   `CommunicationSystem` tick indefinitely, or only for a limited window after learning?
   (Recommendation: indefinitely for this increment - simpler, and TG-500 explicitly says
   "repeated communication reinforces shared understanding," so a citizen who's known
   something for years still being a valid source is consistent with the spec, not a bug.)
2. Is `Relationship.SocialDistance < 40` / `Trust > 30` the right threshold, or should it
   reuse `DiplomacyService`'s existing score-band pattern (80/60/40/20) for consistency across
   the codebase? (Recommendation: reuse the 40/60/80 band shape loosely as done here, but
   don't literally import `DiplomacyService`'s bands - those are settlement-level, this is
   citizen-level, and RFC-001 already established that per-system invented thresholds are
   expected and acceptable given TG-### specs never provide numbers.)

## Implementation notes (Week 5, added at close-out)

- **`KnownEventIds` stores a synthetic key, not `HistoricalRecord.Id`.** `HistorySystem`
  never exposes the archive record it generates back to other subscribers of the same
  domain event, and matching by search after the fact would be fragile (subscription order
  between `HistorySystem` and `CommunicationSystem` isn't guaranteed). Keys are
  `"Technology:{TechnologyId}"`, `"Kingdom:{KingdomId}"`, `"Religion:{ReligionId}"`.
  `CommunicationSystem.EventTitles` (an in-memory dictionary populated alongside each key)
  gives the Observatory a human-readable title without that coupling.
- **`TechnologyDiscoveredEvent` and `ReligionEstablishedEvent` didn't carry a citizen ID at
  all** before this increment, even though `TechnologyService`/`ReligionService` already
  computed a discoverer/founder citizen internally (`tech.DiscoveredByCitizenId`,
  `ReligionService`'s local `founder` variable) - just never put it on the event. Added
  `DiscoveredByCitizenId` and `FounderCitizenId` to the two events respectively, populated
  from the values those services already compute.
- **A single successful "conversation" transfers every event the spreader knows that the
  listener doesn't**, rather than rolling a separate chance per event. Simpler, and the
  SocialDistance/Trust-gated roll to reach that point already shapes the diffusion curve.
- **Live verification could not exercise organic milestone events within the session's time
  budget.** Kingdom/Religion formation are chance-gated (`ReligionService.EvaluateReligion`:
  `CulturalTraits.Count * 0.005` chance/year); Technology discovery is currently unreachable
  in practice due to an unrelated pre-existing bug (see Backlog: `TechnologyService`
  progress-scaling). Verified instead via `CommunicationSystemTests.cs`, publishing the
  domain events directly and asserting both the positive path (marking + diffusion) and the
  negative paths (distance/trust gates, dead citizens) - the same approach
  `EmotionSystemTests.cs` used for RFC-001's rare/hard-to-trigger emotion cases.
