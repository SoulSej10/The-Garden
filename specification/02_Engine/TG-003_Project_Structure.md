# The Garden Design Bible

# Document TG-003 — Project Structure & Clean Architecture

**Document ID:** TG-003

**Version:** 0.1

**Status:** Living Document

**Priority:** CRITICAL

**Depends On**

* TG-000 Vision
* TG-001 Constitution
* TG-002 Software Architecture

---

# Purpose

This document defines the physical and logical structure of The Garden.

It specifies how the codebase is organized, how projects interact, dependency rules, naming conventions, and architectural boundaries.

The objective is to ensure the project remains maintainable for many years while allowing independent evolution of simulation systems.

This document is considered mandatory reading before contributing code.

---

# Architecture Philosophy

The Garden follows four architectural principles.

## Principle 1

The Simulation Engine is independent.

It should not know:

* React
* ASP.NET
* SignalR
* PostgreSQL
* Roblox

It only knows:

* Rules
* Time
* Entities
* Events

---

## Principle 2

Everything communicates through contracts.

Modules should depend on interfaces instead of concrete implementations whenever practical.

---

## Principle 3

The engine owns business logic.

Nothing outside the engine may modify world state directly.

---

## Principle 4

Every simulation discipline is isolated.

Meteorology does not know Agriculture's implementation.

Agriculture does not know Politics' implementation.

Communication happens through shared world state and events.

---

# Repository Structure

```
TheGarden/

docs/

src/

tests/

tools/

docker/

assets/

research/

scripts/
```

---

# Source Layout

```
src/

Garden.Api/

Garden.Engine/

Garden.Core/

Garden.World/

Garden.Infrastructure/

Garden.Contracts/

Garden.Shared/

Garden.Story/

Garden.Simulation/

Garden.Tools/

Garden.Observatory/
```

---

# Responsibilities

## Garden.Api

Technology:

ASP.NET Core

Responsibilities:

* REST API
* SignalR
* DTO mapping
* Authentication
* Save/Load
* Simulation controls

It does NOT contain simulation logic.

---

## Garden.Engine

The beating heart.

Contains:

* Tick Engine
* Scheduler
* Event Bus
* Rule Runner
* Module Pipeline
* Simulation Coordinator

Every tick begins here.

---

## Garden.Core

Contains universal concepts.

Examples:

Time

Events

Interfaces

Value Objects

IDs

Utilities

Shared abstractions

No module-specific logic.

---

## Garden.World

Contains world state.

Examples:

Citizen

Village

Kingdom

Forest

Animal

River

Weather

Biome

Road

Mountain

Everything representing the world's current state belongs here.

---

## Garden.Infrastructure

Responsible for external systems.

Examples:

Entity Framework

PostgreSQL

Logging

Configuration

Caching

File storage

Snapshots

Backups

Infrastructure depends on the Engine—not the other way around.

---

## Garden.Contracts

Contains:

DTOs

API Contracts

Events

Shared interfaces

Messages

Command definitions

Nothing executable.

---

## Garden.Shared

Pure helper library.

Extensions

Math

Geometry

Random utilities

Algorithms

Collections

No business logic.

---

## Garden.Story

Transforms history into narrative.

Consumes:

History

Events

Relationships

Produces:

Stories

Daily summaries

Historical reports

News

Chronicles

Never changes simulation data.

---

## Garden.Simulation

Home for every simulation discipline.

This project contains multiple modules.

---

### Meteorology

Weather

Seasons

Climate

Humidity

Storms

---

### Ecology

Plants

Trees

Animals

Growth

Decay

---

### Hydrology

Rivers

Floods

Lakes

Water cycles

---

### Agriculture

Food

Harvest

Farming

Storage

---

### Sociology

Families

Friendships

Marriage

Communities

Conflict

---

### Demography

Births

Deaths

Population

Migration

---

### Economics

Production

Trade

Resources

Markets

---

### Politics

Leadership

Kingdoms

Diplomacy

Wars

Laws

---

### Culture

Traditions

Religion

Language

Architecture

Education

Technology

Each discipline lives independently.

---

## Garden.Tools

Internal developer utilities.

World generators

Seed generators

Debug visualizers

Replay tools

Benchmark tools

Migration helpers

Not shipped to players.

---

## Garden.Observatory

Technology:

React + Vite

Purpose:

Observe.

Never simulate.

Features:

Dashboard

Citizen Inspector

Timeline

Charts

Story Feed

Live Log

Map

Analytics

Simulation Controls

---

# Tests

```
tests/

Garden.UnitTests/

Garden.IntegrationTests/

Garden.SimulationTests/

Garden.PerformanceTests/

Garden.Playground/
```

---

## Unit Tests

Every simulation rule.

---

## Integration Tests

Module interactions.

---

## Simulation Tests

Long-running simulations.

Example:

Run 100 simulated years.

Verify:

Population remains stable.

Resources remain believable.

History continues recording.

---

## Performance Tests

Stress tests.

1 million citizens.

100 kingdoms.

500 years.

Memory usage.

Tick duration.

---

## Playground

A safe place for experimentation.

Prototype ideas.

Destroy them.

Keep the engine clean.

---

# Dependency Diagram

```
Observatory

↓

API

↓

Engine

↓

Simulation Modules

↓

World State

↓

Infrastructure
```

Allowed:

Downward.

Forbidden:

Upward.

No circular dependencies.

---

# Module Ownership

Each module owns its own rules.

Example:

Meteorology owns:

Rain.

Temperature.

Wind.

Storms.

Agriculture responds to weather.

Meteorology never grows crops.

---

# Internal Event Bus

Every module communicates through events.

Examples:

RainStarted

CitizenBorn

VillageFounded

AnimalMigrated

BridgeCollapsed

CropFailed

MarketOpened

No module calls another module directly unless absolutely necessary.

---

# Naming Conventions

Projects:

Garden.*

Namespaces:

Garden.*

Interfaces:

I*

Events:

Past tense.

Examples:

CitizenBorn

HarvestCompleted

StormEnded

Commands:

Imperative.

AdvanceSimulation

CreateSnapshot

PauseSimulation

Entities:

Singular.

Citizen

Village

Tree

Animal

Repositories:

<Entity>Repository

Services:

<Entity>Service

Rule Processors:

<Domain>Processor

Examples:

WeatherProcessor

CitizenProcessor

TradeProcessor

---

# Coding Standards

Avoid static state.

Prefer dependency injection.

Keep methods small.

Prefer immutable value objects.

Prefer composition over inheritance.

Every public method should have one clear responsibility.

Simulation rules should be deterministic whenever possible.

---

# Future Expandability

This structure must support:

Desktop viewer.

Roblox integration.

Dedicated server.

Cloud hosting.

Mobile companion app.

Replay viewer.

AI storyteller.

Without restructuring the engine.

---

# Architectural Goal

After ten years of development:

The project structure should still make sense.

Adding a new discipline (Astronomy, Genetics, Oceanography) should feel like adding another independent scientific field—not modifying existing systems.

---

# Closing Statement

The Garden is designed as a platform rather than a single application.

Its architecture mirrors the structure of the real world:

Independent disciplines.

Shared reality.

Common history.

Continuous interaction.

Every directory exists to preserve that philosophy.
