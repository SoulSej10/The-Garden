# TG-DEV-003 — World & Environment

**Document Version:** 1.0 (Living Document)
**Status:** Active
**Prerequisites:** TG-DEV-001, TG-DEV-002, TG-004, TG-006, TG-007
**Estimated Duration:** Week 5–6

---

# Purpose

This phase brings **The Garden** to life by creating the world's first active simulation systems.

The world shall exist before its inhabitants.

By the completion of this phase, the simulation engine should be capable of maintaining a living environment that evolves independently over time. Weather changes, seasons transition, rivers flow, forests grow, resources regenerate, and the terrain continuously changes according to deterministic simulation rules.

No citizens, settlements, villages, kingdoms, or civilizations shall exist during this phase.

---

# Phase Objectives

## Primary Goal

Implement the complete environmental simulation layer.

## Success Criteria

✓ World generation operational

✓ Terrain generation complete

✓ Biome generation complete

✓ Weather simulation operational

✓ Seasonal simulation operational

✓ Climate zones established

✓ Hydrology system operational

✓ Resource regeneration operational

✓ Ecology foundation operational

✓ Environment fully observable through the Observatory

---

# Development Rules

The environment exists independently of life.

Every environmental system must influence at least one other system.

Examples:

* Rain increases river flow.
* River flow replenishes lakes.
* Lakes increase nearby humidity.
* Humidity affects vegetation growth.
* Vegetation affects wildlife spawning (future phase).
* Seasons influence rainfall and temperature.
* Drought reduces vegetation.
* Flooding alters terrain moisture.

No environmental system may operate in isolation.

---

# Feature Branches

```text
feature/world-generation

feature/terrain

feature/biomes

feature/weather

feature/climate

feature/hydrology

feature/resources

feature/ecology

feature/environment-dashboard
```

---

# World Generation

## Task 1

Implement

* WorldGenerator
* SeedGenerator
* WorldInitializer

Requirements

* Deterministic
* Seed-based
* Configurable world size
* Repeatable generation

Generate:

* Terrain
* Initial climate
* Rivers
* Lakes
* Forests
* Natural resources

The same seed must always produce the same world.

---

# Terrain System

## Task 2

Implement terrain types.

Minimum terrain types

* Ocean
* Coast
* Plains
* Grassland
* Hills
* Mountains
* Forest
* Swamp
* River
* Lake

Each terrain type stores

* Elevation
* Moisture
* Fertility
* Temperature modifier
* Traversal cost (future use)

Terrain should be immutable unless modified through approved environmental events.

---

# World Map

Implement a tile-based world.

Each tile should contain

* Coordinates
* Terrain
* Biome
* Elevation
* Moisture
* Temperature
* Resource references
* Occupancy state

Future systems must query tiles instead of maintaining duplicate world information.

---

# Climate System

## Task 3

Implement climate zones.

Minimum climates

* Tropical
* Temperate
* Dry
* Cold
* Highland

Each climate determines

* Average rainfall
* Seasonal variation
* Average temperature
* Vegetation potential

Climate should influence weather—not replace it.

---

# Season System

## Task 4

Implement

* Spring
* Summer
* Autumn
* Winter

Season controls

* Temperature adjustments
* Rain probability
* Plant growth modifier
* River replenishment
* Resource regeneration modifier

Season transitions should be gradual.

Avoid abrupt environmental changes.

---

# Weather System

## Task 5

Implement weather states.

Minimum weather

* Clear
* Cloudy
* Rain
* Heavy Rain
* Storm
* Fog
* Snow (where applicable)

Weather stores

* Duration
* Intensity
* Temperature modifier
* Wind strength
* Humidity modifier

Weather changes must always have an explainable cause.

Random weather without climatic context is prohibited.

---

# Hydrology

## Task 6

Implement

* Rivers
* Lakes
* Ground moisture

Simulation responsibilities

* Rain fills rivers.
* Rivers feed lakes.
* Lakes evaporate.
* Drought lowers water levels.
* Water affects surrounding terrain.

Flood events should exist internally but need not be visualized yet.

---

# Resource System

## Task 7

Implement renewable resources.

Minimum resources

* Trees
* Stone
* Clay
* Wild Plants
* Fresh Water

Each resource tracks

* Quantity
* Maximum capacity
* Regeneration rate
* Environmental dependencies

Resources regenerate naturally.

No infinite resources.

---

# Ecology Foundation

## Task 8

Implement vegetation growth.

Vegetation responds to

* Temperature
* Moisture
* Terrain
* Season

Examples

* Grass spreads.
* Trees mature.
* Forest density changes.
* Dead vegetation decomposes.

Wildlife is not yet implemented.

---

# Environmental Events

Implement immutable events.

Examples

* RainStarted
* RainStopped
* SeasonChanged
* RiverExpanded
* RiverShrank
* LakeDried
* ForestExpanded
* ForestDeclined
* ResourceRegenerated
* DroughtStarted

Events must never contain simulation logic.

---

# Scheduler Integration

Register systems.

Hourly

* Weather

Daily

* Moisture
* Resource regeneration

Weekly

* Forest growth

Monthly

* Climate recalculation

Yearly

* Long-term climate adjustments

---

# Observatory

Create the Environment page.

Sections

## World Overview

Display

* Current Season
* Weather
* Average Temperature
* Humidity
* Rainfall

---

## Climate Overview

Cards

* Climate Zones
* Rainfall
* Average Temperature
* Water Coverage

---

## Resource Summary

Display

* Forest Coverage
* Water Resources
* Stone Deposits
* Vegetation Density

---

## Environmental Events

Scrollable table.

Columns

* Time
* Event
* Location
* Severity

Newest first.

---

# Map Viewer

Implement the first world map visualization.

Requirements

Display

* Terrain
* Rivers
* Lakes
* Forests
* Climate

Support

* Zoom
* Pan
* Tile inspection

Clicking a tile displays

* Coordinates
* Terrain
* Elevation
* Moisture
* Temperature
* Resources

Editing is disabled.

The Observatory observes only.

---

# UI Standards

Continue using shadcn/ui.

Primary components

* Card
* Table
* Badge
* Tooltip
* Tabs
* Scroll Area
* Separator
* Skeleton

Avoid decorative animations.

The environment should feel scientific rather than game-like.

---

# API Endpoints

Create

```text
GET /world

GET /world/map

GET /world/tiles/{id}

GET /environment/weather

GET /environment/climate

GET /environment/resources

GET /environment/events
```

Read-only endpoints only.

---

# Logging

Log

* Weather transitions
* Seasonal transitions
* Resource regeneration
* River changes
* Climate updates
* Tick execution time

Support Debug mode for tile inspection.

---

# Performance Targets

World generation

< 5 seconds (default world size)

Average environmental update

< 30 ms

Map loading

< 500 ms

No duplicated environmental events.

No resource synchronization issues.

---

# Definition of Completion

Backend

✓ World generation operational

✓ Terrain generated

✓ Climate operational

✓ Weather operational

✓ Hydrology operational

✓ Resource regeneration functioning

✓ Ecology foundation complete

✓ Environmental events published

---

Frontend

✓ Environment dashboard complete

✓ Interactive world map operational

✓ Tile inspection available

✓ Resource overview complete

✓ Weather monitoring complete

---

Infrastructure

✓ CI passing

✓ Repository updated

✓ Documentation synchronized

---

# Bi-Weekly Progress Report (End of Week 6)

OpenCode shall provide:

## Completed Features

* World Generation
* Terrain
* Climate
* Weather
* Hydrology
* Resources
* Ecology Foundation
* Environment Observatory

## Metrics

* World generation duration
* Average simulation update time
* Number of generated tiles
* Number of active environmental events

## Technical Decisions

* Terrain generation approach
* Climate calculations
* Resource regeneration strategy

## Remaining Issues

Document any limitations that must be addressed before introducing living entities.

## Ready for Next Phase

Confirm that the environment is capable of sustaining life before beginning **TG-DEV-004 — Citizens & Life**.

The world should continue evolving indefinitely without any citizens present.
