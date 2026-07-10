# TG-DEV-001 — Foundation & Project Setup

**Document Version:** 1.0 (Living Document)
**Status:** Active
**Prerequisites:** TG-000 through TG-008
**Estimated Duration:** Week 1–2

---

# Purpose

This document defines the implementation of The Garden's development foundation.

Unlike the Blueprint documents, this document contains actionable engineering tasks. OpenCode should follow this document sequentially.

The objective is to establish a production-ready foundation that every future simulation system will rely upon.

At the completion of this document:

* The repository is fully structured.
* The backend is operational.
* The frontend is operational.
* PostgreSQL is connected.
* The application can be launched locally.
* GitHub contains a clean project history.
* The Observatory can communicate with the API.
* The project is ready for simulation development.

No simulation logic should be implemented during this phase.

---

# Phase Objectives

## Primary Goal

Build a stable development foundation before writing any world simulation.

## Success Criteria

✓ Repository organized

✓ ASP.NET Core solution created

✓ React Observatory running

✓ PostgreSQL connected

✓ Entity Framework configured

✓ GitHub initialized

✓ Initial CI workflow functioning

✓ Local development environment documented

✓ Basic dashboard operational

✓ Health endpoint working

---

# Development Rules

This phase must respect all Constitutional Laws defined in TG-001.

Additional rules:

* Never skip foundational work to build simulation features.
* Prefer clean architecture over rapid implementation.
* Avoid placeholder systems that violate future architecture.
* Every project must compile before ending the work session.
* Every commit must leave the repository in a working state.

---

# Repository

Repository:

https://github.com/SoulSej10/The-Garden

Main branch:

```
main
```

Development branch:

```
develop
```

Feature branches:

```
feature/project-setup
feature/backend-foundation
feature/frontend-foundation
feature/database
feature/github-actions
feature/dashboard-layout
```

Merge Order

```
feature/*
↓

develop

↓

main
```

Never commit unfinished work directly to **main**.

---

# Commit Convention

Use descriptive commits.

Examples

```
feat(api): initialize ASP.NET solution

feat(frontend): create observatory shell

feat(database): configure PostgreSQL

feat(core): establish project structure

refactor(shared): move common abstractions

fix(api): correct dependency injection

docs: update development roadmap
```

Avoid commits like

```
update

fix

changes

stuff

final

done
```

---

# Solution Structure

```
src/

Garden.Api

Garden.Engine

Garden.Core

Garden.World

Garden.Infrastructure

Garden.Contracts

Garden.Shared

Garden.Story

Garden.Simulation

Garden.Tools

Garden.Observatory

tests/

UnitTests

IntegrationTests

SimulationTests

PerformanceTests

Playground
```

Do not alter this structure unless the Blueprint changes.

---

# Backend Tasks

## Task 1

Create ASP.NET Core Solution.

Deliverables

* Solution file
* Project references
* Dependency Injection
* Configuration
* Logging

---

## Task 2

Configure Clean Architecture.

Implement dependency boundaries.

Projects should reference only appropriate layers.

No circular references.

---

## Task 3

Configure Dependency Injection.

Register

* Services
* Repositories
* Rule Engine
* Event Bus (empty implementation)
* Time Service (empty implementation)

Only infrastructure should know concrete implementations.

---

## Task 4

Configuration

Create

```
appsettings.json

appsettings.Development.json
```

Support

* Connection Strings
* Logging
* Simulation configuration
* Future SignalR configuration

---

## Task 5

Logging

Integrate structured logging.

Minimum:

* Information
* Warning
* Error
* Critical

Log startup information.

---

# Database Tasks

Install PostgreSQL.

Configure Entity Framework Core.

Create initial migration.

Database should successfully migrate on startup.

Initial tables only.

No simulation tables yet.

---

# Frontend Tasks

Create React application using:

* Vite
* TypeScript

Install

* Tailwind CSS
* shadcn/ui
* React Router
* TanStack Query
* Recharts
* Axios
* Lucide Icons

Do not add unnecessary UI libraries.

---

# Frontend Folder Structure

```
src/

components/

layouts/

pages/

hooks/

services/

types/

contexts/

lib/

styles/

assets/
```

---

# UI Principles

The Observatory exists to observe.

The UI must never feel like a game HUD.

Design priorities:

1. Clarity

2. Readability

3. Information density

4. Calm appearance

5. Fast navigation

Avoid:

* flashy animations
* oversized cards
* excessive gradients
* game-inspired interfaces
* distracting colors

---

# Layout Standard

Desktop-first.

Maximum width:

```
1600px
```

Centered content.

Layout

```
-------------------------------------------------

Top Navigation

-------------------------------------------------

Sidebar | Main Content

Sidebar | Main Content

Sidebar | Main Content

-------------------------------------------------
```

---

# Navigation

Top Navigation

Contains

* Logo
* World Name
* Simulation Status
* Current Time
* User Menu

Sidebar

Contains

* Dashboard
* World
* Citizens
* Settlements
* Environment
* Economy
* History
* Stories
* Settings

No nested menus during Phase 1.

---

# Dashboard Layout

Page order

## Header

Contains

World Name

Simulation Status

Simulation Time

---

## Summary Cards

One responsive row.

Cards

* Population
* Settlements
* Temperature
* Active Events

Desktop

Four cards.

Tablet

Two rows.

Mobile

Single column.

---

## Main Area

```
------------------------------

Simulation Overview

------------------------------

World Statistics

------------------------------

Recent Activity

------------------------------
```

---

# Card Style

Use shadcn Card component.

Padding

```
24px
```

Rounded corners

```
rounded-xl
```

Shadow

Subtle only.

No glowing borders.

---

# Tables

Use shadcn Table.

Requirements

* Internal scrolling
* Sticky header
* Pagination
* Search
* Empty states

Avoid infinite page scrolling.

---

# Forms

Use

* Input
* Select
* Dialog
* Sheet
* Popover

from shadcn.

Validation messages appear below the field.

Never use browser alert dialogs.

---

# Icons

Use Lucide Icons only.

Maintain consistent icon sizing.

Default

```
18px
```

Navigation

```
20px
```

---

# API Tasks

Create endpoints

```
GET /health

GET /version
```

Health returns

* API Status
* Database Status
* Version

Version returns

* Application Version
* Build Date
* Environment

No simulation endpoints yet.

---

# GitHub Actions

Create workflow

On

```
push

pull_request
```

Run

Backend

* Restore
* Build

Frontend

* Install
* Build

Fail if either project fails.

---

# Local Development

Application should start with one command sequence.

Backend

```
dotnet run
```

Frontend

```
npm run dev
```

Database

```
PostgreSQL
```

Document required environment variables.

---

# End-of-Phase Deliverables

Backend

✓ Solution compiles

✓ API starts

✓ Health endpoint works

✓ Database connects

✓ Migration succeeds

---

Frontend

✓ Observatory loads

✓ Dashboard shell visible

✓ Navigation functional

✓ Responsive layout complete

---

Infrastructure

✓ GitHub repository organized

✓ CI operational

✓ Initial documentation updated

✓ Project builds without errors

---

# Bi-Weekly Progress Report (End of Week 2)

OpenCode shall prepare a concise implementation report including:

## Completed

* Finished tasks
* Features implemented
* Repository structure finalized
* UI components completed

## Issues Encountered

* Build failures
* Dependency conflicts
* Package incompatibilities
* Technical blockers

## Decisions Made

* Architecture adjustments (if any)
* Package replacements
* Configuration changes

## Repository Status

* Current active branch
* Pull Requests merged
* Remaining feature branches

## Ready for Next Phase

Confirm whether the project is prepared to begin **TG-DEV-002 — Time System & Simulation Core**.

No simulation logic should exist before this confirmation.
