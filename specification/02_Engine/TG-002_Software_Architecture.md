# The Garden Design Bible

# Document 02 — Software Architecture

**Document ID:** TG-002

**Version:** 0.1

**Status:** Living Document

**Priority:** CRITICAL

**Prerequisites**

* TG-000 Vision
* TG-001 Constitution

---

# Purpose

This document defines the technical architecture of The Garden.

Unlike most games, The Garden is not built around rendering graphics.

The primary product is the **simulation engine**.

Every other component exists only to observe, configure, or influence that simulation.

The architecture must support decades of growth without requiring major rewrites.

---

# High-Level Architecture

The Garden consists of six independent layers.

```
                    The Garden

                World Observatory
         (React / Desktop / Roblox)

                      │
                 HTTP / SignalR

                      │

                Garden API Layer

                      │

              Simulation Engine

                      │

            World State Repository

                      │

              PostgreSQL Database
```

Each layer has one responsibility.

No layer may bypass another.

---

# Architectural Philosophy

The Garden follows one simple rule:

> **The Simulation Engine is the source of truth.**

Nothing else may define reality.

Not the UI.

Not AI.

Not the database.

Not administrators.

Only the Simulation Engine.

---

# Layer 1 — World Observatory

Purpose:

Allow humans to observe the world.

Responsibilities:

* Display dashboards
* Display history
* Display stories
* Inspect citizens
* Display maps
* Visualize statistics
* Control simulation speed
* Send player requests

Non-responsibilities:

* Calculate weather
* Calculate births
* Decide politics
* Move citizens
* Create history

The Observatory is a window.

Not a brain.

---

# Layer 2 — Garden API

Purpose:

Expose the simulation safely.

Responsibilities:

* Authentication (future)
* Request validation
* REST endpoints
* SignalR broadcasting
* DTO conversion
* Rate limiting
* Save/load commands

The API never contains world logic.

---

# Layer 3 — Simulation Engine

This is the heart of The Garden.

Responsibilities:

* Advance time
* Execute rules
* Process citizens
* Process weather
* Process ecology
* Process economy
* Generate events
* Maintain consistency

Everything meaningful happens here.

If this layer disappeared, the world would stop existing.

---

# Layer 4 — World State Repository

Purpose:

Maintain the current state of the world.

Examples:

Current weather

Current citizens

Current villages

Current forests

Current food supplies

Current relationships

Current kingdoms

Current technologies

Think of this as "the present."

---

# Layer 5 — Historical Archive

Purpose:

Record every meaningful event.

Examples:

Birth

Death

Marriage

Migration

Harvest

Flood

Trade agreement

Discovery

Election

War

Nothing is deleted.

History is append-only.

The past is immutable.

---

# Layer 6 — Persistence

Purpose:

Store everything safely.

Technology:

PostgreSQL

Future considerations:

Backups

Snapshots

Compression

Long-running worlds

---

# Simulation Flow

Every simulation cycle follows the same sequence.

```
Tick Begins

↓

Advance Time

↓

Update Environment

↓

Update Resources

↓

Update Citizens

↓

Update Settlements

↓

Update Economy

↓

Resolve Events

↓

Generate History

↓

Publish Updates

↓

Tick Ends
```

Every tick follows this exact order.

The order must remain deterministic.

---

# Tick-Based Architecture

The Garden is a tick simulation.

Real time is merely one possible driver.

Example:

```
1 Tick

↓

1 Simulated Hour
```

Players may configure simulation speed.

Possible speeds:

Paused

1×

5×

10×

100×

1000×

The engine should behave identically regardless of speed.

Only the frequency of execution changes.

---

# The Event Pipeline

The Garden does not immediately mutate the world.

Instead, systems produce events.

Example:

```
Weather System

↓

Rain Started

↓

Crop System

↓

Harvest Increased

↓

Food Storage Increased

↓

Population Health Improved

↓

Birth Rate Increased

↓

History Recorded
```

This pipeline allows one event to influence many systems without creating tight coupling.

---

# Module Independence

Every simulation domain is isolated.

Examples:

Meteorology

Ecology

Hydrology

Agriculture

Demography

Economics

Politics

Culture

Religion

Language

These modules communicate only through world state and events.

No module should directly manipulate another module's internal logic.

---

# Folder Architecture

```
TheGarden/

backend/

frontend/

docs/

engine/

    Core/

    Time/

    Events/

    Weather/

    Ecology/

    Citizens/

    Economy/

    Politics/

    History/

simulation/

database/

shared/

tests/
```

Each engine module owns its own rules.

---

# Dependency Rules

Allowed:

```
Observatory

↓

API

↓

Engine

↓

Database
```

Forbidden:

```
Observatory

↓

Database
```

Forbidden:

```
Weather

↓

Citizen internals
```

Instead:

Weather creates events.

Citizens respond.

---

# Communication Model

Internal communication should be event-driven.

Examples:

RainStarted

HarvestCompleted

CitizenBorn

CitizenDied

VillageFounded

DiseaseSpread

BridgeBuilt

The Event Bus distributes these events to interested systems.

This minimizes coupling and encourages emergence.

---

# Story Engine

The Story Engine is **not** part of the simulation.

It never invents facts.

Instead, it transforms simulation history into human-readable narratives.

Example:

Simulation Event:

```
Citizen 284 died from starvation.
```

Story Output:

> "After a difficult winter, one of Pinewood's oldest residents passed away as food supplies ran dry."

The Story Engine is a translator.

Not an author.

---

# AI Integration

Artificial Intelligence is optional.

Simulation is mandatory.

If AI is unavailable:

The world continues.

If AI is offline:

The world continues.

If AI produces poor output:

The simulation remains correct.

AI must never become a dependency of the simulation.

---

# Scalability Principles

Every subsystem should be replaceable.

Examples:

Replace PostgreSQL.

Replace React.

Replace ASP.NET.

Replace Story Engine.

The Simulation Engine should remain largely unaffected.

The simulation is the product.

Everything else is replaceable.

---

# Engineering Goals

The architecture should prioritize:

* Determinism
* Modularity
* Explainability
* Testability
* Long-term maintainability
* Extensibility
* Performance

Above all else:

Consistency.

---

# Closing Statement

The Garden is not a web application.

It is not a game with a backend.

It is a simulation platform.

The frontend observes it.

The API exposes it.

The database preserves it.

Only the Simulation Engine creates reality.

Every future feature must strengthen this separation rather than weaken it.
