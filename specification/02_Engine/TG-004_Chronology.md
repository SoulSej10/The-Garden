# The Garden Design Bible

# Document TG-004 — Chronology: Time System & Tick Engine

**Document ID:** TG-004

**Version:** 0.1

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Scientific Discipline:** Chronology

**Depends On**

* TG-000 Vision
* TG-001 Constitution
* TG-002 Architecture
* TG-003 Project Structure

---

# Purpose

Chronology defines the flow of existence inside The Garden.

Nothing may occur unless time advances.

Every birth.

Every storm.

Every harvest.

Every migration.

Every death.

Every civilization.

Every memory.

Exists because Chronology permits it.

Chronology is therefore the first active system of the simulation.

---

# Design Philosophy

Time is not measured.

Time is simulated.

The simulation does not ask:

"What time is it?"

Instead it asks:

"What should happen because time has advanced?"

---

# The Universal Rule

Everything in The Garden reacts to time.

Nothing acts independently of time.

This creates a deterministic simulation where every change has an identifiable moment of origin.

---

# The Tick

The Tick is the smallest indivisible unit of simulation.

A Tick is **not** one real-world second.

A Tick represents one discrete simulation update.

Its duration is configurable.

Default:

```
1 Tick = 1 Simulated Hour
```

Future configurations may support different scales, but all systems operate in ticks.

---

# Hierarchy of Time

```
Tick

↓

Hour

↓

Day

↓

Week

↓

Month

↓

Season

↓

Year

↓

Era
```

Every larger unit is derived from ticks.

Nothing skips this hierarchy.

---

# Time Authority

Chronology owns:

* Current Tick
* Current Hour
* Current Day
* Current Week
* Current Month
* Current Season
* Current Year
* Era progression
* Calendar calculations

No other module may calculate time independently.

---

# The Tick Lifecycle

Every Tick follows exactly the same sequence.

```
Begin Tick

↓

Advance Clock

↓

Publish Tick Event

↓

Simulation Modules Execute

↓

Collect Generated Events

↓

Commit World Changes

↓

Record History

↓

Notify Observers

↓

End Tick
```

This order is mandatory.

Changing the order risks introducing inconsistencies.

---

# Time Events

Chronology publishes events whenever temporal boundaries are crossed.

Examples:

HourChanged

DayStarted

DayEnded

WeekStarted

MonthStarted

SeasonChanged

YearChanged

EraChanged

SimulationPaused

SimulationResumed

SimulationStopped

Modules subscribe only to the events they require.

---

# Time Ownership

Examples:

Meteorology

Responds every hour.

Agriculture

Responds every day.

Politics

Responds every month.

Culture

Responds every season.

Technology

Responds every year.

No discipline runs continuously.

Every discipline wakes only when time notifies it.

---

# Determinism

Chronology must be deterministic.

Given:

* identical world state
* identical random seed
* identical configuration

The simulation should produce identical outcomes.

This enables:

* debugging
* replay
* testing
* historical verification

---

# Simulation Speed

Real time is only a presentation concern.

The simulation itself knows only ticks.

Suggested speeds:

Paused

1×

2×

5×

10×

25×

50×

100×

500×

1000×

Changing speed changes execution frequency—not simulation logic.

---

# Pause

Pause freezes Chronology.

Nothing progresses.

No citizen moves.

No crops grow.

No weather changes.

History remains unchanged.

---

# Single Step

Developers may advance exactly one tick.

This feature is mandatory for debugging.

Example:

```
Advance One Tick

↓

Observe

↓

Advance One Tick

↓

Observe
```

The engine should support this natively.

---

# Fast Forward

Chronology may process many ticks rapidly.

Examples:

Advance:

1 day

1 week

1 month

1 year

100 years

Fast Forward is simply repeated Tick execution.

No shortcuts.

The world must evolve naturally.

---

# Tick Categories

Not every Tick requires every module.

Example:

Hourly:

Citizens

Energy

Movement

Weather

Daily:

Food

Work

Socialization

Weekly:

Markets

Trade

Population reports

Monthly:

Politics

Migration

Infrastructure

Yearly:

Culture

Technology

Demographics

This keeps large worlds performant.

---

# Temporal Consistency

Within one Tick:

Every module observes the same current time.

No module may exist "ahead" or "behind."

This prevents paradoxes.

---

# Calendar

Initial implementation:

12 months

30 days each

4 seasons

Simple calendar.

Real-world accuracy is unnecessary.

Consistency is more important than realism.

Future civilizations may even introduce custom calendars without affecting Chronology itself.

---

# Time as a Service

Chronology exposes information.

Examples:

Current Season

Current Year

Current Day

Current Hour

Elapsed Ticks

Simulation Age

Every module queries Chronology rather than calculating time independently.

---

# Replay Capability

Because every change is tick-based, the world becomes replayable.

Future functionality:

Jump to Tick 5,000

Replay Year 42

Observe Winter 103

Investigate Citizen 284

Chronology makes replay possible.

---

# Long-Term Worlds

Chronology must support worlds lasting:

Hundreds

Thousands

Potentially millions of simulated years.

Overflow protection must therefore be considered early.

Use sufficiently large numeric types for tick counters.

---

# Debugging Tools

Chronology should expose:

Current Tick

Tick Duration

Simulation Speed

Ticks Per Second

Processing Time

Average Tick Cost

Slowest Module

This becomes invaluable during optimization.

---

# Performance Principle

The engine should never attempt to "catch up" by skipping simulation logic.

If one million ticks are required, then one million ticks execute.

Correctness is preferred over shortcuts.

Optimization should improve execution speed—not reduce simulation fidelity.

---

# Future Possibilities

Variable calendars.

Lunar cycles.

Astronomical events.

Leap years.

Multiple planets.

Multiple timelines.

Time dilation.

These are optional extensions.

The core Chronology system should remain simple and stable.

---

# Closing Statement

Chronology is not another module.

Chronology is the heartbeat of The Garden.

Every discipline awakens because time advances.

Every story exists because yesterday became today.

Without Chronology, there is no world.

Only data.

With Chronology, data becomes history.

And history becomes life.
