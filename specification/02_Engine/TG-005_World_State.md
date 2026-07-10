# The Garden Design Bible

# Document TG-005 — World State & Entity Model

**Document ID:** TG-005

**Version:** 0.1

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Scientific Discipline:** Ontology

**Depends On**

* TG-000 Vision
* TG-001 Constitution
* TG-002 Software Architecture
* TG-003 Project Structure
* TG-004 Chronology

---

# Purpose

This document defines what exists inside The Garden.

Before citizens can think...

Before weather can change...

Before villages can grow...

The simulation must first define **what reality is made of.**

This document establishes the canonical model of the world.

---

# Philosophy

The Garden contains exactly one reality.

That reality is called the **World State**.

Every module observes it.

Every module modifies it through approved simulation rules.

No module owns reality.

Reality belongs only to the World.

---

# Definition

The World State is the complete snapshot of existence at one specific Tick.

If the simulation pauses, the World State completely describes the current universe.

It answers questions like:

* Who exists?
* Where are they?
* What is the weather?
* Which villages remain?
* Which rivers flow?
* Which resources exist?
* Which kingdoms rule?
* Which technologies have been discovered?

The World State represents **the present**.

History represents **the past**.

---

# World State vs History

These two concepts must never be confused.

## World State

Mutable.

Represents now.

Examples:

Current weather

Current population

Current food

Current forest size

Current ruler

---

## History

Immutable.

Represents everything that has already happened.

Examples:

Village founded.

Citizen born.

Flood occurred.

War declared.

King crowned.

History is append-only.

The World State changes.

History grows.

---

# Canonical Reality

There is exactly one canonical World State.

The following are forbidden:

Multiple conflicting world states.

UI-owned truth.

Cached simulation truth.

Temporary parallel realities.

The engine owns the only source of truth.

---

# What Is an Entity?

An Entity is anything that exists independently within the simulation.

Examples:

Citizen

Tree

Animal

Village

River

Bridge

Road

Mountain

Kingdom

Storm

Each entity has:

Identity.

Properties.

Lifecycle.

Relationships.

---

# Entity Identity

Every entity receives a globally unique identifier.

The identifier never changes.

Even if:

The citizen dies.

The village is abandoned.

The kingdom collapses.

Its identity remains valid for historical reference.

History depends on stable identities.

---

# Entity Lifecycle

Every entity follows four stages.

```text
Creation

↓

Existence

↓

Transformation

↓

Removal
```

Removal never erases history.

Removal simply means the entity no longer exists in the current World State.

---

# Living vs Non-Living Entities

Entities fall into broad categories.

## Living

Citizens

Animals

Plants (future)

---

## Constructed

Buildings

Roads

Bridges

Ships

---

## Geographic

Mountains

Forests

Rivers

Lakes

Regions

---

## Social

Villages

Kingdoms

Cultures

Religions

Markets

Guilds

---

## Environmental

Storms

Droughts

Wildfires

Floods

Seasons

Some entities are temporary.

Others persist for centuries.

---

# Relationships

Entities rarely exist in isolation.

Relationships define interaction.

Citizen

↓

Lives In

↓

Village

Village

↓

Belongs To

↓

Kingdom

Citizen

↓

Parent Of

↓

Citizen

River

↓

Flows Through

↓

Region

Relationships are first-class simulation concepts.

---

# Ownership

Ownership is contextual.

Examples:

Citizen owns:

Inventory

Home

Tools

Village owns:

Food storage

Infrastructure

Kingdom owns:

Territory

Policies

Ownership may change over time.

History records these changes.

---

# Spatial Existence

Every entity exists somewhere.

Future implementations may represent location as:

Coordinates

Region IDs

Navigation graphs

Tile maps

Chunks

The architecture intentionally avoids enforcing a specific spatial model.

Simulation disciplines may evolve independently.

---

# World Composition

The World is composed of independent collections.

Examples:

Citizens

Animals

Plants

Villages

Roads

Resources

Weather systems

Kingdoms

Cultures

Each collection evolves independently.

Together they form reality.

---

# Entity State

Every entity owns its own state.

Example:

Citizen

Age

Health

Hunger

Energy

Occupation

Location

Relationships

Inventory

Memories

Current activity

Example:

Village

Population

Food

Buildings

Economy

Influence

Culture

Current projects

State represents the present only.

History records everything else.

---

# Immutable Identity

Properties may change.

Identity may not.

Citizen #284 remains Citizen #284 forever.

Even after death.

Even after thousands of simulated years.

---

# World Snapshots

At any Tick, the engine should be capable of producing a complete snapshot.

Snapshots allow:

Saving.

Loading.

Debugging.

Replay.

Historical comparison.

Snapshots represent current reality only.

They are not replacements for History.

---

# Serialization

The World State must be serializable.

This enables:

Save files.

Cloud synchronization.

Version migration.

Testing.

Replay.

Deterministic debugging.

Serialization should preserve canonical reality exactly.

---

# Consistency Rules

Every Tick must end in a valid World State.

Forbidden examples:

Negative population.

Citizen living in two villages.

Bridge connected to nonexistent road.

Parent younger than child.

Consistency is validated before the Tick completes.

---

# World Queries

Simulation modules should ask questions such as:

Which citizens live here?

Which villages have no food?

Which forests border rivers?

Which kingdoms share borders?

The World provides information.

Modules decide behavior.

---

# Future Expandability

Future entity types should require minimal architectural changes.

Possible additions:

Ocean

Island

Volcano

Disease

Language

Currency

Constellation

Planet

Species

Adding an entity should not require redesigning the engine.

---

# The Canonical Principle

The World State is not a database.

It is not a cache.

It is not a DTO.

It is the living universe itself.

The database stores it.

The API exposes it.

The Observatory visualizes it.

Only the Simulation Engine changes it.

---

# Closing Statement

Every citizen.

Every tree.

Every kingdom.

Every storm.

Every road.

Every memory.

Exists because it occupies a place within the World State.

History remembers where it has been.

Chronology determines when it changes.

Simulation determines why it changes.

Together, these systems transform data into a living world.
