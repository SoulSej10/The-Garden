# TG-DEV-010 — Engineering Standards & Definition of Done

**Document Version:** 1.0 (Living Document)
**Status:** Active
**Prerequisites:** Applies to TG-DEV-001 through TG-DEV-009

---

# Purpose

This document establishes the engineering standards that every contribution to **The Garden** must follow.

Unlike the Blueprint documents, which define **what** The Garden is, and the Development documents, which define **what should be built**, this document defines **how it should be built**.

Every feature, bug fix, refactor, optimization, and future contribution must comply with these standards.

This document acts as the project's engineering contract.

---

# Core Principle

The Garden is expected to live for many years.

Every line of code written today should make future development easier—not harder.

Optimize for:

* Readability
* Maintainability
* Simplicity
* Determinism
* Scalability

Never optimize for cleverness.

---

# Definition of Done

A task is **NOT** considered complete simply because it works.

Every completed task must satisfy all of the following.

## Functional

✓ Feature behaves according to the Blueprint.

✓ Feature satisfies the corresponding TG-DEV document.

✓ No known breaking issues.

---

## Compilation

✓ Backend builds successfully.

✓ Frontend builds successfully.

✓ No compilation warnings that should reasonably be resolved.

---

## Architecture

✓ Clean Architecture maintained.

✓ No circular dependencies.

✓ Layer responsibilities respected.

✓ Simulation logic remains inside the Engine.

✓ UI remains presentation only.

---

## Repository

✓ Changes committed using descriptive commit messages.

✓ Feature branch merged into **develop**.

✓ **main** remains stable.

---

## Documentation

If implementation changes expected behavior:

* Update Blueprint (if architectural)
* Update DEV document (if implementation sequence changes)
* Update README (if setup changes)

Never allow documentation to become outdated.

---

# Engineering Principles

Every implementation should follow these priorities.

1. Correctness
2. Determinism
3. Readability
4. Maintainability
5. Performance

Performance is important.

Correctness is mandatory.

---

# Naming Conventions

## C#

Classes

```text
Citizen

SettlementManager

SimulationCoordinator
```

Methods

```text
AdvanceTick()

GenerateWorld()

EvaluateNeeds()
```

Interfaces

```text
IScheduler

IEventBus

IRule
```

Private fields

```text
_currentTick

_logger

_worldState
```

---

## React

Components

```text
CitizenCard

HistoryTimeline

SimulationToolbar
```

Hooks

```text
useSimulation()

useCitizens()

useTimeline()
```

Pages

```text
DashboardPage

HistoryPage

CitizenPage
```

Avoid abbreviations unless universally understood.

---

# Folder Ownership

Every file belongs somewhere for a reason.

Never create folders simply because they "might be useful."

If uncertain, follow TG-003.

Avoid

```text
misc/

helpers2/

temp/

old/

new/

random/
```

---

# Code Quality

Prefer

Small classes.

Small methods.

Single responsibility.

Readable variable names.

Avoid

God classes.

500-line methods.

Nested logic.

Magic numbers.

Duplicated logic.

---

# Comments

Comments should explain

**why**

Not

**what**

Bad

```csharp
// Add one to x

x++;
```

Good

```csharp
// Citizens age once per simulation year.

citizen.Age++;
```

---

# Error Handling

Handle failures gracefully.

Never silently ignore exceptions.

Log meaningful errors.

Return actionable messages.

Avoid

```text
Something went wrong.
```

Prefer

```text
Failed to load World State because PostgreSQL connection was unavailable.
```

---

# Logging Standards

Information

Application startup.

Simulation started.

Simulation paused.

---

Warning

Slow queries.

Large queues.

Retry attempts.

---

Error

Unexpected failures.

Database errors.

Unhandled simulation exceptions.

---

Critical

Corrupted save.

Failed migration.

Simulation unable to continue.

---

# API Standards

REST conventions.

Plural resources.

Examples

```text
GET /citizens

GET /settlements

GET /history
```

Avoid verbs inside URLs.

Bad

```text
/getCitizens
```

Good

```text
/citizens
```

---

# Database Standards

Use Entity Framework Core.

Never bypass repositories.

Never write raw SQL unless justified.

Migrations must be version-controlled.

Never modify historical records manually.

---

# UI Standards

The Observatory should feel like

* a monitoring platform
* a research application
* a scientific instrument

It should never resemble

* an RPG
* a city builder HUD
* an RTS interface

---

# shadcn/ui Standards

Preferred components

* Card
* Table
* Tabs
* Sheet
* Dialog
* Badge
* Tooltip
* Scroll Area
* Command
* Alert
* Skeleton

Reuse existing components whenever possible.

Avoid creating custom components that duplicate shadcn functionality.

---

# Responsive Design

Desktop first.

Support

Desktop

Tablet

Mobile

No page should require horizontal scrolling.

Tables should scroll internally.

---

# Accessibility

Support

Keyboard navigation.

Meaningful labels.

Consistent focus states.

Color should never be the sole indicator of status.

---

# Performance Standards

Avoid

Repeated database queries.

Repeated API requests.

Unnecessary React re-renders.

Duplicate simulation calculations.

Measure before optimizing.

---

# Git Standards

Feature branches only.

Merge through Pull Requests.

Never force-push **main**.

Delete merged feature branches.

Tag milestone releases.

Example

```text
v0.1.0

v0.5.0

v1.0.0
```

---

# Versioning

Use Semantic Versioning.

Major

Breaking changes.

Minor

New functionality.

Patch

Bug fixes.

Examples

```text
1.0.0

1.1.0

1.1.2
```

---

# Dependencies

Before adding a package, verify

* It solves a real problem.
* Existing packages cannot already solve it.
* It is actively maintained.
* It aligns with project architecture.

Avoid dependency bloat.

---

# AI Usage

AI may assist with

* documentation
* summaries
* explanations
* developer productivity

AI must never

* determine simulation outcomes
* overwrite history
* replace deterministic systems

All AI-generated code should be reviewed before acceptance.

---

# Pull Request Checklist

Every Pull Request should answer

* What was implemented?
* Why was it implemented?
* Which TG-DEV document does it satisfy?
* Were architectural changes introduced?
* Does it remain Blueprint compliant?

---

# Release Checklist

Before creating a release

Confirm

✓ Builds pass

✓ CI passes

✓ Database migrations validated

✓ Documentation updated

✓ Version incremented

✓ Changelog updated

✓ Repository clean

---

# Project Completion Standard

The Garden Version 1.0 is considered complete when

* Every Blueprint document is implemented.
* Every TG-DEV document is completed.
* The Constitutional Laws remain unbroken.
* The Simulation Engine remains the sole source of truth.
* The Observatory remains read-only.
* History remains immutable.
* The world operates continuously without player intervention.

If any of these conditions are violated, Version 1.0 is **not** considered complete.

---

# Closing Statement

The purpose of these standards is not to slow development.

Their purpose is to protect the integrity of **The Garden**.

Features can always be added later.

Architecture is much harder to repair once compromised.

When uncertainty arises, choose the implementation that best preserves determinism, clarity, maintainability, and the Constitutional Laws established by the Blueprint.

A world that never sleeps deserves an engineering foundation that will endure.
