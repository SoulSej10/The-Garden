# The Garden Specification

# Volume IV — Biological Sciences

# TG-210 — Flora

**Document ID:** TG-210

**Volume:** IV — Biological Sciences

**Scientific Discipline:** Botany

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Depends On**

* TG-160 Biomes
* TG-170 Natural Resources
* TG-180 Biosphere
* TG-200 Biology Foundations

---

# Purpose

Flora defines every plant lifeform within The Garden.

Plants transform sunlight into biological energy, stabilize ecosystems, shape landscapes, regulate climate, produce resources, and sustain nearly every food web.

Plants are the foundation upon which almost all terrestrial life depends.

---

# Scientific Basis

Plants are living organisms that acquire energy primarily through photosynthesis.

They grow, reproduce, compete for resources, respond to environmental conditions, and eventually die.

The Garden models plant life at ecological scales while allowing exceptional individual organisms—such as ancient trees—to become meaningful parts of the world's history.

---

# Design Philosophy

Plants are builders.

Animals move through the world.

Plants build it.

A forest is thousands of individual organisms quietly cooperating and competing across centuries.

Civilizations may construct cities in decades.

Forests reclaim them in generations.

Plants are patient.

---

# Core Concepts

Plants are living organisms.

They:

Capture energy.

Grow continuously.

Compete for sunlight.

Require water.

Consume nutrients.

Reproduce.

Age.

Die.

Their success depends entirely upon their surrounding environment.

---

# Plant Categories

The initial simulation supports:

Trees

Shrubs

Grasses

Flowers

Ferns

Mosses

Aquatic Plants

Agricultural Crops

Climbing Plants

Ground Cover

Future updates may introduce additional botanical diversity.

---

# Plant Structure

Every plant possesses:

Species

Age

Life Stage

Size

Health

Energy

Water Status

Nutrient Status

Growth Rate

Reproductive State

Root Depth

Canopy Size

Environmental Fitness

Not every variable must be simulated at full fidelity for every individual plant.

Aggregation is acceptable for common vegetation.

---

# Photosynthesis

Plants convert sunlight into stored biological energy.

Photosynthetic productivity depends upon:

Sunlight.

Temperature.

Water availability.

Soil fertility.

Season.

Plant health.

Reduced productivity slows growth and reproduction.

---

# Growth

Plants continuously develop throughout life.

Growth influences:

Height.

Canopy coverage.

Root expansion.

Biomass.

Resource production.

Habitat creation.

Different species exhibit different growth strategies.

---

# Competition

Plants compete for:

Sunlight.

Water.

Nutrients.

Physical space.

Competition naturally shapes forest structure without scripted placement.

---

# Reproduction

Plants reproduce through methods appropriate to their species.

Examples include:

Seeds.

Spores.

Vegetative propagation.

Seasonal flowering.

Reproduction depends upon environmental suitability and mature plant health.

---

# Succession

Plant communities naturally change over time.

Typical progression:

```text id="g0m1vb"
Bare Ground
      ↓
Grasses
      ↓
Shrubs
      ↓
Young Forest
      ↓
Mature Forest
      ↓
Old-Growth Forest
```

Disturbances restart succession without permanently destroying ecological potential.

---

# Old-Growth Forests

Some forests survive for centuries.

Characteristics include:

Large ancient trees.

High biodiversity.

Complex canopy layers.

Exceptional resource quality.

Rare habitats.

Old-growth ecosystems should be uncommon and historically significant.

---

# Seasonal Behavior

Plants respond to seasons.

Possible behaviors include:

Flowering.

Leaf emergence.

Leaf fall.

Dormancy.

Fruit production.

Seed dispersal.

Growth acceleration.

Seasonality creates visual and ecological diversity.

---

# Environmental Stress

Plant health declines under:

Drought.

Flooding.

Poor soil.

Extreme temperatures.

Disease.

Fire.

Excessive harvesting.

Stress accumulates gradually.

Recovery is possible if environmental conditions improve.

---

# State Variables

The Flora System owns:

Plant Populations

Species Distribution

Vegetation Density

Canopy Coverage

Plant Health

Growth Stage

Reproductive Activity

Succession Stage

Primary Productivity

Habitat Complexity

---

# Simulation Rules

Plant life evolves continuously.

Rules include:

Plants require suitable environmental conditions.

Growth consumes resources.

Healthy plants reproduce.

Dead plants contribute nutrients.

Disturbance alters succession.

Natural recovery begins automatically where conditions permit.

Vegetation continuously reshapes ecosystems.

---

# Botanical Events

Examples include:

TreeGerminated

ForestExpanded

FloweringSeasonStarted

FruitProduced

OldGrowthEstablished

ForestRegenerated

VegetationDeclined

PlantPopulationCollapsed

AncientTreeDied

EcologicalSuccessionAdvanced

These events propagate through the Causality Engine.

---

# Relationships

Flora directly influences:

Biosphere

Fauna

Agriculture

Climate

Hydrology

Soil Fertility

Wildfire

Natural Resources

Settlement Development

Civilization

Story Generation

Every terrestrial ecosystem depends upon plant life.

---

# Edge Cases

The simulation should support:

Ancient solitary trees.

Floating vegetation.

Alpine plant communities.

Seasonal grasslands.

Mangrove forests.

Bamboo forests.

Sacred groves.

Naturally regenerating abandoned settlements.

Rare botanical environments create memorable histories.

---

# Performance Considerations

Most vegetation should be simulated at ecosystem scale.

Only exceptional plants—such as ancient landmark trees or culturally significant specimens—require persistent individual identities.

Regional aggregation should remain the default approach.

---

# Future Extensions

Potential future additions include:

Plant genetics.

Hybridization.

Selective cultivation.

Medicinal botany.

Forest management.

Plant diseases.

Invasive plant species.

Sacred trees.

Domesticated orchards.

Ecological restoration.

---

# Relationship to Civilization

Civilizations never truly conquer forests.

They negotiate with them.

Wood becomes homes.

Fruit becomes food.

Shade becomes refuge.

Medicinal plants become healing.

Fields replace forests.

Eventually abandoned fields become forests again.

Plant life quietly records every chapter of civilization through the landscapes it transforms.

---

# Closing Statement

The first forest asked for no audience.

It grew because the world allowed it.

One seed became many.

One sapling became a giant.

Generations of leaves fell, fed the soil, and gave life to those yet to come.

Kingdoms would one day clear forests to build their cities.

Centuries later, roots would crack their stone walls, branches would cover forgotten roads, and the forest would remember what humanity had forgotten.

Plants are not decorations within The Garden.

They are the world's oldest builders, patiently shaping every generation that follows.
