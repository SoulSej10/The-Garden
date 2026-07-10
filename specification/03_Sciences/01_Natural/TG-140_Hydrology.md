# The Garden Specification

# Volume III — Natural Sciences

# TG-140 — Hydrology

**Document ID:** TG-140

**Volume:** III — Natural Sciences

**Scientific Discipline:** Hydrology

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Depends On**

* TG-100 Planetary Model & Physical Laws
* TG-110 Celestial Mechanics & Seasons
* TG-120 Meteorology
* TG-130 Climate

---

# Purpose

Hydrology governs the movement, storage, and distribution of water throughout the world.

Its purpose is to transform atmospheric water into rivers, lakes, wetlands, groundwater, and floodplains that sustain ecosystems and civilizations.

Water is the lifeblood of The Garden.

Every living system ultimately depends upon its availability.

---

# Scientific Basis

Hydrology models the natural water cycle using simplified but believable physical processes.

Rather than simulating every water molecule, the system tracks the flow of water between major reservoirs and landscapes.

The objective is emergent environmental behavior while maintaining computational efficiency.

---

# Design Philosophy

Water is always moving.

Sometimes slowly.

Sometimes violently.

Rain becomes streams.

Streams become rivers.

Rivers nourish forests.

Floods reshape valleys.

Droughts expose riverbeds.

Civilizations rise beside water because life follows water.

Hydrology is not a decoration.

It is one of the primary architects of history.

---

# Core Concepts

Water follows gravity.

Water accumulates.

Water evaporates.

Water infiltrates the soil.

Water sustains life.

Water reshapes the land over time.

Every body of water exists because of upstream causes.

---

# The Hydrological Cycle

The Garden models a continuous water cycle.

```text
Evaporation
        ↓
Atmosphere
        ↓
Condensation
        ↓
Precipitation
        ↓
Surface Water
        ↓
Groundwater
        ↓
Rivers
        ↓
Lakes & Oceans
        ↓
Evaporation
```

This cycle never stops.

It is one of the oldest systems in the simulation.

---

# Water Sources

Water originates from multiple sources.

Examples:

Rainfall

Snowmelt

Springs

Groundwater

Lakes

Rivers

Glaciers (future)

Oceans

Each source behaves differently while remaining part of the same water cycle.

---

# Rivers

Rivers transport water across the landscape.

Every river possesses:

Source

Direction

Flow Rate

Depth

Width

Velocity

Watershed

Seasonal Variation

Rivers should emerge naturally from terrain rather than being manually placed whenever possible.

---

# Lakes

Lakes act as natural water reservoirs.

Properties include:

Surface Area

Depth

Water Volume

Inflow

Outflow

Water Quality

Seasonal Variation

Lakes moderate regional climate and support biodiversity.

---

# Wetlands

Wetlands form where water accumulates persistently.

Wetlands provide:

High biodiversity

Natural flood control

Water filtration

Rich soils

Disease vectors (future)

Wetlands should emerge from terrain and water movement.

---

# Groundwater

Groundwater stores water beneath the surface.

It supports:

Springs

Plant roots

Wells

Drought resistance

Groundwater changes more slowly than surface water.

---

# Soil Moisture

Every land region possesses soil moisture.

Soil moisture affects:

Plant growth

Agriculture

Wildfire risk

Erosion

Construction

Animal habitats

Soil moisture varies according to rainfall, evaporation, and drainage.

---

# Floodplains

Floodplains form beside rivers.

They provide:

Extremely fertile soil

High agricultural productivity

Flood risk

Settlement opportunities

Many early civilizations naturally emerge in floodplains.

---

# Water Quality

Water possesses environmental quality.

Factors include:

Freshness

Pollution (future)

Sediment

Stagnation

Biological contamination (future)

Water quality influences health, agriculture, and ecosystems.

---

# State Variables

Hydrology owns:

River Networks

Lake Systems

Groundwater Levels

Surface Water

Soil Moisture

Water Flow

Flood Risk

Water Quality

Watersheds

Seasonal Water Storage

---

# Simulation Rules

Hydrology evolves continuously.

Rules include:

Water always flows downhill.

Rain increases surface water.

Evaporation reduces surface water.

Groundwater replenishes gradually.

Flooding occurs when river capacity is exceeded.

Drought reduces available freshwater.

Water seeks equilibrium while remaining dynamic.

---

# Hydrological Events

Examples include:

RiverFlooded

RiverDried

LakeExpanded

LakeContracted

SpringDiscovered

GroundwaterDepleted

FloodOccurred

DroughtIntensified

ReservoirFilled

These events propagate through the Causality Engine.

---

# Relationships

Hydrology directly influences:

Geology

Biomes

Natural Resources

Ecology

Agriculture

Wildlife

Disease

Transportation

Trade

Construction

Settlement Formation

Civilization

Nearly every human settlement depends upon nearby freshwater.

---

# Edge Cases

The system should support:

Seasonal rivers.

Dry riverbeds.

Floodplains.

Natural lakes.

Endorheic basins.

Mountain streams.

Coastal estuaries.

Large inland seas (future).

These create unique regional identities.

---

# Performance Considerations

Water simulation should prioritize watershed-level behavior over per-tile calculations where possible.

Only regions experiencing significant hydrological change require intensive updates.

Static water bodies should be cached efficiently.

---

# Future Extensions

Potential future additions include:

River erosion.

Delta formation.

Canal construction.

Dams.

Aqueducts.

Irrigation.

Water pollution.

Groundwater depletion by civilization.

Reservoir management.

Underground rivers.

Water rights between kingdoms.

---

# Relationship to Civilization

Water determines where civilization begins.

Villages seek rivers.

Cities expand around lakes.

Trade follows navigable waterways.

Kingdoms compete for fertile valleys.

Floods inspire engineering.

Droughts trigger migration.

Control of water becomes one of the greatest sources of prosperity—and conflict.

No civilization can ignore the movement of water.

---

# Closing Statement

The sky gives water.

The land guides it.

The rivers carry it.

The forests depend upon it.

The fields drink it.

The people build beside it.

Water is not merely another resource within The Garden.

It is the invisible thread that connects mountains to oceans, storms to harvests, wilderness to civilization, and one generation to the next.
