# TG-DEV-007 — Observatory & Live World

**Document Version:** 1.0 (Living Document)
**Status:** Active
**Prerequisites:** TG-DEV-001 through TG-DEV-006
**Estimated Duration:** Week 13–14

---

# Purpose

This phase transforms **The Garden Observatory** from a static dashboard into a live observation platform.

By this point, the simulation engine is already capable of creating and maintaining a persistent world. The purpose of this phase is **not** to add new simulation mechanics, but to allow developers and future players to observe the world's evolution in real time.

The Observatory is not a control panel for the world.

It is a window into reality.

Every piece of information displayed by the Observatory originates from the Simulation Engine.

The UI never becomes the source of truth.

---

# Phase Objectives

## Primary Goal

Implement a real-time Observatory capable of monitoring, inspecting, and visualizing the simulation without influencing it.

## Success Criteria

✓ SignalR integration complete

✓ Live dashboard operational

✓ Live world map operational

✓ Real-time notifications operational

✓ Simulation controls operational

✓ Developer diagnostics available

✓ Performance monitoring operational

✓ Observatory fully responsive

---

# Development Rules

The Observatory observes.

The Engine decides.

Never place simulation logic inside the frontend.

Never duplicate world state inside React.

Every displayed value must originate from the API or SignalR.

The Observatory should continue functioning if refreshed at any time.

---

# Feature Branches

```text
feature/signalr

feature/live-dashboard

feature/world-map

feature/notifications

feature/diagnostics

feature/performance-monitor

feature/observatory-ui
```

---

# SignalR Integration

## Task 1

Implement SignalR communication.

Create hubs for

* Simulation Status
* Environment Updates
* Citizen Updates
* Settlement Updates
* History Updates

Clients subscribe to updates.

The server broadcasts only changed information.

Avoid transmitting entire world states.

---

# Live Dashboard

## Task 2

Create the Live Dashboard.

Sections

## Simulation

Display

* Running Status
* Simulation Speed
* Tick Count
* World Age

---

## Population

Display

* Population
* Births
* Deaths
* Average Age

---

## Environment

Display

* Current Season
* Temperature
* Weather
* Forest Coverage

---

## Settlements

Display

* Total Settlements
* Buildings
* Food Reserves
* Production

---

## History

Display

* Historical Records
* Latest Story
* Latest Major Event

All cards update automatically.

---

# World Map

## Task 3

Upgrade the world map.

Features

* Live updates
* Zoom
* Pan
* Tile inspection
* Settlement overlay
* Resource overlay
* Climate overlay
* Population overlay

Future overlays should be easy to register.

---

# Time Controls

Simulation controls

* Pause
* Resume
* Single Step
* Speed Selection

Simulation speed

* Paused
* 1×
* 2×
* 5×
* 10×
* 25×
* 50×
* 100×
* 250×
* 500×
* 1000×

Buttons must reflect current engine state.

---

# Live Notifications

## Task 4

Display significant events.

Examples

* Settlement Founded
* Citizen Died
* Flood Started
* Drought Ended
* Harvest Completed

Notifications disappear automatically.

Notifications are informational only.

---

# Activity Feed

Create a continuously updating activity feed.

Display

* Time
* Event
* Category

Support

* Filtering
* Search
* Auto-scroll toggle

Avoid overwhelming the interface.

---

# Developer Diagnostics

## Task 5

Create a Diagnostics page.

Display

* Tick Duration
* Memory Usage
* Event Queue Size
* Mutation Queue
* Scheduler Status
* Connected Clients
* API Latency

This page is intended for development.

Future production deployments may restrict access.

---

# Performance Monitor

Create charts for

* Tick Duration
* Simulation Throughput
* Event Count
* API Response Time
* SignalR Messages

Use Recharts.

Display rolling history.

---

# Global Search

Implement search across

* Citizens
* Settlements
* Buildings
* Historical Records
* Stories

Search should navigate directly to the selected entity.

---

# Layout Improvements

Improve responsiveness.

Desktop

Permanent sidebar.

Tablet

Collapsible sidebar.

Mobile

Drawer navigation.

Maintain consistent spacing across all pages.

---

# UI Standards

Continue using shadcn/ui.

Primary components

* Card
* Table
* Sheet
* Dialog
* Tabs
* Scroll Area
* Badge
* Tooltip
* Dropdown Menu
* Command (Global Search)
* Toast

Avoid dashboard clutter.

Every page should answer one question.

---

# Theme Support

Implement

* Light Theme
* Dark Theme

Theme selection should not alter information hierarchy.

Avoid excessive colors.

Use color only to communicate status.

Examples

Green

Healthy

Yellow

Warning

Red

Critical

Blue

Informational

---

# API Endpoints

Create

```text
GET /dashboard

GET /dashboard/summary

GET /dashboard/activity

GET /dashboard/performance

GET /diagnostics

GET /search
```

SignalR

```text
/simulationHub

/environmentHub

/citizenHub

/settlementHub

/historyHub
```

---

# Logging

Log

* Client connections
* SignalR events
* Dashboard requests
* Search requests
* Diagnostics access

---

# Performance Targets

Dashboard load

< 1 second

SignalR latency

< 100 ms

Search

< 300 ms

Map updates

Smooth under normal simulation load

Support

* Multiple simultaneous Observatory clients

---

# Definition of Completion

Backend

✓ SignalR operational

✓ Dashboard APIs complete

✓ Diagnostics complete

✓ Search operational

✓ Performance endpoints complete

---

Frontend

✓ Live Dashboard complete

✓ Live World Map complete

✓ Notifications operational

✓ Diagnostics dashboard complete

✓ Global Search operational

✓ Responsive layout complete

---

Infrastructure

✓ CI passing

✓ Repository updated

✓ SignalR deployment validated

✓ Documentation synchronized

---

# Bi-Weekly Progress Report (End of Week 14)

OpenCode shall provide:

## Completed Features

* SignalR Integration
* Live Dashboard
* Live World Map
* Notifications
* Diagnostics
* Global Search
* Performance Monitoring

## Metrics

* Dashboard load time
* SignalR latency
* Search performance
* Connected client count
* Average frontend render time

## Technical Decisions

* SignalR architecture
* State management strategy
* Caching approach
* Live update batching

## Remaining Issues

Document any remaining UI or synchronization limitations before implementing advanced civilization systems.

## Ready for Next Phase

Confirm readiness to begin **TG-DEV-008 — Advanced Civilization**.

By the completion of this phase, The Garden should be fully observable in real time, allowing developers and future players to watch a living world evolve continuously without becoming part of its decision-making process.
