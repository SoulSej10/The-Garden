# The Garden Specification

# Volume III — Natural Sciences

# TG-100 — Planetary Model & Physical Laws

**Document ID:** TG-100

**Volume:** III — Natural Sciences

**Scientific Discipline:** Planetary Science

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Depends On**

* TG-004 Chronology
* TG-005 World State
* TG-006 Rule Engine
* TG-007 Causality Engine

---

# Purpose

Before weather can exist...

Before rivers can flow...

Before forests can grow...

There must first be a physical world.

The purpose of the Planetary Model is to define the immutable characteristics of that world.

This document establishes the physical framework upon which every other natural and human system depends.

---

# Scientific Basis

The Garden is not intended to be an astrophysics simulator.

Instead, it models a believable terrestrial world with simplified physical laws.

The emphasis is on consistency, emergent behavior, and long-term simulation rather than scientific precision.

The world should feel natural without requiring real-world complexity.

---

# Design Philosophy

The physical world changes slowly.

Living organisms change quickly.

Civilizations change even faster.

The terrain should outlive kingdoms.

Mountains should outlive civilizations.

Continents should outlive history itself.

The physical world provides stability against which all other change is measured.

---

# Core Concepts

The world is finite.

Every location belongs to exactly one world.

Every simulation occurs within this shared physical space.

The world is continuous across time.

Terrain is persistent.

Geography changes only through major geological events.

---

# Planet Structure

The initial implementation contains a single planet.

Future versions may support multiple worlds, but the simulation architecture assumes one canonical world.

---

# Coordinate System

Every location has spatial coordinates.

The coordinate system must remain consistent across all sciences.

Possible future representations include:

* Tile grids
* Continuous coordinates
* Chunk-based worlds

The implementation is abstracted from the simulation rules.

---

# Regions

The world is divided into logical regions.

Regions provide:

* Administrative grouping
* Climate boundaries
* Resource distribution
* Ecological variation
* Performance optimization

Regions are not political.

Political borders are defined by civilization, not geography.

---

# Continents

Continents are permanent landmasses.

Properties include:

* Area
* Elevation profile
* Climate zones
* Resource richness
* Biome distribution

Continents naturally constrain migration, trade, and expansion.

---

# Oceans

Oceans are continuous bodies of water separating landmasses.

Oceans influence:

* Climate
* Rainfall
* Trade routes
* Exploration
* Resource availability

Future systems may include currents and tides.

---

# Elevation

Elevation is a foundational physical property.

It influences:

* Temperature
* Water flow
* River generation
* Soil quality
* Travel difficulty
* Settlement suitability

Elevation changes rarely.

---

# Terrain

Every location possesses one terrain classification.

Examples:

Ocean

Beach

Plain

Grassland

Forest

Hill

Mountain

Desert

Swamp

Tundra

Terrain defines environmental constraints but does not determine ownership or civilization.

---

# Physical Resources

Resources originate from the planet.

Examples:

Stone

Iron

Copper

Coal

Gold

Fresh water

Clay

Fertile soil

Forests

Wildlife

Resources may regenerate, deplete, or remain effectively permanent depending on type.

---

# Natural Boundaries

Natural features influence movement.

Examples:

Mountain ranges

Large rivers

Dense forests

Oceans

Marshes

These barriers shape migration and civilization organically.

---

# State Variables

The Planetary Model owns:

* World dimensions
* Coordinate system
* Elevation
* Terrain type
* Region membership
* Resource deposits
* Static geographical features

These variables change infrequently compared to biological or social systems.

---

# Lifecycle

The planet follows a simple lifecycle.

```text
Generation

↓

Initialization

↓

Simulation

↓

Rare Geological Change

↓

Persistence
```

The world is generated once and then evolves slowly over time.

---

# Simulation Rules

The Planetary Model does not actively drive the simulation every Tick.

Instead, it provides environmental context.

Rules include:

* Terrain remains stable.
* Elevation changes only through geological events.
* Resources follow regeneration or depletion rules.
* Regions remain geographically fixed.
* Coordinates are immutable.

---

# Events

Possible Events include:

WorldGenerated

EarthquakeOccurred

VolcanoErupted

LandslideOccurred

NewIslandFormed

RiverCourseChanged

Most geological events are intentionally rare.

---

# Relationships

The Planetary Model influences every other science.

Meteorology depends on elevation.

Hydrology depends on terrain.

Ecology depends on climate.

Civilizations depend on resources.

Trade depends on geography.

No discipline may redefine physical geography.

---

# Edge Cases

The system should gracefully handle:

Isolated islands.

Landlocked regions.

Resource-poor continents.

Mountain-encircled valleys.

Extremely fertile regions.

Harsh environments.

These variations create unique historical outcomes.

---

# Performance Considerations

Physical geography is largely static.

Calculations should therefore be cached where appropriate.

Frequent recalculation of terrain relationships should be avoided unless geological events occur.

---

# Future Extensions

Future planetary systems may introduce:

Plate tectonics.

Ocean currents.

Volcanic belts.

Glaciers.

Caves.

Underground resources.

Dynamic coastlines.

Multiple planets.

These extensions should build upon—not replace—the core planetary model.

---

# Closing Statement

The Planetary Model is the oldest system in The Garden.

Before life...

Before weather...

Before civilization...

There was the land.

Every forest grows because the terrain allows it.

Every river flows because elevation guides it.

Every kingdom rises because the planet provides a place to stand.

The world is the stage upon which all history unfolds.
