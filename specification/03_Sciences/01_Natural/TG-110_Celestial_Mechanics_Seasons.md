# The Garden Specification

# Volume III — Natural Sciences

# TG-110 — Celestial Mechanics & Seasons

**Document ID:** TG-110

**Volume:** III — Natural Sciences

**Scientific Discipline:** Celestial Mechanics & Seasonal Cycles

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Depends On**

* TG-004 Chronology
* TG-005 World State
* TG-007 Causality Engine
* TG-100 Planetary Model & Physical Laws

---

# Purpose

Time alone does not create change.

The movement of celestial cycles transforms time into seasons, daylight, temperature, and environmental rhythms.

The purpose of this document is to define the astronomical and seasonal systems that regulate every recurring natural process within The Garden.

These cycles provide the world's natural rhythm and become the foundation for weather, ecology, agriculture, migration, and civilization.

---

# Scientific Basis

The Garden models a simplified terrestrial planet with predictable celestial cycles.

Scientific realism is desirable where it enhances gameplay and emergent simulation, but unnecessary complexity should always be avoided.

The simulation values consistency over astrophysical precision.

---

# Design Philosophy

Time is measured.

Seasons are experienced.

The simulation should not merely know that it is Day 183.

The world should feel that it is entering autumn.

Citizens should prepare for winter.

Animals should migrate.

Trees should shed leaves.

Farmers should adjust planting schedules.

Civilizations should build traditions around recurring natural cycles.

---

# Core Concepts

Celestial mechanics govern recurring environmental change.

The system is deterministic.

Given the same simulation seed, identical worlds will always experience identical seasonal progression.

Random weather exists within predictable seasonal boundaries.

---

# Calendar System

The Garden maintains a canonical calendar.

The calendar exists independently from civilizations.

Civilizations may eventually invent their own calendars, but the simulation operates on one universal chronological system.

The calendar tracks:

* Year
* Season
* Day
* Time of Day

---

# Day and Night Cycle

Every day is divided into distinct phases.

Recommended phases:

* Dawn
* Morning
* Midday
* Afternoon
* Dusk
* Night
* Midnight

These phases influence:

* Visibility
* Citizen schedules
* Wildlife behavior
* Temperature
* Travel
* Security
* Social activity

Future implementations may interpolate continuously between phases.

---

# Annual Cycle

A year consists of four seasons.

The default sequence is:

Spring

↓

Summer

↓

Autumn

↓

Winter

Each season has a defined duration.

Season length should be configurable during world generation but remain constant throughout the life of that world.

---

# Seasonal Identity

## Spring

Characteristics:

* Increasing temperatures
* Frequent rainfall
* Rapid plant growth
* Animal breeding
* River expansion from snowmelt (future)
* Agricultural planting

---

## Summer

Characteristics:

* Highest temperatures
* Long daylight hours
* Strong vegetation growth
* Increased evaporation
* High agricultural productivity
* Elevated wildfire risk

---

## Autumn

Characteristics:

* Cooling temperatures
* Harvest season
* Leaf coloration
* Animal preparation for winter
* Increased food storage
* Migration begins

---

## Winter

Characteristics:

* Lowest temperatures
* Short daylight hours
* Reduced biological activity
* Scarce food production
* Increased survival challenges
* Frozen landscapes in cold regions

---

# Time of Day

Every simulation tick belongs to one period of the day.

Time of day affects:

Citizen routines.

Animal activity.

Temperature.

Visibility.

Travel efficiency.

Crime (future).

Festivals.

Construction.

Exploration.

---

# Sunlight

Sunlight is the primary environmental energy source.

It influences:

Photosynthesis.

Plant growth.

Surface temperature.

Seasonal transitions.

Future solar-powered systems.

Sunlight intensity varies according to:

Season.

Time of day.

Weather conditions.

Terrain obstruction (future).

---

# Seasonal Variables

The seasonal system owns:

Current season.

Current day.

Current year.

Day length.

Night length.

Sunlight intensity baseline.

Season progression.

These variables provide inputs for downstream sciences.

---

# Simulation Rules

The seasonal system progresses automatically.

Rules include:

* Seasons change only through the passage of time.
* Day/night transitions follow the calendar.
* Seasonal transitions occur gradually rather than instantaneously where practical.
* Biological systems should anticipate seasonal change rather than react only after it occurs.

---

# Seasonal Events

Examples:

SpringStarted

SummerStarted

AutumnStarted

WinterStarted

Sunrise

Sunset

LongestDay

LongestNight

SeasonEnded

These events are distributed through the Causality Engine.

---

# Relationships

The seasonal system directly influences:

Meteorology.

Climate.

Hydrology.

Ecology.

Agriculture.

Animal behavior.

Citizen schedules.

Construction.

Transportation.

Religion.

Festivals.

Economy.

Nearly every long-term simulation discipline references seasonal state.

---

# Edge Cases

The simulation should support:

Exceptionally long winters.

Short growing seasons.

Harsh continental climates.

Island climates.

Future hemispherical variations.

Future polar regions.

These configurations should emerge naturally from world generation rather than special-case logic.

---

# Performance Considerations

Seasonal calculations are predictable.

Most values should be derived mathematically rather than recomputed through expensive simulation.

Only seasonal transitions should trigger significant event propagation.

---

# Future Extensions

Potential future additions include:

Axial tilt customization.

Variable year lengths.

Lunar cycles.

Eclipses.

Multiple moons.

Planetary events.

Tidal influences.

Astronomical observations.

Calendars created by civilizations.

Religious observances tied to celestial events.

---

# Relationship to Civilization

Although celestial mechanics are natural systems, civilizations respond culturally.

Examples include:

Harvest festivals.

Winter preparation.

Planting ceremonies.

Seasonal markets.

Religious holidays.

Migration traditions.

Military campaigning seasons.

Trade schedules.

Nature creates cycles.

Civilizations create meaning from those cycles.

---

# Closing Statement

The movement of time gives the world direction.

The movement of the heavens gives it rhythm.

Every sunrise begins new possibilities.

Every winter tests survival.

Every spring renews hope.

Every autumn rewards preparation.

The seasons are not merely changes in weather—they are the heartbeat by which every living thing in The Garden learns when to grow, when to rest, when to journey, and when to endure.
