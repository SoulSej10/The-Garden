# The Garden Specification

# Volume V — Cognitive Sciences

# TG-380 — Relationships

**Document ID:** TG-380

**Volume:** V — Cognitive Sciences

**Scientific Discipline:** Social Psychology & Interpersonal Dynamics

**Status:** Living Document

**Priority:** FOUNDATIONAL

**Depends On**

* TG-300 Foundations of Intelligence
* TG-310 Perception
* TG-320 Memory
* TG-330 Emotion
* TG-340 Motivation & Needs
* TG-350 Decision Making
* TG-360 Learning
* TG-370 Personality

**Implementation note (2026-07-09):** a first increment now exists in code -
`Garden.World.Entities.Relationship` (Trust, Affection, Social Distance) and
`Garden.Engine/Systems/RelationshipSystem.cs`, created lazily on real
interaction events (currently: having a child together). Reciprocity
Balance and Conflict History (also named below) are not yet implemented -
no system in this codebase tracks favors or conflicts to derive them from.
See `DEVELOPMENT_PLAN.md` Week 3 Day 13 for scope notes.

---

# Purpose

Relationships define the persistent social connections between intelligent individuals.

Relationships are dynamic.

They strengthen, weaken, transform, and sometimes disappear over time.

Every relationship influences future perception, emotion, motivation, trust, cooperation, conflict, and decision-making.

Civilizations emerge from these countless interconnected relationships.

---

# Scientific Basis

Human societies are built upon repeated social interaction.

Trust, affection, cooperation, conflict, reciprocity, and shared history shape interpersonal relationships.

Relationships evolve through cumulative experience rather than isolated events.

The Garden models relationships as continuously changing systems influenced by cognition and lived experience.

---

# Design Philosophy

No individual exists alone.

Every life touches others.

Some connections last a moment.

Others last generations.

History is woven from relationships.

---

# Core Concepts

Relationships are defined by:

History.

Trust.

Familiarity.

Emotion.

Shared experiences.

Mutual expectations.

Social roles.

Relationships are always changing.

---

# Relationship Types

Individuals may simultaneously maintain many different relationships.

Examples include:

Parent.

Child.

Sibling.

Friend.

Companion.

Mentor.

Student.

Neighbor.

Leader.

Follower.

Merchant.

Customer.

Employer.

Worker.

Spouse.

Partner.

Ally.

Enemy.

Rival.

Stranger.

Relationship types may evolve over time.

---

# Relationship Strength

Every relationship possesses strength.

Strength develops through:

Time spent together.

Shared experiences.

Mutual assistance.

Communication.

Conflict resolution.

Repeated interaction.

Neglect gradually weakens relationships.

---

# Trust

Trust represents confidence in another individual.

Trust increases through:

Reliability.

Honesty.

Cooperation.

Protection.

Competence.

Trust decreases through:

Betrayal.

Broken promises.

Violence.

Deception.

Abandonment.

Trust strongly influences future cooperation.

---

# Affection

Relationships contain emotional significance.

Possible emotional states include:

Respect.

Love.

Admiration.

Sympathy.

Gratitude.

Jealousy.

Resentment.

Fear.

Hatred.

Indifference.

Multiple emotional states may coexist.

---

# Reciprocity

Relationships are influenced by reciprocal behavior.

Examples include:

Helping others.

Returning favors.

Sharing resources.

Teaching.

Protecting.

Ignoring.

Exploiting.

Individuals remember patterns of reciprocity over time.

---

# Reputation Transfer

Relationships influence how information spreads.

Trusted individuals have greater influence.

Recommendations from respected people carry more weight.

Warnings from unreliable individuals may be ignored.

Social networks shape collective understanding.

---

# Social Distance

Not every relationship is equally active.

Relationships naturally vary by:

Physical proximity.

Communication frequency.

Shared interests.

Family ties.

Occupation.

Community membership.

Distance influences relationship maintenance.

---

# Relationship Change

Relationships evolve continuously.

Possible transitions include:

Strangers becoming friends.

Friends becoming rivals.

Mentors becoming colleagues.

Enemies reconciling.

Partners separating.

Children becoming caregivers.

Relationships reflect changing life circumstances.

---

# State Variables

The Relationship System owns:

Relationship Network

Relationship Type

Relationship Strength

Trust Level

Affection Profile

Interaction Frequency

Conflict History

Shared Memories

Reciprocity Balance

Social Distance

---

# Simulation Rules

Relationships evolve continuously.

Rules include:

Interaction changes relationship strength.

Shared memories influence future interactions.

Trust develops gradually.

Conflict affects future cooperation.

Neglect weakens relationships.

Relationships influence cognition and decision-making.

No relationship remains permanently unchanged.

---

# Relationship Events

Examples include:

FriendshipFormed

TrustEarned

TrustBroken

AllianceCreated

RelationshipStrengthened

RelationshipWeakened

MentorshipEstablished

PartnershipCreated

ConflictResolved

RelationshipEnded

These events propagate through the Causality Engine.

---

# Relationships with Other Systems

Relationships directly influence:

Emotion.

Memory.

Motivation.

Communication.

Learning.

Leadership.

Politics.

Culture.

Family.

Civilization.

Social structures emerge from interpersonal relationships.

---

# Edge Cases

The simulation should support:

One-sided friendships.

Unrequited love.

Mutual distrust.

Long-distance relationships.

Generational mentorship.

Inherited rivalries.

Forgiveness.

Estrangement.

Reconciliation.

Unexpected friendship.

These situations enrich emergent storytelling.

---

# Performance Considerations

Relationships should prioritize frequently interacting individuals.

Dormant relationships may be compressed while preserving major historical events.

Relationship updates should occur primarily after meaningful interaction rather than every simulation tick.

---

# Future Extensions

Potential future additions include:

Marriage systems.

Dynasties.

Political alliances.

Diplomatic networks.

Guild membership.

Religious communities.

Secret societies.

Intercultural relationships.

International diplomacy.

---

# Relationship to Civilization

Civilizations are networks of relationships.

Families create villages.

Villages create communities.

Communities create institutions.

Institutions create civilizations.

Every alliance begins with trust.

Every revolution begins with broken trust.

Every dynasty begins with a family.

Every civilization is ultimately a web of relationships stretching across generations.

---

# Closing Statement

Two strangers met at a market.

Years later, they built a home together.

A teacher inspired a student.

The student later taught hundreds more.

Two rivals argued over a field.

Their grandchildren inherited the feud.

One act of kindness echoed across decades.

One betrayal reshaped an entire kingdom.

Lives do not unfold in isolation.

They intertwine.

In The Garden, every civilization grows from countless invisible threads connecting one life to another.
