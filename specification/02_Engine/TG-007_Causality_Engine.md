# The Garden Design Bible

# Document TG-007 — Causality Engine (Event System)

**Document ID:** TG-007

**Version:** 0.1

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Scientific Discipline:** Causality

**Depends On**

* TG-000 Vision
* TG-001 Constitution
* TG-002 Software Architecture
* TG-003 Project Structure
* TG-004 Chronology
* TG-005 World State
* TG-006 Rule Engine

---

# Purpose

The Causality Engine defines how change propagates through The Garden.

The simulation is built upon one fundamental principle:

> Nothing happens without a cause.

Every meaningful change produces one or more Events.

Every Event becomes an opportunity for other disciplines to react.

This creates chains of consequences instead of isolated mechanics.

The Causality Engine is therefore responsible for connecting every scientific discipline into one coherent ecosystem.

---

# Philosophy

Reality is not updated.

Reality unfolds.

A storm does not tell crops to grow.

The storm simply exists.

Rain falls.

Soil becomes wet.

Roots absorb water.

Plants respond.

Animals migrate.

Harvests improve.

Villages prosper.

Each consequence is connected by causality rather than direct control.

The Causality Engine exists to preserve this philosophy.

---

# What Is an Event?

An Event represents a fact that has occurred during the simulation.

Events describe completed changes.

Examples:

CitizenBorn

CitizenDied

RainStarted

RainEnded

HarvestCompleted

VillageFounded

BridgeCollapsed

FireStarted

FoodSpoiled

LeaderElected

Events never describe intentions.

Events describe reality.

---

# Event Lifecycle

Every Event follows the same lifecycle.

```text
Rule Executes

↓

Event Created

↓

Event Published

↓

Subscribers React

↓

New Events Generated

↓

History Recorded

↓

Event Retired
```

An Event exists only for the duration of its propagation.

History preserves what happened after propagation is complete.

---

# Event Immutability

Events are immutable.

Once created, an Event cannot be modified.

If information changes, a new Event is generated.

This guarantees consistency, replayability, and reliable debugging.

---

# Event Ownership

Rules create Events.

The Causality Engine distributes Events.

Modules consume Events.

History records Events.

No discipline owns the Event after publication.

---

# Event Propagation

One Event may influence many independent disciplines.

Example:

```text
RainStarted

↓

Hydrology
River level increases.

↓

Ecology
Soil moisture improves.

↓

Agriculture
Crop growth accelerates.

↓

Citizens
Outdoor work slows.

↓

Road Network
Mud increases travel time.

↓

History
Rainfall recorded.
```

None of these systems communicate directly.

They simply react to the same Event.

---

# Event Categories

Events are grouped by domain.

## Environmental

RainStarted

StormEnded

RiverFlooded

WildfireSpread

SeasonChanged

---

## Biological

CitizenBorn

CitizenDied

AnimalMigrated

TreeMatured

DiseaseSpread

---

## Social

MarriageOccurred

FriendshipFormed

VillageFounded

FamilyExpanded

FestivalHeld

---

## Economic

TradeCompleted

MarketOpened

FoodStored

MineExhausted

ResourceDiscovered

---

## Political

ElectionHeld

TreatySigned

WarDeclared

BorderExpanded

LawPassed

---

## Historical

EraEnded

KingdomCollapsed

CivilizationDiscovered

WonderConstructed

---

# Event Ordering

Within a Tick, Events are processed deterministically.

Identical simulations must produce identical Event sequences.

Ordering rules should be explicit and documented.

The engine must never rely on unpredictable execution order.

---

# Cascading Events

Events may produce additional Events.

Example:

```text
LightningStruck

↓

TreeIgnited

↓

ForestFireStarted

↓

AnimalsMigrated

↓

VillageEvacuated

↓

FoodProductionDropped

↓

FamineRiskIncreased
```

This chain creates emergent stories without hardcoding them.

---

# Event Boundaries

Events should communicate facts.

They should not contain business logic.

Correct:

CitizenDied

Incorrect:

CitizenDiedAndReduceVillagePopulationAndNotifyEconomy

Events remain small, focused, and reusable.

---

# Event Subscribers

Every scientific discipline subscribes only to Events it cares about.

Meteorology ignores marriages.

Politics ignores soil moisture.

Agriculture ignores elections unless they affect farming.

Loose coupling keeps the simulation modular.

---

# Event Replay

Because Events are immutable and ordered, they can be replayed.

Replay enables:

Debugging.

Historical investigation.

Simulation verification.

Educational visualization.

The replay system should reproduce the same outcomes when paired with the correct World State and random seed.

---

# Event Logging

Development tools should expose:

Event name.

Tick.

Originating rule.

Subscribers notified.

Processing duration.

Generated child Events.

Rejected Events (if any).

This information is essential during development.

---

# Performance

The Causality Engine should minimize unnecessary work.

Only subscribed disciplines receive an Event.

Unused Events are discarded after propagation.

Events are lightweight.

History retains only the information necessary for long-term records.

---

# Architectural Principles

Every Event should answer three questions:

What happened?

When did it happen?

Who or what was involved?

Events should never answer:

What should happen next?

That decision belongs to subscribers.

---

# Relationship to History

An Event is temporary.

History is permanent.

Events move the simulation forward.

History remembers that it happened.

History never republishes Events.

Replay reconstructs them when necessary.

---

# Future Extensions

The Causality Engine should support:

Event prioritization.

Distributed simulation.

Remote observers.

Network synchronization.

Analytics pipelines.

AI Story Engine subscriptions.

Without altering the core propagation model.

---

# Closing Statement

The Garden is not driven by commands.

It is driven by consequences.

Every storm echoes through rivers.

Every harvest shapes villages.

Every death changes families.

Every discovery influences civilization.

The Causality Engine exists to ensure that every action leaves a trace, every consequence has a cause, and every story emerges from the natural unfolding of the world.
