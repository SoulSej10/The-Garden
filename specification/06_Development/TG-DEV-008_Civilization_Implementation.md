# TG-DEV-008 — Advanced Civilization

**Document Version:** 2.0 (Completed)
**Status:** Completed
**Prerequisites:** TG-DEV-001 through TG-DEV-007
**Actual Duration:** 1 session

---

# Purpose

This phase transforms settlements into civilizations.

Until now, citizens have survived, cooperated, built settlements, produced resources, and recorded history. Beginning with this phase, societies become increasingly complex through the natural emergence of leadership, governance, trade networks, diplomacy, technological advancement, cultural identity, religion, migration, and conflict.

Nothing in this phase should be scripted.

Kingdoms are not placed into the world.

Leaders are not manually assigned.

Wars are not triggered by random events.

Every civilization must emerge from the interactions of the systems implemented in previous phases.

---

# Phase Objectives

## Primary Goal

Implement the advanced societal systems that allow civilizations to emerge naturally from existing settlements.

## Success Criteria

✓ Leadership emerges naturally

✓ Governance operational

✓ Kingdom formation operational

✓ Diplomacy operational

✓ Trade routes operational

✓ Migration operational

✓ Technology progression operational

✓ Cultural identity operational

✓ Religion operational

✓ Civilization Observatory complete

---

# Development Rules

Civilizations emerge.

They are never spawned.

Every institution must originate from observable simulation events.

Examples

Settlement grows

↓

Resource surplus

↓

Population increases

↓

Leadership emerges

↓

Government forms

↓

Nearby settlements cooperate

↓

Kingdom forms

No feature may skip intermediate simulation steps.

---

# Feature Branches

```text id="2m7qke"
feature/leadership

feature/governments

feature/kingdoms

feature/diplomacy

feature/trade-routes

feature/migration

feature/culture

feature/religion

feature/technology

feature/civilization-observatory
```

---

# Leadership System

## Task 1

Implement natural leadership.

Leadership should emerge based on

* Experience
* Reputation
* Contributions
* Trust
* Community recognition

Leadership is earned.

It is never randomly assigned.

Leaders may lose influence naturally.

---

# Governance

Implement governance models.

Initial forms

* Informal Community
* Council
* Village Chief
* Elder Assembly

Future forms

* Monarchy
* Republic
* Federation

Government responsibilities

* Resource allocation
* Construction priorities
* Settlement planning
* External diplomacy

Governments should evolve over time.

---

# Kingdom Formation

## Task 2

Implement kingdom creation.

Requirements

Multiple settlements

↓

Shared interests

↓

Stable leadership

↓

Political alliance

↓

Kingdom established

Kingdoms maintain

* Territory
* Population
* Settlements
* Leadership
* Resources
* Relationships

Kingdoms may dissolve naturally.

---

# Diplomacy

Implement relationships between settlements and kingdoms.

Relationship states

* Neutral
* Friendly
* Allied
* Suspicious
* Hostile

Diplomatic actions

* Trade Agreement
* Resource Assistance
* Alliance
* Border Negotiation

Future

* Peace Treaties
* Military Cooperation

Relationships evolve through repeated interaction.

---

# Migration

## Task 3

Citizens may migrate because of

* Food shortages
* Employment opportunities
* Safety
* Family
* Resource availability

Migration must generate historical events.

Population movement should reshape the world naturally.

---

# Trade Routes

Implement trade routes.

Trade develops when

* Settlements produce surpluses
* Demand exists elsewhere
* Safe routes are available

Track

* Route length
* Goods transported
* Frequency
* Economic importance

Trade routes may disappear if no longer viable.

---

# Technology Progression

## Task 4

Technology is discovered through accumulated experience.

Categories

* Agriculture
* Construction
* Tools
* Transportation
* Resource Processing

Technology should unlock new possibilities rather than direct upgrades.

Examples

Improved tools

↓

Higher production

↓

Population growth

↓

Settlement expansion

---

# Culture

Implement cultural identity.

Culture develops through

* Geography
* History
* Climate
* Traditions
* Shared experiences

Examples

* Architecture
* Festivals
* Naming conventions
* Social customs

Culture changes gradually.

---

# Religion

Implement belief systems.

Religions emerge from

* Shared values
* Historical experiences
* Community traditions

Track

* Followers
* Places of worship (future)
* Cultural influence

Religion influences society.

It does not control it.

---

# Advanced Events

Implement immutable events.

Examples

* LeaderElected
* GovernmentFormed
* KingdomFounded
* AllianceSigned
* TradeRouteEstablished
* TechnologyDiscovered
* MigrationStarted
* MigrationCompleted
* CulturalFestivalHeld
* ReligionEstablished

Events remain factual.

Interpretation belongs to the Story Engine.

---

# Scheduler Integration

Daily

* Leadership evaluation

Weekly

* Trade routes
* Diplomacy

Monthly

* Migration
* Government decisions

Yearly

* Technology progression
* Cultural evolution
* Religious development

---

# Observatory

Create the Civilization page.

Sections

## Civilization Summary

Cards

* Kingdoms
* Governments
* Trade Routes
* Technologies

---

## Kingdom Table

Columns

* Kingdom
* Population
* Settlements
* Government
* Leader
* Stability

---

## Diplomacy

Display

* Alliances
* Trade Agreements
* Diplomatic Relations

Relationship visualization

Friendly

↓

Neutral

↓

Hostile

---

## Technology

Display

* Technologies Discovered
* Current Research
* Historical Progression

---

## Culture

Display

* Traditions
* Festivals
* Cultural Identity
* Regional Distribution

---

## Religion

Display

* Belief Systems
* Followers
* Geographic Spread

---

## Migration

Display

* Origin
* Destination
* Population Movement
* Reasons

---

## Trade Routes

Interactive visualization

Display

* Connected Settlements
* Resources
* Route Usage
* Economic Value

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
* Scroll Area
* Command
* Tooltip

Use diagrams and relationship visualizations where appropriate.

Avoid fantasy-inspired visuals.

Maintain the Observatory's scientific aesthetic.

---

# API Endpoints

Create

```text id="7v0xhb"
GET /kingdoms

GET /governments

GET /leaders

GET /diplomacy

GET /trade-routes

GET /migration

GET /culture

GET /religion

GET /technology
```

Read-only.

Simulation remains authoritative.

---

# Logging

Record

* Leadership changes
* Government formation
* Diplomatic agreements
* Migration
* Technology discoveries
* Cultural changes
* Religious growth

---

# Performance Targets

Support

* 250 settlements

* 20 kingdoms

* Thousands of diplomatic relationships

Civilization updates

< 50 ms

Trade calculations

Stable

Migration calculations

Stable

No duplicated kingdoms.

No invalid diplomatic relationships.

No circular trade routes.

---

# Definition of Completion

Backend

✓ Leadership operational

✓ Governments operational

✓ Kingdoms operational

✓ Diplomacy operational

✓ Migration operational

✓ Trade Routes operational

✓ Technology progression operational

✓ Culture operational

✓ Religion operational

---

Frontend

✓ Civilization dashboard complete

✓ Kingdom management view

✓ Diplomacy visualization

✓ Trade route visualization

✓ Technology progression viewer

✓ Culture and religion dashboards

---

Infrastructure

✓ CI passing

✓ Repository updated

✓ Documentation synchronized

---

# Final Phase Report

## Completed Features

* **Leadership** — Citizens earn leadership through contribution score (2×), reputation (1×), intelligence (0.5×), compassion (0.3×), diligence (0.4×), and age (0.1×). Evaluated daily.
* **Governments** — Progression: Informal Community (3+ pop) → Council (5+ pop) → Village Chief (10+ pop) → Elder Assembly (20+ pop). Evaluated monthly.
* **Kingdoms** — Formed from 2+ friendly settlements with leaders within 20 tiles of each other. Preserve stability, territory, member settlements. Auto-dissolve when active members drop below 2.
* **Diplomacy** — Automatic score-based relations between settlements within 30 tiles. Scores drift based on shared kingdom (+20%), distance, trade agreements, and historical interactions. Thresholds: 80+ Allied, 60+ Friendly, 40+ Neutral, 20+ Suspicious, <20 Hostile.
* **Migration** — Citizens leave due to food/water shortages (>70), poor health (<30), climate, or curiosity. Destinations scored by food availability, housing, leadership, building count, distance, and personality.
* **Trade Routes** — Established between settlements with surplus (20+) and deficit (<10) of any trade good. Auto-abandoned after 168 ticks of inactivity.
* **Technology** — 19 techs across 5 categories. Progress accumulates yearly based on settlement population. Agriculture techs boosted 1.5× by farms, construction 1.3× by existing buildings.
* **Culture** — 12 trait templates adopted by settlements based on economic, spiritual, social, and knowledge conditions. Festivals held at 2% monthly chance for settlements of 5+ pop.
* **Religion** — 5 belief system templates. Formed in 5+ pop settlements with cultural traits. Conversion chance based on cultural influence (0.01×). Spreads to nearby settlements within 20 tiles.

## Final Metrics

* Backend: 0 build errors
* Frontend: 0 build errors (tsc + vite)
* Unit tests: 17/17 passed
* 11 new API endpoints (all read-only)
* 9 new services, 1 new scheduled system
* 6 new entity types
* 11 new event types
* 1 new frontend page with 9 sub-tabs

## Technical Decisions

* **Leadership scoring** — Weighted sum rather than complex voting sim. Contribution×2 dominates; personality and age add nuance. Simple, deterministic, observable.
* **Government progression** — Population thresholds (3/5/10/20) trigger transitions upward. No regression mechanism yet (future: monarchy/republic/federation).
* **Diplomatic scoring** — Continuous 0–100 score mapped to discrete relation states. Score drifts via constant small adjustments from proximity, shared kingdom, trade agreements. Prevents oscillating relationships.
* **Technology progression** — Experience-point model where settlement population × time = research progress. Category-specific multipliers from relevant buildings. No research tree — any undiscovered tech can progress simultaneously.
* **Religion spread** — Contact-based propagation. Settlements with same religion act as hubs (range 20 tiles). Cultural influence grows with follower count, making larger religions spread faster.
* **Migration** — Pull-based (destination attractiveness) rather than push-based (expulsion). Destinations scored on multiple weighted factors. Migration completes immediately (instant relocation) rather than travel-time based, for simplicity.
* **Trade routes** — Bilateral surplus/deficit detection. Single primary good per route. Volume capped at 10 units per trip. Routes persist until 168 ticks idle.

## Known Limitations

* **No Monarchy/Republic/Federation** — Government progression stops at Elder Assembly. Future forms need culture-based evolution triggers.
* **No road network** — Trade routes and migration use Manhattan distance, not pathfinding. Actual road building deferred.
* **No military/conflict** — Diplomacy supports Hostile relations but no consequence (wars, raids, conquest). Spec lists military as a future item.
* **No culture-religion interaction** — Religion should influence cultural development and vice versa. Currently independent.
* **Migration is instant** — Citizens teleport to destination. Travel time with pathfinding deferred.
* **Kingdom leadership** — Kingdom leader is always the capital settlement's leader. No distinct kingdom leadership election.
* ~~**All events are backend-only** — Civilization events (LeaderElected, KingdomFounded, etc.) are published to IEventBus but not yet subscribed by HistorySystem for archival.~~ **Resolved 2026-07-09** (`DEVELOPMENT_PLAN.md` Week 1 Day 1) — `HistorySystem` now subscribes to all 12 civilization events; see `SPEC_INDEX.md` Change Log.

## Ready for Final Phase

Confirm readiness to begin **TG-DEV-009 — AI Integration & Production Readiness**.

At the completion of this phase, The Garden should function as a complete autonomous world simulation where civilizations arise naturally from the interactions of its foundational systems, with no scripted histories, predetermined rulers, or artificial progression.
