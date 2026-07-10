# The Garden Specification

# Volume III — Natural Sciences

# TG-120 — Meteorology

**Document ID:** TG-120

**Volume:** III — Natural Sciences

**Scientific Discipline:** Meteorology

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Depends On**

* TG-100 Planetary Model & Physical Laws
* TG-110 Celestial Mechanics & Seasons
* TG-007 Causality Engine

---

# Purpose

Meteorology governs the atmosphere.

Its purpose is to simulate believable weather patterns that emerge naturally from the interaction of geography, seasons, water, and temperature.

Weather is not cosmetic.

Weather influences nearly every living system within The Garden.

Every storm leaves consequences.

Every drought changes history.

Every rainfall creates opportunity.

---

# Scientific Basis

The Garden simulates weather using simplified atmospheric principles.

Scientific accuracy is valued where it creates believable behavior, but the system prioritizes emergent gameplay, deterministic simulation, and computational efficiency over perfect physical realism.

The objective is a living atmosphere rather than a meteorological simulator.

---

# Design Philosophy

Weather should emerge.

It should never appear random without cause.

Rain falls because moisture exists.

Snow forms because temperatures permit it.

Storms develop because atmospheric conditions become unstable.

Every weather event should be explainable through the simulation.

The player may not always know the cause.

The engine always does.

---

# Core Concepts

Weather is regional.

Different regions experience different atmospheric conditions simultaneously.

The atmosphere is continuous.

Weather evolves gradually.

Extreme weather is uncommon but meaningful.

The world should spend most of its life in ordinary weather.

Rare events become memorable precisely because they are rare.

---

# Atmospheric State

Each region maintains an atmospheric state.

The atmospheric state consists of:

Air Temperature

Humidity

Atmospheric Pressure

Cloud Coverage

Wind Speed

Wind Direction

Precipitation

Visibility

These values evolve continuously over time.

---

# Weather Conditions

Possible weather states include:

Clear

Partly Cloudy

Overcast

Light Rain

Heavy Rain

Thunderstorm

Snow

Blizzard

Fog

Heatwave

Cold Snap

High Winds

Future weather types may include:

Hail

Dust Storms

Tornadoes

Hurricanes

Monsoons

Ashfall

---

# Weather Formation

Weather emerges from interacting variables.

Examples:

High humidity + cooling temperatures → Rain.

Low humidity + prolonged heat → Drought.

Cold temperatures + precipitation → Snow.

Pressure instability + moisture → Storms.

No single variable determines weather.

Multiple factors combine to produce atmospheric behavior.

---

# Temperature

Temperature varies according to:

Season

Time of day

Elevation

Latitude (future)

Cloud cover

Nearby water bodies

Temperature directly influences:

Plant growth

Animal activity

Citizen comfort

Disease spread

Food preservation

Snow formation

Ice formation (future)

---

# Humidity

Humidity measures atmospheric moisture.

Humidity influences:

Rain probability

Fog

Plant health

Human comfort

Disease transmission

Fire risk

Humidity changes through:

Evaporation

Rainfall

Seasonal conditions

Large bodies of water

Vegetation

---

# Wind

Wind transports atmospheric energy.

Wind influences:

Storm movement

Cloud movement

Wildfire spread

Seed dispersal (future)

Travel difficulty

Sailing (future)

Smoke movement

Wind possesses:

Speed

Direction

Persistence

---

# Clouds

Clouds are temporary atmospheric formations.

Cloud coverage influences:

Sunlight

Temperature

Rain probability

Visibility

Agricultural productivity

Clouds gradually form and dissipate.

---

# Precipitation

Precipitation transfers water from atmosphere to planet.

Forms include:

Rain

Snow

Future:

Hail

Freezing rain

Precipitation influences:

Rivers

Ground moisture

Crop growth

Flooding

Reservoirs

Wildlife

Freshwater availability

---

# Visibility

Visibility affects:

Travel

Exploration

Hunting

Military operations

Construction

Observation

Fog and storms reduce visibility.

Clear weather improves it.

---

# Seasonal Influence

Each season modifies atmospheric tendencies.

Spring:

High rainfall.

Variable temperatures.

Summer:

Heat.

Convective storms.

Autumn:

Cooling.

Fog.

Harvest weather.

Winter:

Snow.

Cold.

Reduced evaporation.

Seasons guide weather.

They do not dictate every day's conditions.

---

# State Variables

Meteorology owns:

Regional temperature

Humidity

Pressure

Cloud cover

Wind

Precipitation

Visibility

Weather condition

Atmospheric stability

---

# Simulation Rules

The atmosphere evolves every simulation cycle.

Rules include:

Weather changes gradually.

Rapid transitions require atmospheric causes.

Storms consume atmospheric instability.

Rain reduces humidity.

Sunlight increases evaporation.

Snow accumulates under sustained freezing conditions.

Every atmospheric change should have a traceable cause.

---

# Meteorological Events

Examples:

RainStarted

RainStopped

StormFormed

StormDissipated

FogDeveloped

HeatwaveStarted

ColdSnapEnded

SnowBegan

BlizzardEnded

LightningStrike

These Events are propagated through the Causality Engine.

---

# Relationships

Meteorology directly influences:

Climate

Hydrology

Ecology

Agriculture

Natural Resources

Citizens

Transportation

Construction

Trade

Economy

Religion

History

Weather rarely acts alone.

Its influence ripples throughout the simulation.

---

# Edge Cases

The system should support:

Extended droughts.

Multi-day storms.

Localized rainfall.

Mountain rain shadows.

Persistent fog.

Extreme winters.

Heatwaves.

Rare but catastrophic atmospheric events.

---

# Performance Considerations

Weather simulation should operate primarily at the regional level.

Fine-grained atmospheric calculations should only occur where additional fidelity provides meaningful gameplay.

Cached atmospheric states should minimize unnecessary recomputation.

---

# Future Extensions

Future versions may include:

Ocean-atmosphere interaction.

Jet streams.

Monsoon systems.

Cyclones.

Microclimates.

Air pollution.

Volcanic ash.

Climate change driven by civilization.

Magic or supernatural weather (optional world settings).

---

# Relationship to Civilization

Weather silently shapes history.

Farmers depend on rainfall.

Merchants fear storms.

Armies avoid harsh winters.

Religions interpret unusual skies.

Cities prepare for floods.

Kingdoms may prosper—or collapse—because of the atmosphere.

Weather is one of the oldest and most impartial forces in the world.

No civilization commands it.

Every civilization adapts to it.

---

# Closing Statement

The sky is never truly still.

Clouds gather.

Winds shift.

Rain nourishes the earth.

Storms test resilience.

Snow brings silence.

Sunlight restores life.

The atmosphere is not merely the space above the world.

It is a living system whose invisible movements shape every forest, every river, every harvest, every migration, and every civilization that will ever rise within The Garden.
