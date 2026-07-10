# TG-DEV-006 — History & Story Systems

**Document Version:** 1.0 (Living Document)
**Status:** Active
**Prerequisites:** TG-DEV-001 through TG-DEV-005, TG-008
**Estimated Duration:** Week 11–12

---

# Purpose

This phase implements one of The Garden's defining features: **History**.

A simulation without memory is merely a sequence of events. A simulation with history becomes a living world.

The objective of this phase is to permanently preserve meaningful events, organize them into a searchable historical archive, and transform historical facts into readable narratives without altering the underlying truth.

The Story System does **not** invent events.

It only interprets events that have already occurred.

History remains objective.

Stories remain subjective.

---

# Phase Objectives

## Primary Goal

Create an immutable historical archive and narrative engine that records, organizes, and presents the world's history.

## Success Criteria

✓ Historical Archive operational

✓ Event significance evaluation complete

✓ Timeline system operational

✓ Citizen memories operational

✓ Family memories operational

✓ Collective memories operational

✓ Story Engine operational

✓ History fully observable through the Observatory

---

# Development Rules

History is permanent.

Nothing in the simulation may delete or rewrite history.

Corrections must always generate new historical records.

The Story Engine must never alter historical facts.

Facts are immutable.

Narratives are interpretations.

Every historical record must be traceable back to its originating simulation events.

---

# Feature Branches

```text
feature/history

feature/archive

feature-memory

feature/timeline

feature/story-engine

feature-history-api

feature-history-observatory
```

---

# Historical Archive

## Task 1

Implement

* HistoricalArchive
* HistoricalRecord
* HistoryManager

Responsibilities

* Store historical records
* Preserve chronological order
* Support historical search
* Support future replay systems

History is append-only.

Records are never modified.

---

# Historical Significance

## Task 2

Not every event deserves to become history.

Implement a significance evaluator.

Events with low significance remain operational logs.

Examples

Do NOT archive

* Citizen walked
* Citizen ate
* Resource regenerated

Archive

* Citizen born
* Citizen died
* Settlement founded
* Major flood
* Long drought
* First harvest
* First trade
* Settlement abandoned

The evaluator should be configurable.

Future systems may introduce additional historical thresholds.

---

# Historical Record

Each historical record contains

* Unique ID
* Timestamp
* Simulation Time
* Event Type
* Title
* Description
* Location
* Participants
* Related Events
* Severity
* Source Event IDs

History should support future localization.

---

# Timeline System

## Task 3

Implement

* Timeline
* TimelineEntry
* TimelineSearch

Support

* Chronological browsing
* Filtering
* Searching
* Date range queries
* Event type queries

Timeline order is always chronological.

---

# Citizen Memory

## Task 4

Implement personal memories.

Citizens remember

* Births
* Deaths
* Friendships (future)
* Family events
* Significant discoveries
* Major disasters

Memories contain

* Confidence
* Emotional impact
* Timestamp

Future phases may allow memories to fade or become inaccurate.

---

# Family Memory

Implement inherited memories.

Families preserve

* Ancestors
* Important events
* Traditions (future)
* Historic achievements

Family memories persist beyond an individual's lifetime.

---

# Collective Memory

Implement settlement memories.

Communities remember

* Settlement founding
* Disasters
* Exceptional harvests
* Heroic acts (emergent)
* Major construction

Collective memory provides the foundation for future culture.

---

# Story Engine

## Task 5

Implement

* StoryEngine
* StoryGenerator
* NarrativeBuilder

Responsibilities

Translate historical records into readable narratives.

Example

Historical Fact

> Settlement Alpha founded on Year 3, Day 12.

Narrative

> "After weeks of wandering, a small group of pioneers established the settlement that would later become Alpha."

The narrative must never contradict historical facts.

---

# Story Templates

Implement template-based narration.

Categories

* Birth
* Death
* Settlement
* Disaster
* Harvest
* Discovery
* Migration
* Trade

Templates should support future AI enhancement without replacing deterministic generation.

---

# Story Metadata

Each story contains

* Title
* Summary
* Narrative
* Participants
* Timeline references
* Related historical records

Stories reference history.

History never references stories.

---

# Scheduler Integration

Daily

* Evaluate significance

Weekly

* Build timeline indexes

Monthly

* Generate historical summaries

Yearly

* Generate annual world chronicle

---

# Observatory

Create the History page.

Sections

## Historical Summary

Cards

* Total Historical Records
* World Age
* Recorded Births
* Recorded Deaths

---

## Timeline

Display

* Date
* Event
* Location
* Participants

Requirements

* Search
* Pagination
* Sticky Header
* Filtering

---

## Story Feed

Display generated narratives.

Cards

* Story Title
* Summary
* Date
* Participants

Selecting a story opens the full narrative.

---

## Citizen Memory

Selecting a citizen displays

* Personal memories
* Historical participation
* Timeline references

Future phases will add emotional interpretation.

---

## Historical Search

Support searching by

* Citizen
* Settlement
* Date
* Event Type
* Keyword

---

# UI Standards

Continue using shadcn/ui.

Primary components

* Card
* Table
* Tabs
* Sheet
* Scroll Area
* Badge
* Timeline-style vertical layout

History should feel like reading an archive rather than browsing game logs.

Use typography and spacing to emphasize chronology.

---

# API Endpoints

Create

```text
GET /history

GET /history/{id}

GET /history/timeline

GET /history/search

GET /stories

GET /stories/{id}

GET /citizens/{id}/memories
```

All endpoints are read-only.

---

# Logging

Record

* Historical record creation
* Story generation
* Timeline indexing
* Archive performance

Do not log narrative generation as simulation events.

Narrative generation is a presentation layer.

---

# Performance Targets

Historical archive

Support

* 10,000+ historical records

Timeline search

< 200 ms

Story generation

< 100 ms per story

Archive growth must not significantly impact simulation performance.

---

# Definition of Completion

Backend

✓ Historical Archive complete

✓ Timeline operational

✓ Significance evaluator complete

✓ Citizen memory operational

✓ Family memory operational

✓ Collective memory operational

✓ Story Engine complete

---

Frontend

✓ History dashboard complete

✓ Timeline viewer complete

✓ Story viewer complete

✓ Memory viewer complete

✓ Historical search operational

---

Infrastructure

✓ CI passing

✓ Repository updated

✓ Documentation synchronized

---

# Bi-Weekly Progress Report (End of Week 12)

OpenCode shall provide:

## Completed Features

* Historical Archive
* Timeline
* Story Engine
* Citizen Memory
* Family Memory
* Collective Memory
* History Observatory

## Metrics

* Total historical records
* Timeline search performance
* Story generation performance
* Average archive growth per simulation year

## Technical Decisions

* Archive storage strategy
* Significance evaluation logic
* Story template architecture
* Timeline indexing implementation

## Remaining Issues

Document any limitations that should be addressed before introducing live synchronization and advanced observability.

## Ready for Next Phase

Confirm readiness to begin **TG-DEV-007 — Observatory & Live World**.

By the completion of this phase, The Garden should no longer simply simulate a world—it should remember it, preserve it, and tell its story without ever changing the truth.
