# TG-DEV-005 — Settlements & Economy

**Document Version:** 1.0 (Living Document)
**Status:** Active
**Prerequisites:** TG-DEV-001, TG-DEV-002, TG-DEV-003, TG-DEV-004
**Estimated Duration:** Week 9–10

---

# Purpose

This phase marks the transition from individual survival to collective civilization.

Citizens should naturally discover that cooperation is more beneficial than isolation. Through their own decisions and environmental pressures, they begin constructing shelters, forming communities, producing resources, exchanging goods, and creating the first settlements.

No villages should appear automatically.

Every settlement must emerge because the simulation determined that establishing one was beneficial.

Likewise, every economy should emerge from actual production and consumption—not from artificial currency generation or scripted markets.

---

# Phase Objectives

## Primary Goal

Enable citizens to establish settlements and create a self-sustaining local economy.

## Success Criteria

✓ Citizens build homes

✓ Settlements emerge naturally

✓ Resource gathering operational

✓ Crafting operational

✓ Storage system operational

✓ Food production operational

✓ Local economy operational

✓ Trade between citizens operational

✓ Settlement dashboard operational

---

# Development Rules

Settlements are consequences—not features.

No "Create Village" button.

No scripted towns.

No predefined capitals.

No magically generated buildings.

Every structure must exist because:

* Resources were gathered.
* Labor was available.
* Citizens decided construction was worthwhile.

Likewise, every item in existence must have a traceable origin.

Nothing should appear from nowhere.

---

# Feature Branches

```text
feature/buildings

feature/construction

feature/resources

feature/inventory

feature/crafting

feature/agriculture

feature/settlements

feature/economy

feature/trading

feature/settlement-observatory
```

---

# Settlement System

## Task 1

Implement

* Settlement
* SettlementManager

Responsibilities

* Track members
* Track territory
* Track buildings
* Track food reserves
* Track resources
* Track production

A settlement begins with a single constructed shelter.

As additional structures are built, it grows organically.

---

# Territory System

Each settlement owns a territory.

Territory contains

* Buildings
* Resource nodes
* Roads (future-ready)
* Farms
* Storage

Territory expands gradually as the settlement grows.

Territories may not overlap.

---

# Building System

## Task 2

Implement building types.

Minimum buildings

* Shelter
* House
* Storage
* Farm
* Well
* Workshop

Every building requires

* Construction materials
* Labor
* Build time

Buildings progress through states

Planned

↓

Under Construction

↓

Completed

↓

Damaged (future)

↓

Ruined (future)

---

# Construction System

Citizens should

* Propose construction
* Gather materials
* Deliver materials
* Build over time
* Complete construction

Construction speed depends on

* Workers
* Weather
* Material availability

Construction pauses if resources are unavailable.

---

# Inventory System

## Task 3

Implement inventories.

Supported inventories

* Citizen
* Building
* Settlement

Items

* Wood
* Stone
* Clay
* Food
* Water

Future items should be easily added.

No inventory should have infinite capacity.

---

# Item System

Every item stores

* ID
* Name
* Category
* Quantity
* Weight
* Stack Limit

Items should support durability in future phases.

---

# Resource Gathering

Citizens gather

* Trees
* Stone
* Clay
* Water
* Wild plants

Gathering reduces environmental resources.

Environmental regeneration replenishes them naturally.

---

# Agriculture

## Task 4

Implement

* Farming
* Planting
* Harvesting

Food types

* Grain
* Vegetables
* Fruit

Growth depends on

* Climate
* Season
* Rainfall
* Soil fertility

Crop failure should be possible.

---

# Food System

Food

* Spoils over time
* Can be stored
* Can be transported
* Can be consumed

Future preservation systems may extend lifespan.

---

# Crafting

Implement basic production.

Examples

Wood

↓

Planks

Stone

↓

Stone Blocks

Grain

↓

Stored Food

Crafting requires

* Resources
* Labor
* Time

---

# Economy System

## Task 5

The economy is resource-driven.

Initially, no currency exists.

Citizens exchange actual goods.

Examples

Wood

↔

Food

Stone

↔

Water

Future monetary systems may emerge organically.

---

# Trade System

Citizens may

* Offer goods
* Request goods
* Exchange resources

Trading occurs only when beneficial.

No forced trading.

Trade history should be recorded.

---

# Production System

Track

* Produced
* Consumed
* Stored
* Lost
* Traded

This information will later support economic analysis.

---

# Settlement Growth

Growth depends on

* Available housing
* Food reserves
* Water access
* Resource availability

Settlements should never grow without supporting infrastructure.

---

# Settlement Events

Implement immutable events.

Examples

* SettlementFounded
* BuildingPlanned
* BuildingCompleted
* ResourceGathered
* FarmHarvested
* GoodsCrafted
* GoodsStored
* TradeCompleted
* SettlementExpanded

Events remain factual.

Business logic stays within simulation systems.

---

# Scheduler Integration

Hourly

* Construction
* Inventory updates

Daily

* Farming
* Production
* Consumption

Weekly

* Trade evaluation
* Resource allocation

Monthly

* Settlement statistics

---

# Observatory

Create the Settlements page.

Sections

## Settlement Summary

Cards

* Total Settlements
* Total Buildings
* Population
* Stored Food

---

## Settlement Table

Columns

* Name (temporary generated identifier)
* Population
* Buildings
* Food
* Resources
* Growth Status

Requirements

* Search
* Pagination
* Sticky Header
* Sorting

---

## Settlement Details

Display

* Buildings
* Members
* Food Storage
* Inventories
* Production
* Territory
* Construction Queue

Future tabs

* Politics
* Culture
* Diplomacy

---

## Economy Dashboard

Display

* Resource Production
* Resource Consumption
* Trade Volume
* Food Reserves

---

## Building View

Display

* Building Type
* Status
* Assigned Workers
* Progress
* Inventory

---

# World Map

Extend the map.

Display

* Settlement borders
* Buildings
* Farms
* Storage
* Wells

Support filtering

* Buildings
* Food
* Construction
* Resources

---

# UI Standards

Continue using shadcn/ui.

Primary components

* Card
* Table
* Tabs
* Sheet
* Badge
* Progress
* Tooltip
* Scroll Area

Construction progress should use Progress components.

Avoid game-like resource counters.

Prioritize operational dashboards.

---

# API Endpoints

Create

```text
GET /settlements

GET /settlements/{id}

GET /settlements/statistics

GET /economy

GET /economy/production

GET /economy/trade

GET /buildings

GET /inventories
```

Read-only.

Simulation remains authoritative.

---

# Logging

Record

* Settlement creation
* Construction
* Harvests
* Production
* Trade
* Resource shortages
* Inventory changes

---

# Performance Targets

Support

* 100 settlements

Average settlement update

< 5 ms

Construction updates

Stable

Inventory synchronization

Consistent

No duplicated items.

No negative inventories.

No phantom resources.

---

# Definition of Completion

Backend

✓ Settlement system operational

✓ Buildings complete

✓ Construction complete

✓ Inventory operational

✓ Agriculture operational

✓ Crafting operational

✓ Trade operational

✓ Economy operational

✓ Settlement events published

---

Frontend

✓ Settlements dashboard complete

✓ Economy dashboard complete

✓ Building inspection complete

✓ Inventory visualization complete

✓ Settlement map operational

---

Infrastructure

✓ CI passing

✓ Repository updated

✓ Documentation synchronized

---

# Bi-Weekly Progress Report (End of Week 10)

OpenCode shall provide:

## Completed Features

* Settlement System
* Buildings
* Construction
* Inventory
* Agriculture
* Crafting
* Economy
* Trade
* Settlement Observatory

## Metrics

* Total settlements
* Total buildings
* Daily food production
* Resource consumption
* Average construction duration
* Trade volume

## Technical Decisions

* Building placement logic
* Territory expansion
* Inventory implementation
* Trade evaluation algorithm

## Remaining Issues

Document any limitations that should be addressed before introducing social institutions, governance, and culture.

## Ready for Next Phase

Confirm readiness to begin **TG-DEV-006 — History & Story Systems.**.

By the completion of this phase, citizens should no longer simply survive—they should cooperate, construct, produce, and establish the first true communities, laying the foundation for civilization.
