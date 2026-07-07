# TG-DEV-004 — Citizens & Life

**Document Version:** 1.0 (Living Document)
**Status:** Active
**Prerequisites:** TG-DEV-001, TG-DEV-002, TG-DEV-003
**Estimated Duration:** Week 7–8

---

# Purpose

This phase introduces the first living inhabitants of **The Garden**.

For the first time, the world will contain autonomous citizens capable of living independently within the simulated environment.

Citizens are **not NPCs**.

They are complete simulated individuals with their own identities, biological needs, personalities, schedules, memories, and decisions. They are not created for the player's entertainment, nor do they exist to perform scripted behaviors.

The simulation should continue functioning indefinitely even if no player is connected.

---

# Phase Objectives

## Primary Goal

Implement a complete citizen life simulation that allows individuals to survive, make decisions, interact with their environment, and naturally form the foundation for future societies.

## Success Criteria

✓ Citizens spawn successfully

✓ Citizens possess unique identities

✓ Citizens satisfy basic survival needs

✓ Daily routines operate autonomously

✓ Citizens navigate the world

✓ Citizens interact with environmental resources

✓ Aging functions correctly

✓ Death functions correctly

✓ Population remains stable over long simulations

✓ Citizens are observable through the Observatory

---

# Development Rules

Every citizen is equal.

No hidden "main character."

No hero system.

No plot armor.

No scripted personalities.

No artificial intelligence shortcuts that violate determinism.

Citizens make decisions based only on:

* Current world state
* Internal needs
* Personal traits
* Memories
* Available knowledge

Never because "the game needs it."

---

# Feature Branches

```text
feature/citizens

feature/needs

feature/daily-routines

feature/pathfinding

feature/aging

feature/death

feature/citizen-dashboard

feature/population
```

---

# Citizen Entity

## Task 1

Create the Citizen entity.

Minimum properties

* Unique ID
* First Name
* Last Name
* Birth Date
* Age
* Biological Sex
* Current Location
* Home (nullable)
* Occupation (future-ready)
* Family (future-ready)
* Current State
* Current Goal
* Current Activity

Citizens should be serializable for future save/load functionality.

---

# Citizen Attributes

Each citizen possesses permanent attributes.

Examples

* Strength
* Endurance
* Intelligence
* Dexterity
* Perception

These attributes influence future simulation systems.

Attributes should not determine destiny.

They only influence probabilities and capabilities.

---

# Personality System

Implement personality traits.

Examples

* Curious
* Patient
* Aggressive
* Compassionate
* Hardworking
* Introverted
* Extroverted

Traits influence decision-making but never override survival priorities.

The system must allow future expansion without modifying existing citizens.

---

# Needs System

## Task 2

Implement biological needs.

Minimum needs

* Hunger
* Thirst
* Energy
* Warmth
* Health

Every need continuously changes over time.

Each need has

* Current Value
* Maximum Value
* Warning Threshold
* Critical Threshold

Ignoring needs should naturally increase the risk of illness or death.

---

# Daily Routine System

## Task 3

Citizens should naturally progress through activities.

Examples

* Wake Up
* Walk
* Eat
* Drink
* Gather Resources
* Rest
* Explore
* Sleep

Activities are selected based on current priorities.

No fixed daily schedule.

No scripting.

Behavior emerges from decision-making.

---

# Decision System

Implement utility-based decision making.

Every simulation cycle

Citizen

↓

Observe surroundings

↓

Evaluate needs

↓

Generate possible actions

↓

Score each action

↓

Select highest-value action

↓

Execute

Future systems can add new actions without modifying the core decision engine.

---

# Movement System

## Task 4

Implement navigation.

Requirements

* Tile-based movement
* Terrain cost consideration
* Water avoidance (unless future abilities allow crossing)
* Destination validation

Movement speed depends on terrain.

Citizens should never teleport.

---

# World Interaction

Citizens should interact with the environment.

Examples

* Drink from lakes
* Gather berries
* Chop trees (future-ready)
* Collect stones
* Rest beneath trees
* Seek shelter

Interactions generate events.

---

# Spawn System

## Task 5

Implement citizen spawning.

Requirements

Spawn only on habitable terrain.

Avoid

* Oceans
* Deep lakes
* Mountains

Spawn population should be configurable.

Examples

* 10
* 100
* 500
* 1,000

Future world generators may adjust starting populations.

---

# Aging System

Implement life progression.

Life stages

* Infant
* Child
* Teen
* Adult
* Elder

Age updates automatically.

No sudden transitions.

Citizens should physically and statistically progress over time.

---

# Health System

Implement health.

Health influenced by

* Hunger
* Thirst
* Exposure
* Age

Future disease systems should integrate without redesigning this system.

---

# Death System

## Task 6

Citizens may die from

* Old Age
* Starvation
* Dehydration
* Exposure

Death is permanent.

Death immediately

* removes active simulation behavior
* generates historical events
* preserves citizen records

Never delete a citizen from history.

---

# Population Manager

Implement

PopulationManager

Responsibilities

* Spawn
* Remove deceased citizens
* Track population
* Publish demographic statistics

No artificial population balancing.

Population should naturally fluctuate.

---

# Citizen Events

Implement immutable events.

Examples

* CitizenSpawned
* CitizenMoved
* CitizenAte
* CitizenDrank
* CitizenSlept
* CitizenRested
* CitizenAged
* CitizenBecameHungry
* CitizenDied

Events contain facts only.

No embedded business logic.

---

# Scheduler Integration

Hourly

* Needs
* Movement
* Decision Making

Daily

* Health
* Aging checks

Monthly

* Population statistics

---

# Observatory

Create the Citizens page.

Sections

## Population Summary

Cards

* Total Population
* Births
* Deaths
* Average Age

---

## Citizen Table

Columns

* Name
* Age
* Current Activity
* Health
* Hunger
* Energy
* Location

Requirements

* Search
* Pagination
* Sticky Header
* Internal Scroll
* Sorting

No infinite scrolling.

---

## Citizen Details

Selecting a citizen opens a side panel.

Display

* Identity
* Attributes
* Personality
* Needs
* Current Goal
* Current Activity
* Current Tile
* Recent Events

Future tabs

* Relationships
* Memories
* Inventory
* Family

---

## Population Charts

Display

* Age Distribution
* Population Trend
* Causes of Death

Use Recharts.

Keep visualizations simple and readable.

---

# UI Standards

Continue using shadcn/ui.

Primary components

* Card
* Table
* Sheet
* Badge
* Tabs
* Avatar
* Progress
* Scroll Area

Use Progress components to visualize citizen needs.

Avoid decorative elements that imply game mechanics.

---

# API Endpoints

Create

```text
GET /citizens

GET /citizens/{id}

GET /citizens/population

GET /citizens/statistics

GET /citizens/events
```

Read-only.

No manual editing endpoints.

The simulation remains the sole authority.

---

# Logging

Record

* Citizen creation
* Daily decisions
* Activity changes
* Aging
* Deaths
* Critical health conditions

Support verbose logging for debugging individual citizens.

---

# Performance Targets

Support

* 1,000 active citizens

Average citizen update

< 2 ms

Population update

Stable

No duplicate citizens.

No invalid world positions.

No orphaned records.

---

# Definition of Completion

Backend

✓ Citizen simulation operational

✓ Needs system complete

✓ Decision engine complete

✓ Movement complete

✓ Aging operational

✓ Death operational

✓ Population manager complete

✓ Citizen events published

---

Frontend

✓ Citizens dashboard complete

✓ Population statistics visible

✓ Citizen inspection panel complete

✓ Charts operational

---

Infrastructure

✓ CI passing

✓ Repository updated

✓ Documentation synchronized

---

# Bi-Weekly Progress Report (End of Week 8)

OpenCode shall provide:

## Completed Features

* Citizen System
* Needs
* Daily Decision Engine
* Movement
* Aging
* Death
* Population Manager
* Citizens Observatory

## Metrics

* Current population
* Average life expectancy
* Average decision execution time
* Average movement time
* Citizen update performance

## Technical Decisions

* Decision algorithm
* Pathfinding implementation
* Need balancing
* Attribute structure

## Remaining Issues

Document any limitations before introducing social structures and settlements.

## Ready for Next Phase

Confirm readiness to begin **TG-DEV-005 — Settlements & Economy**.

The simulation should now sustain a small autonomous population capable of surviving in the environment without player intervention.
