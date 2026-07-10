# The Garden Specification

# Volume III — Natural Sciences

# TG-160 — Biomes

**Document ID:** TG-160

**Volume:** III — Natural Sciences

**Scientific Discipline:** Biogeography

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Depends On**

* TG-100 Planetary Model & Physical Laws
* TG-110 Celestial Mechanics & Seasons
* TG-120 Meteorology
* TG-130 Climate
* TG-140 Hydrology
* TG-150 Geology

---

# Purpose

Biomes define the living environments of the planet.

They represent the natural equilibrium created by climate, water, geology, and time.

A biome is not terrain.

It is the ecological identity of a region.

Biomes determine what life can naturally exist, flourish, or struggle within a particular environment.

---

# Scientific Basis

Biomes emerge from long-term environmental conditions.

Temperature, precipitation, elevation, soil moisture, and geological features combine over time to produce stable ecological regions.

The Garden models these interactions using simplified environmental rules while preserving believable ecological diversity.

---

# Design Philosophy

Nature organizes itself.

No one plants an entire forest.

No one paints a desert.

The world continuously adjusts toward ecological equilibrium.

Every biome should feel earned.

Players should be able to understand why a biome exists simply by observing the surrounding environment.

---

# Core Concepts

A biome is an emergent environmental system.

It is:

Not terrain.

Not vegetation.

Not climate.

Instead, it is the long-term expression of all three.

Biomes evolve slowly and rarely change abruptly.

---

# Biome Formation

Biome generation considers:

Long-term temperature

Annual rainfall

Humidity

Elevation

Water availability

Drainage

Soil moisture

Terrain stability

Seasonal variation

Neighboring ecosystems

No single environmental factor determines the final biome.

---

# Default Biomes

The initial simulation supports:

Ocean

Coastal

Beach

Grassland

Prairie

Savanna

Temperate Forest

Tropical Forest

Rainforest

Wetland

Marsh

Swamp

Shrubland

Desert

Semi-Arid

Taiga

Tundra

Alpine

Rocky Highlands

Volcanic

Future versions may introduce additional biome classifications.

---

# Biome Characteristics

Every biome defines:

Primary vegetation

Typical wildlife

Soil fertility

Water retention

Resource richness

Fire susceptibility

Travel difficulty

Agricultural suitability

Construction suitability

Population carrying capacity

Climate resilience

---

# Ecological Stability

Every biome possesses ecological stability.

Stable environments resist sudden transformation.

Persistent environmental pressure gradually shifts biome boundaries.

Examples:

Extended drought may convert grassland into semi-arid land.

Long-term reforestation may restore woodland.

Wetlands may shrink during prolonged dry periods.

Biome evolution occurs over decades rather than days.

---

# Biome Boundaries

Biome transitions should be gradual.

Ecotones naturally form between neighboring environments.

Examples:

Forest → Woodland → Grassland

Grassland → Shrubland → Desert

Mountain Forest → Alpine → Tundra

Transition zones often contain the greatest biodiversity.

---

# Productivity

Every biome possesses biological productivity.

Productivity influences:

Plant growth

Food production

Wildlife populations

Human carrying capacity

Resource regeneration

Carbon storage (future)

Productivity varies seasonally.

---

# Carrying Capacity

Each biome has natural ecological limits.

These limits affect:

Maximum vegetation density

Animal populations

Human settlement potential

Agricultural expansion

Resource extraction

Overuse may temporarily exceed carrying capacity but creates environmental stress.

---

# State Variables

The Biome System owns:

Biome Type

Ecological Stability

Productivity

Fertility

Moisture Index

Vegetation Density

Succession Stage

Carrying Capacity

Environmental Stress

---

# Simulation Rules

Biomes emerge rather than being manually assigned.

Rules include:

Climate defines possibilities.

Water enables growth.

Geology constrains development.

Vegetation reinforces biome stability.

Long-term environmental shifts alter biome identity.

Rapid biome transitions are exceptional.

Every biome change must have identifiable environmental causes.

---

# Biome Events

Examples include:

ForestExpanded

ForestDeclined

WetlandFormed

WetlandDried

DesertExpanded

GrasslandRecovered

BiomeShiftStarted

BiomeShiftCompleted

EcologicalRecovery

EcologicalCollapse

These events are propagated through the Causality Engine.

---

# Relationships

Biomes directly influence:

Natural Resources

Ecology

Plant Distribution

Animal Distribution

Agriculture

Wildfire

Disease

Transportation

Settlement Formation

Civilization Development

Story Generation

Every living system begins with its biome.

---

# Edge Cases

The simulation should support:

Oases within deserts.

Mountain forests above grasslands.

Coastal rainforests.

River corridors through arid regions.

High-altitude wetlands.

Volcanic ecosystems.

Seasonally flooded forests.

Naturally fragmented habitats.

These unusual environments encourage unique ecological and historical outcomes.

---

# Performance Considerations

Biome calculations should occur infrequently.

Most environmental updates should modify biome state gradually through accumulated environmental trends.

Neighboring regions may share cached environmental evaluations where appropriate.

---

# Future Extensions

Potential future additions include:

Ecological succession.

Primary and secondary forests.

Old-growth ecosystems.

Invasive species.

Human-driven biome conversion.

Rewilding.

Climate migration.

Artificial ecosystems.

Terraforming.

---

# Relationship to Civilization

Civilizations do not choose their biome.

They inherit it.

Forests encourage timber industries.

Grasslands favor agriculture.

Wetlands provide rich fisheries but increase disease risk.

Mountains limit expansion while protecting borders.

Deserts demand adaptation and efficient water management.

The identity of a civilization is often inseparable from the biome in which it develops.

---

# Closing Statement

The land provides the foundation.

The sky provides the rhythm.

Water provides life.

Nature answers with diversity.

Forests rise where rain endures.

Grasslands stretch where seasons balance growth.

Deserts emerge where scarcity prevails.

Mountains shelter fragile alpine worlds.

Every biome is the planet expressing itself through life.

Together, they transform an empty landscape into a living world, ready for the countless organisms—and civilizations—that will one day call it home.
