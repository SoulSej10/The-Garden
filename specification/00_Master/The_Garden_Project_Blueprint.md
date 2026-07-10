# The Garden

## A World That Never Sleeps

> **Vision:** Build a living world simulator where the world continues
> to evolve through autonomous rules rather than scripted events.

## Core Philosophy

The Garden is not a game first. It is a **simulation engine**.

Everything happens because of rules:

-   Time advances.
-   Weather changes.
-   Resources grow or decline.
-   People make decisions.
-   Villages emerge.
-   History accumulates.

The player observes, experiments, and influences---but does not manually
script every event.

## Project Goals

-   Create a persistent simulation engine.
-   Support emergent behavior.
-   Keep the engine independent from any UI.
-   Make the simulation observable through dashboards, stories,
    timelines, and inspectors.
-   Build locally first. Cloud hosting is optional later.

------------------------------------------------------------------------

# Architecture

                     The Garden
    +----------------------------------------+
    |        World Observatory (React)       |
    +----------------------------------------+
                    REST API
    +----------------------------------------+
    |      ASP.NET Core Simulation API       |
    +----------------------------------------+
                    Engine
    +----------------------------------------+
    | Time | NPC | Economy | Weather | Rules |
    +----------------------------------------+
                    Storage
    +----------------------------------------+
    | PostgreSQL + Entity Framework Core     |
    +----------------------------------------+

## Technology Stack

### Backend

-   ASP.NET Core
-   Entity Framework Core
-   Hosted Background Service
-   xUnit

### Frontend

-   React
-   Vite
-   TypeScript
-   Tailwind CSS
-   shadcn/ui
-   Recharts
-   Vitest
-   Playwright

### Database

-   PostgreSQL

### Future

-   SignalR
-   Docker Compose
-   AI-assisted storytelling

------------------------------------------------------------------------

# Guiding Principles

1.  The engine owns all world logic.
2.  The UI only observes and issues high-level commands.
3.  Every meaningful change becomes a historical event.
4.  Small rules should combine into surprising outcomes.
5.  Prefer simulation over scripting.
6.  Prefer deterministic systems over AI where possible; use AI later
    for narration.

------------------------------------------------------------------------

# Simulation Layers

## Time

-   Tick
-   Hour
-   Day
-   Month
-   Year

Simulation speed should be configurable.

## Environment

-   Terrain
-   Forests
-   Rivers
-   Weather
-   Seasons

## Resources

-   Food
-   Wood
-   Stone
-   Water

## Citizens

Attributes: - Name - Age - Occupation - Home - Hunger - Energy -
Health - Personality (later) - Memories (later)

Daily loop: Wake → Eat → Work → Socialize → Sleep

## Relationships

-   Parent
-   Child
-   Partner
-   Friend
-   Rival

## Settlements

Villages form naturally when conditions are met.

## Economy

Supply, demand, production, storage, trade.

## History

Everything important becomes an immutable event: - Birth - Death -
Marriage - Harvest - Fire - Flood - Migration - Discovery

------------------------------------------------------------------------

# World Observatory

## Dashboard

-   Current year
-   Season
-   Population
-   Births
-   Deaths
-   Weather
-   Villages
-   Resources

## Live Event Feed

Chronological developer-oriented event stream.

## Story Feed

Human-friendly narrative generated from simulation events.

## Citizen Inspector

Inspect an individual: - Stats - Relationships - Timeline - Memories -
Current activity

## Timeline

Scrollable world history.

## Charts

Population, food, resources, weather, births/deaths.

------------------------------------------------------------------------

# Suggested Milestones

## Phase 1 - Foundation

-   Project setup
-   Time system
-   Tick engine
-   PostgreSQL
-   Basic dashboard
-   Event log

## Phase 2 - Environment

-   Weather
-   Seasons
-   Terrain
-   Resource regeneration

## Phase 3 - Citizens

-   Spawn citizens
-   Daily routines
-   Hunger
-   Energy
-   Jobs

## Phase 4 - Relationships

-   Families
-   Friendships
-   Births
-   Deaths

## Phase 5 - Settlements

-   Houses
-   Villages
-   Production

## Phase 6 - History

-   Immutable event log
-   Timeline
-   Search

## Phase 7 - Story Engine

Translate events into readable stories.

## Phase 8 - Live Simulation

SignalR updates. Simulation speed controls. Pause/resume.

## Phase 9 - Advanced Systems

-   Trade
-   Politics
-   Kingdoms
-   Culture
-   Religion
-   Language
-   Migration

## Phase 10 - AI Layer

Use LLMs only to narrate or summarize existing simulation facts.

------------------------------------------------------------------------

# Future Vision

Potential viewers: - Web - Desktop - Roblox - CLI - Mobile

All consume the same engine.

------------------------------------------------------------------------

# Long-Term Identity

**The Garden is not a game that contains a world.**

**It is a world that can be experienced through many different
interfaces.**

Its defining promise is:

> **A world that never sleeps.**
