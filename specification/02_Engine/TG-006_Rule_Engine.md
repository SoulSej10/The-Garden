# The Garden Design Bible

# Document TG-006 — Rule Engine & Simulation Pipeline

**Document ID:** TG-006

**Version:** 0.1

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Scientific Discipline:** Systemics

**Depends On**

* TG-000 Vision
* TG-001 Constitution
* TG-002 Software Architecture
* TG-003 Project Structure
* TG-004 Chronology
* TG-005 World State

---

# Purpose

This document defines how The Garden executes simulation logic.

The Rule Engine is responsible for transforming one valid World State into the next.

It does not decide *what* the world contains.

It decides *how* the world changes.

Every simulation discipline—from Meteorology to Sociology—expresses its behavior through rules executed by this engine.

---

# Philosophy

The Garden is not a collection of update loops.

It is a collection of independent rules.

Rules observe the current world.

Rules evaluate conditions.

Rules propose changes.

The Rule Engine coordinates those proposals into a consistent next state.

---

# The Fundamental Cycle

Every Tick follows the same logical flow.

```text
Current World State
        ↓
Chronology Advances
        ↓
Eligible Rules Execute
        ↓
Events Produced
        ↓
World Changes Validated
        ↓
History Recorded
        ↓
New World State
```

No rule bypasses this cycle.

---

# What Is a Rule?

A Rule is the smallest unit of simulation behavior.

A Rule answers one question:

> "If these conditions are true, what should happen?"

Examples:

* Grass grows.
* Rain evaporates.
* Hunger increases.
* Crops mature.
* Citizens age.
* Bridges decay.
* Friendships strengthen.

A rule should do one thing well.

---

# Rule Characteristics

Every rule should be:

* Deterministic (given the same inputs and seed).
* Focused on one responsibility.
* Independent of presentation.
* Testable in isolation.
* Free of side effects outside approved world mutations.

Rules are building blocks.

Complex behavior emerges from many simple rules.

---

# Rule Lifecycle

Each rule progresses through four phases.

```text
Observe
   ↓
Evaluate
   ↓
Propose
   ↓
Commit
```

### Observe

Read the current World State.

No modifications are allowed.

### Evaluate

Determine whether the rule applies.

If conditions are not met, exit.

### Propose

Create one or more proposed changes or events.

Do not modify the World State directly.

### Commit

After validation, accepted changes become part of the new World State.

---

# Read Before Write

Rules should always read from the same consistent snapshot of the current Tick.

They should never depend on partial updates made by earlier rules within the same phase.

This prevents execution-order bugs and makes behavior easier to reason about.

---

# Rule Categories

Rules may be grouped by scientific discipline.

Examples:

## Chronology

* Advance hour.
* Advance season.

## Meteorology

* Form clouds.
* Move storms.
* Change temperature.

## Ecology

* Grow trees.
* Spread forests.
* Animal migration.

## Agriculture

* Crop growth.
* Harvest readiness.
* Food spoilage.

## Sociology

* Friendship formation.
* Family growth.
* Community interaction.

## Demography

* Births.
* Deaths.
* Aging.

## Economics

* Production.
* Trade.
* Market prices.

## Politics

* Elections.
* Diplomacy.
* Territory changes.

Each discipline contributes rules rather than owning the simulation loop.

---

# Rule Scheduling

Not every rule executes every Tick.

Examples:

Hourly:

* Hunger increases.
* Citizen movement.
* Weather changes.

Daily:

* Work.
* Farming.
* Social interaction.

Weekly:

* Trade.
* Migration.

Monthly:

* Governance.
* Infrastructure.

Yearly:

* Cultural evolution.
* Technology.
* Population statistics.

Chronology determines when rules become eligible.

---

# Simulation Pipeline

The Rule Engine executes rules in ordered stages.

```text
1. Time
2. Environment
3. Resources
4. Living Creatures
5. Settlements
6. Economy
7. Politics
8. Culture
9. Validation
10. History
11. Story Publication
```

The stages are stable.

Individual rules within a stage may evolve over time.

---

# World Mutations

Rules do not directly change the World State.

Instead, they generate mutation proposals.

Examples:

* Increase food by 12.
* Move citizen to village.
* Reduce river depth.
* Create settlement.

The engine validates these proposals before committing them.

This centralizes consistency checks.

---

# Conflict Resolution

Sometimes multiple rules affect the same entity.

Example:

A citizen is injured in a storm.

The same citizen receives medical care.

Both proposals are valid.

The Rule Engine resolves conflicts using deterministic ordering and domain-specific resolution strategies.

Conflict resolution must be explicit and testable.

---

# Validation Phase

Before a Tick completes, the engine validates the proposed World State.

Examples of invalid outcomes:

* Negative population.
* Food below zero when not permitted.
* Duplicate identities.
* Invalid parent-child relationships.
* Citizens occupying impossible locations.

Invalid changes are rejected or corrected according to documented rules.

The simulation must never end a Tick in an inconsistent state.

---

# Event Generation

Rules may emit events describing what occurred.

Examples:

CitizenBorn

HarvestCompleted

StormMoved

TreeDied

BridgeCollapsed

Events are immutable records of change.

They allow other disciplines to react without tight coupling.

---

# Deterministic Randomness

Randomness is allowed.

Chaos is not.

Every stochastic decision must originate from a seeded random source controlled by the engine.

Given the same seed, configuration, and World State, the simulation should produce the same sequence of outcomes.

This enables replay, debugging, and scientific experimentation.

---

# Observability

Every rule execution should be observable in development mode.

Developers should be able to inspect:

* Which rules executed.
* Execution duration.
* Proposed mutations.
* Accepted mutations.
* Rejected mutations.
* Generated events.

The Rule Engine should make debugging straightforward.

---

# Performance Principles

Optimize only after correctness.

When optimization is required:

* Reduce unnecessary evaluations.
* Execute only eligible rules.
* Partition large datasets.
* Cache read-only calculations within a Tick when safe.
* Parallelize independent work only if determinism can be preserved.

Performance must never compromise simulation integrity.

---

# Extensibility

Adding a new scientific discipline should involve:

1. Defining new entities if required.
2. Creating new rules.
3. Registering those rules with the Rule Engine.
4. Writing validation tests.

Existing disciplines should require little or no modification.

---

# Example Walkthrough

A simplified sequence:

1. Chronology advances to a new day.
2. Weather rules determine rainfall.
3. Rain increases soil moisture.
4. Agriculture rules detect improved conditions.
5. Crops gain growth progress.
6. Citizens harvest mature crops.
7. Food stores increase.
8. Hunger decreases.
9. Population health improves.
10. Events are recorded.
11. Stories are generated.

No single rule "creates" this story.

The interaction of many simple rules does.

---

# Architectural Motto

Small rules.

Shared reality.

Emergent history.

---

# Closing Statement

The Rule Engine is the nervous system of The Garden.

Chronology provides the heartbeat.

The World State provides the body.

The Rule Engine allows every discipline to sense, respond, and evolve without losing coherence.

The Garden should never feel programmed.

It should feel governed by natural laws.
