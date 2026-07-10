# The Garden Specification

# Volume V — Cognitive Sciences

# TG-330 — Emotion

**Document ID:** TG-330

**Volume:** V — Cognitive Sciences

**Scientific Discipline:** Affective Science & Emotional Cognition

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Depends On**

* TG-300 Foundations of Intelligence
* TG-310 Perception
* TG-320 Memory

**Implementation note (2026-07-09):** a first increment now exists in code -
see `RFC/RFC-001-emotion-system.md` for scope/rationale and
`Garden.Engine/Systems/EmotionSystem.cs` for the implementation. Covers 6 of
the 15 emotions named below (Fear, Joy, Sadness, Trust, Curiosity,
Loneliness); the remaining 9 (Hope, Love, Grief, Pride, Shame, Disgust, Awe,
Calmness, and Anger) are still unimplemented. This document remains the
authoritative design reference; the RFC records what was deliberately
simplified or deferred and why.

---

# Purpose

Emotion defines how intelligent agents internally evaluate experiences and how those evaluations influence future cognition and behavior.

Emotions are not actions.

They are internal states that bias attention, perception, memory, motivation, relationships, and decision-making.

Emotion gives personal meaning to events and ensures that no two individuals respond identically to the same situation.

---

# Scientific Basis

Modern affective science suggests that emotions emerge from the interaction of physiology, cognition, memory, expectations, and environmental context.

Emotions are adaptive mechanisms that evolved to help organisms survive, cooperate, learn, and respond efficiently to changing circumstances.

The Garden models emotions as continuously evolving internal states rather than isolated events.

---

# Design Philosophy

Facts describe the world.

Emotions describe what the world means to an individual.

The same event may inspire hope in one citizen, fear in another, and indifference in a third.

Emotion personalizes reality.

---

# Core Concepts

Emotion influences:

Attention.

Memory.

Motivation.

Learning.

Relationships.

Decision-making.

Risk tolerance.

Behavior.

Every cognitive process is affected by emotional state.

---

# Emotional State

Every intelligent agent maintains an evolving emotional profile.

Multiple emotions may exist simultaneously.

Examples include:

Joy.

Fear.

Sadness.

Anger.

Curiosity.

Hope.

Love.

Grief.

Pride.

Shame.

Trust.

Disgust.

Loneliness.

Awe.

Calmness.

No emotion completely replaces another.

They coexist with varying intensity.

---

# Emotional Intensity

Each emotion possesses an intensity value.

Intensity changes according to:

Recent experiences.

Memory recall.

Relationships.

Physical needs.

Health.

Environmental conditions.

Personality.

High intensity increases behavioral influence.

---

# Emotional Duration

Not all emotions last equally long.

Examples include:

Startle lasting seconds.

Fear lasting hours.

Grief lasting years.

Love lasting decades.

Some emotional patterns become lifelong tendencies.

---

# Emotional Triggers

Emotions arise from perceived meaning rather than objective events.

Possible triggers include:

Success.

Failure.

Danger.

Safety.

Recognition.

Loss.

Discovery.

Isolation.

Conflict.

Achievement.

Unexpected change.

The same trigger may affect individuals differently.

---

# Emotional Regulation

Agents gradually regulate emotional intensity.

Regulation depends upon:

Personality.

Age.

Experience.

Relationships.

Culture.

Current circumstances.

Emotions naturally rise and decline over time.

---

# Emotional Contagion

Emotions may spread socially.

Examples include:

Panic during disaster.

Celebration after victory.

Collective mourning.

Religious inspiration.

Political unrest.

Crowd excitement.

Groups often amplify individual emotions.

---

# Emotional Memory

Emotion strengthens memory formation.

Highly emotional experiences are:

Encoded more rapidly.

Retained longer.

Recalled more frequently.

Influential in future decisions.

Emotion and memory continuously reinforce one another.

---

# Emotional Conflict

Agents frequently experience competing emotions.

Examples include:

Hope and fear.

Love and anger.

Pride and shame.

Curiosity and caution.

Decision-making emerges from balancing emotional influences.

---

# State Variables

The Emotion System owns:

Emotional Profile

Current Emotional State

Emotion Intensity

Emotional Stability

Dominant Emotion

Emotional Momentum

Emotional Resilience

Stress Level

Emotional Recovery Rate

---

# Simulation Rules

Emotions evolve continuously.

Rules include:

Perception triggers emotional evaluation.

Memory modifies emotional response.

Relationships influence emotional intensity.

Personality shapes emotional expression.

Emotion biases decision-making.

Emotions naturally decay unless reinforced.

---

# Emotion Events

Examples include:

FearTriggered

JoyExperienced

TrustStrengthened

GriefBegan

CuriosityAwakened

HopeRestored

AngerEscalated

EmotionRegulated

TraumaExperienced

EmotionalRecoveryCompleted

These events propagate through the Causality Engine.

---

# Relationships

Emotion directly supports:

Memory.

Motivation.

Decision Making.

Learning.

Relationships.

Communication.

Culture.

Identity.

Story Generation.

Emotion gives personal significance to every experience.

---

# Edge Cases

The simulation should support:

Conflicting emotions.

Emotional suppression.

Delayed grief.

Irrational fear.

Collective panic.

Emotional resilience.

Emotional numbness.

Traumatic stress.

Long-term optimism despite hardship.

These situations enrich individual behavior and emergent stories.

---

# Performance Considerations

Only meaningful emotional changes should persist.

Minor emotional fluctuations may be aggregated.

Long-term emotional tendencies should be stored separately from temporary emotional states.

This preserves both realism and computational efficiency.

---

# Future Extensions

Potential future additions include:

Mood systems.

Emotional intelligence.

Empathy.

Emotional disorders (optional realism settings).

Therapy and healing.

Meditation.

Emotional inheritance through parenting.

Group morale.

Leadership charisma.

---

# Relationship to Civilization

Civilizations are shaped as much by emotion as by reason.

Fear builds walls.

Hope inspires exploration.

Grief creates memorials.

Love forms families.

Pride raises monuments.

Anger starts revolutions.

Compassion creates hospitals.

The emotional lives of individuals become the emotional history of civilizations.

---

# Closing Statement

The rain fell upon every roof.

One child laughed and danced.

One farmer worried for the harvest.

One widow remembered a happier spring.

The storm was the same.

The hearts observing it were not.

The world presents events.

Emotion gives those events meaning.

In The Garden, history is not only determined by what happened.

It is determined by how countless individuals felt about what happened.
