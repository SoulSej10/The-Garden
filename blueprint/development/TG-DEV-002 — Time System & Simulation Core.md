# TG-DEV-002 — Time System & Simulation Core

**Document Version:** 1.0 (Living Document)
**Status:** Active
**Prerequisites:** TG-DEV-001, TG-004, TG-006, TG-007
**Estimated Duration:** Week 3–4

---

# Purpose

This phase implements the heart of The Garden.

No simulation discipline (Weather, Ecology, Citizens, Politics, etc.) may be developed until this phase is complete.

The objective is to build a deterministic simulation engine capable of advancing time, scheduling systems, processing rules, publishing events, and committing validated world mutations.

By the end of this phase, The Garden should be capable of advancing through time even if the world is completely empty.

---

# Phase Objectives

## Primary Goal

Create the deterministic simulation framework that every future discipline will use.

## Success Criteria

✓ Simulation Clock operational

✓ Tick Engine operational

✓ Scheduler operational

✓ Rule Engine operational

✓ Event Bus operational

✓ Mutation Pipeline operational

✓ Simulation Coordinator operational

✓ Engine can pause/resume

✓ Engine supports configurable simulation speed

✓ Engine is deterministic

---

# Development Rules

This phase must follow TG-004, TG-006 and TG-007 without deviation.

Additional rules:

* No rule may directly modify the world.
* Every world change must pass through the mutation pipeline.
* Every mutation must be validated before commit.
* Every event must be immutable.
* The engine is the only component allowed to advance simulation time.

---

# Feature Branches

```text
feature/time-system

feature/tick-engine

feature/scheduler

feature/rule-engine

feature/event-bus

feature/world-mutations

feature/simulation-loop
```

Merge each feature branch into **develop** only after successful compilation.

---

# Project Responsibilities

Garden.Core

Responsible for

* Time abstractions
* Shared interfaces
* IDs
* Immutable value objects

---

Garden.Engine

Responsible for

* Tick Engine
* Scheduler
* Rule Runner
* Simulation Coordinator
* Event Dispatcher
* Mutation Commit Pipeline

---

Garden.World

Responsible for

* World State
* Entities
* Collections

No behavior should exist here.

---

Garden.Contracts

Responsible for

* Engine contracts
* DTOs
* Event contracts

---

Garden.Shared

Responsible for

* Utility classes
* Math
* Random generation
* Collections

---

# Task 1 — Time System

Implement

```text
SimulationClock

SimulationTime

Tick

Day

Week

Month

Season

Year

Era
```

Requirements

* Immutable time objects
* Configurable start date
* Tick advances exactly one simulation hour
* Calendar conversion utilities
* Leap year support (future-ready)

Deliverables

✓ Simulation time object

✓ Time conversion utilities

✓ Calendar system

---

# Task 2 — Tick Engine

Implement

```text
TickEngine
```

Responsibilities

* Advance time
* Execute simulation pipeline
* Maintain tick count
* Publish tick events
* Notify scheduler

Requirements

* Deterministic
* Single authority
* Thread-safe
* Restart-safe

---

# Task 3 — Simulation Speed Controller

Implement

Supported speeds

```text
Paused

1×

2×

5×

10×

25×

50×

100×

250×

500×

1000×
```

Requirements

* Runtime switching
* No skipped ticks
* Stable execution
* UI-ready API

---

# Task 4 — Scheduler

Implement

```text
SimulationScheduler
```

Responsibilities

Execute systems at different intervals.

Example

Hourly

* Weather

Daily

* Agriculture

Weekly

* Economy

Monthly

* Politics

Yearly

* Culture

Requirements

* Configurable intervals
* Multiple schedules
* Efficient lookup
* No duplicated execution

---

# Task 5 — Rule Engine

Implement

```text
Rule

RuleContext

RuleRunner

RuleRegistry
```

Every rule follows

Observe

↓

Evaluate

↓

Propose

↓

Commit

Rules never modify world state.

Rules only produce proposals.

---

# Task 6 — Mutation Pipeline

Implement

```text
WorldMutation

MutationValidator

MutationProcessor

MutationCommitter
```

Pipeline

Proposal

↓

Validation

↓

Approval

↓

Commit

↓

Publish Events

Rejected mutations must include reason.

---

# Task 7 — Event Bus

Implement

```text
EventBus

DomainEvent

EventPublisher

EventSubscriber
```

Requirements

Events

* immutable
* timestamped
* uniquely identified
* lightweight

Subscribers

* loosely coupled
* independent
* ordered execution

No subscriber should know another subscriber exists.

---

# Task 8 — Simulation Coordinator

Implement

```text
SimulationCoordinator
```

Responsibilities

Coordinates the entire simulation cycle.

Pipeline

```text
Advance Time

↓

Scheduler

↓

Execute Rules

↓

Validate

↓

Commit

↓

Publish Events

↓

Store Statistics

↓

Finish Tick
```

Only one coordinator may exist.

---

# Task 9 — World State

Implement

```text
WorldState
```

Responsibilities

Contains

* Current simulation time
* Active entities
* Resources
* Environment
* Settlements
* Citizens

History is NOT stored here.

Only the present.

---

# Task 10 — Deterministic Randomness

Implement

```text
SimulationRandom
```

Requirements

Seed-based

Repeatable

Thread-safe

Every random decision must originate from this service.

Never instantiate Random directly.

---

# Task 11 — Hosted Simulation Service

Create ASP.NET Hosted Service.

Responsibilities

* Start engine
* Pause engine
* Resume engine
* Stop engine
* Advance ticks

Simulation must continue without any connected clients.

---

# API Endpoints

Create

```text
GET /simulation/status

GET /simulation/time

POST /simulation/start

POST /simulation/pause

POST /simulation/resume

POST /simulation/step

POST /simulation/speed
```

No simulation editing endpoints.

The Observatory may observe, never modify reality directly.

---

# Observatory Tasks

Create Simulation Control panel.

Display

Simulation Status

Current Tick

Current Day

Season

Year

Simulation Speed

Current Tick Duration

Buttons

* Start
* Pause
* Resume
* Single Step

Disable invalid actions.

Example

Paused

↓

Pause button disabled

Resume enabled

---

# Dashboard Layout

Top Section

Simulation Status Card

Simulation Clock Card

Engine Status Card

Speed Controller Card

Middle Section

Recent Engine Events

Scheduler Activity

Bottom Section

Engine Statistics

No graphs yet.

Simple metrics only.

---

# Logging

Every simulation cycle should log

Tick Number

Simulation Time

Execution Duration

Executed Systems

Published Events

Rejected Mutations

Errors

Support Debug mode.

---

# Performance Targets

Simulation Tick

Target

< 20 ms

Idle Memory

Stable

No memory leaks

No duplicate events

No duplicate mutations

---

# Definition of Completion

Backend

✓ Time system complete

✓ Tick engine complete

✓ Scheduler complete

✓ Rule engine complete

✓ Mutation pipeline complete

✓ Event bus complete

✓ Hosted service operational

✓ API endpoints complete

---

Frontend

✓ Simulation controls operational

✓ Engine status visible

✓ Simulation clock updates

✓ Dashboard reflects engine state

---

Infrastructure

✓ All builds passing

✓ CI successful

✓ Repository organized

✓ Documentation updated

---

# Bi-Weekly Progress Report (End of Week 4)

OpenCode shall submit:

## Completed Features

* Time System
* Tick Engine
* Scheduler
* Event Bus
* Rule Engine
* Mutation Pipeline
* Observatory controls

## Performance Summary

* Average tick duration
* Startup time
* Memory usage
* API response time

## Technical Decisions

* Architectural adjustments
* Refactoring performed
* Interfaces introduced

## Remaining Work

List outstanding issues that must be resolved before beginning environmental simulation.

## Ready for Next Phase

Confirm readiness to begin **TG-DEV-003 — World & Environment**.

The engine must be capable of running indefinitely with an empty world before proceeding.
