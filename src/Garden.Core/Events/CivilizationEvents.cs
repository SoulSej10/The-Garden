using Garden.Core.Identifiers;

namespace Garden.Core.Events;

public abstract record CivilizationEvent : DomainEvent
{
    public string EntityName { get; init; } = string.Empty;
}

public record LeaderElectedEvent : CivilizationEvent
{
    public GameEntityId CitizenId { get; init; }
    public string CitizenName { get; init; } = string.Empty;
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public string PreviousLeaderName { get; init; } = string.Empty;
    public double ContributionScore { get; init; }
}

public record GovernmentFormedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public string GovernmentType { get; init; } = string.Empty;
    public string PreviousGovernmentType { get; init; } = string.Empty;
}

public record KingdomFoundedEvent : CivilizationEvent
{
    public GameEntityId KingdomId { get; init; }
    public string KingdomName { get; init; } = string.Empty;
    public GameEntityId CapitalSettlementId { get; init; }
    public string CapitalName { get; init; } = string.Empty;
    public GameEntityId LeaderId { get; init; }
    public string LeaderName { get; init; } = string.Empty;
    public int MemberCount { get; init; }
}

public record KingdomDissolvedEvent : CivilizationEvent
{
    public GameEntityId KingdomId { get; init; }
    public string KingdomName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public record DiplomaticRelationChangedEvent : CivilizationEvent
{
    public GameEntityId EntityAId { get; init; }
    public string EntityAName { get; init; } = string.Empty;
    public GameEntityId EntityBId { get; init; }
    public string EntityBName { get; init; } = string.Empty;
    public string PreviousRelation { get; init; } = string.Empty;
    public string NewRelation { get; init; } = string.Empty;
    public bool IsSettlementLevel { get; init; }
}

public record TradeRouteEstablishedEvent : CivilizationEvent
{
    public GameEntityId RouteId { get; init; }
    public GameEntityId FromSettlementId { get; init; }
    public string FromSettlementName { get; init; } = string.Empty;
    public GameEntityId ToSettlementId { get; init; }
    public string ToSettlementName { get; init; } = string.Empty;
    public string PrimaryGood { get; init; } = string.Empty;
    public double Distance { get; init; }
}

public record TradeRouteAbandonedEvent : CivilizationEvent
{
    public GameEntityId RouteId { get; init; }
    public string FromSettlementName { get; init; } = string.Empty;
    public string ToSettlementName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

public record MigrationStartedEvent : CivilizationEvent
{
    public GameEntityId CitizenId { get; init; }
    public string CitizenName { get; init; } = string.Empty;
    public GameEntityId? FromSettlementId { get; init; }
    public string FromSettlementName { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public int FromX { get; init; }
    public int FromY { get; init; }
}

public record MigrationCompletedEvent : CivilizationEvent
{
    public GameEntityId CitizenId { get; init; }
    public string CitizenName { get; init; } = string.Empty;
    public GameEntityId? ToSettlementId { get; init; }
    public string ToSettlementName { get; init; } = string.Empty;
    public int ToX { get; init; }
    public int ToY { get; init; }
}

public record TechnologyDiscoveredEvent : CivilizationEvent
{
    public string TechnologyId { get; init; } = string.Empty;
    public string TechnologyName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public GameEntityId? DiscoveredBySettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    // RFC-002: the citizen TechnologyService already picks as the discoverer
    // (settlement's highest-Intelligence member) - wasn't previously exposed
    // on the event, only stored on Technology.DiscoveredByCitizenId.
    public GameEntityId? DiscoveredByCitizenId { get; init; }
}

public record ReligionEstablishedEvent : CivilizationEvent
{
    public string ReligionId { get; init; } = string.Empty;
    public string ReligionName { get; init; } = string.Empty;
    public string CoreValue { get; init; } = string.Empty;
    public string OriginSettlementName { get; init; } = string.Empty;
    public int InitialFollowers { get; init; }
    // RFC-002: the citizen ReligionService already picks as the founder
    // (settlement's highest Compassion+Intelligence member) - wasn't
    // previously exposed on the event.
    public GameEntityId? FounderCitizenId { get; init; }
}

public record CulturalFestivalHeldEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public string FestivalName { get; init; } = string.Empty;
    public string Occasion { get; init; } = string.Empty;
    public int ParticipantCount { get; init; }
}

// RFC-003: first of TG-510_Language.md's 10 named events to get a real
// trigger - fired exactly once per settlement pair, when LanguageSystem's
// Divergence score crosses its threshold.
public record DialectFormedEvent : CivilizationEvent
{
    public GameEntityId SettlementAId { get; init; }
    public string SettlementAName { get; init; } = string.Empty;
    public GameEntityId SettlementBId { get; init; }
    public string SettlementBName { get; init; } = string.Empty;
    public double Divergence { get; init; }
}

// RFC-004: two of TG-550_Education.md's 10 named events - fired when
// EducationSystem pairs a mentor/student and when that apprenticeship ends.
public record ApprenticeshipStartedEvent : CivilizationEvent
{
    public GameEntityId MentorId { get; init; }
    public string MentorName { get; init; } = string.Empty;
    public GameEntityId StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
}

public record ApprenticeshipCompletedEvent : CivilizationEvent
{
    public GameEntityId MentorId { get; init; }
    public string MentorName { get; init; } = string.Empty;
    public GameEntityId StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
}

// RFC-005: two of TG-590_Law_Justice.md's 10 named events - fired when
// LawSystem resolves or fails to resolve an open dispute.
public record CaseResolvedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public GameEntityId CitizenAId { get; init; }
    public string CitizenAName { get; init; } = string.Empty;
    public GameEntityId CitizenBId { get; init; }
    public string CitizenBName { get; init; } = string.Empty;
}

public record JusticeFailureEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public GameEntityId CitizenAId { get; init; }
    public string CitizenAName { get; init; } = string.Empty;
    public GameEntityId CitizenBId { get; init; }
    public string CitizenBName { get; init; } = string.Empty;
}

// RFC-007: two of TG-620_Borders_Territorial_Dynamics.md's 10 named events -
// fired when TerritorySystem contracts a settlement's territory, or detects
// two settlements of comparable influence with overlapping territory.
public record BorderContractedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public int NewTerritorySize { get; init; }
}

public record BorderDisputeBeginsEvent : CivilizationEvent
{
    public GameEntityId SettlementAId { get; init; }
    public string SettlementAName { get; init; } = string.Empty;
    public GameEntityId SettlementBId { get; init; }
    public string SettlementBName { get; init; } = string.Empty;
}

// RFC-008: two of TG-240_Population_Ecology.md's 10 named events - fired
// when a settlement's population crosses its carrying capacity (Decline),
// or grows meaningfully while remaining comfortably under it (Boom).
public record PopulationDeclineEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public int Population { get; init; }
    public double CarryingCapacity { get; init; }
}

public record PopulationBoomEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public int Population { get; init; }
    public double CarryingCapacity { get; init; }
}

// RFC-009: four of TG-260_Disease_Health.md's 10 named events - fired by
// DiseaseSystem's overcrowding-driven infection mechanic.
public record OrganismInfectedEvent : CivilizationEvent
{
    public GameEntityId CitizenId { get; init; }
    public string CitizenName { get; init; } = string.Empty;
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
}

public record DiseaseRecoveredEvent : CivilizationEvent
{
    public GameEntityId CitizenId { get; init; }
    public string CitizenName { get; init; } = string.Empty;
}

public record EpidemicStartedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public double InfectionRate { get; init; }
}

public record EpidemicContainedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
}

// RFC-010: two of TG-250_Evolution_Adaptation.md's 10 named events -
// EvolutionSystem observes population-level attribute drift that
// ReproductionSystem's inheritance and CitizenSystem's differential
// survival already produce, rather than adding a new selection mechanic.
public record AdaptiveShiftObservedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public string AttributeName { get; init; } = string.Empty;
    public double Delta { get; init; }
}

public record EvolutionaryStagnationEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
}

// RFC-011: three of TG-220_Decomposers_Microbiology.md's 9 named events -
// DecomposerSystem converts organic matter from existing CitizenDied/
// ForestDeclined events into SoilHealth, which feeds back into
// AgricultureSystem's yield.
public record NutrientPulseOccurredEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public double SoilHealth { get; init; }
}

public record OrganicMatterAccumulatedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
}

public record WasteFullyDecomposedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
}

// RFC-012: two of TG-230_Fauna_Animal_Behavior.md's 10 named events -
// FaunaSystem tracks an aggregate wildlife population per settlement,
// driven by Forest-tile habitat within its territory. AnimalDied is
// reinterpreted at the aggregate level (a meaningful population die-off),
// not a single animal's death - documented in RFC-012.
public record SpeciesExpandedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public double WildlifePopulation { get; init; }
}

public record AnimalDiedEvent : CivilizationEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
    public double WildlifePopulation { get; init; }
}

// RFC-013: three of TG-640_Warfare_Military_Organization.md's 10 named
// events - WarfareSystem escalates an already-detected TerritorySystem
// border dispute (RFC-007) between settlements with a Hostile
// DiplomaticRelation into a real, resolvable war.
public record WarDeclaredEvent : CivilizationEvent
{
    public GameEntityId SettlementAId { get; init; }
    public string SettlementAName { get; init; } = string.Empty;
    public GameEntityId SettlementBId { get; init; }
    public string SettlementBName { get; init; } = string.Empty;
}

public record BattleFoughtEvent : CivilizationEvent
{
    public GameEntityId WinnerId { get; init; }
    public string WinnerName { get; init; } = string.Empty;
    public GameEntityId LoserId { get; init; }
    public string LoserName { get; init; } = string.Empty;
}

public record PeaceNegotiatedEvent : CivilizationEvent
{
    public GameEntityId SettlementAId { get; init; }
    public string SettlementAName { get; init; } = string.Empty;
    public GameEntityId SettlementBId { get; init; }
    public string SettlementBName { get; init; } = string.Empty;
}

// RFC-014: two of TG-660_Infrastructure.md's 10 named events -
// InfrastructureSystem grows/decays an existing TradeRoute's
// InfrastructureQuality based on sustained use or neglect (ADR-003).
public record RoadConstructedEvent : CivilizationEvent
{
    public GameEntityId RouteId { get; init; }
    public GameEntityId FromSettlementId { get; init; }
    public string FromSettlementName { get; init; } = string.Empty;
    public GameEntityId ToSettlementId { get; init; }
    public string ToSettlementName { get; init; } = string.Empty;
}

public record InfrastructureFailureEvent : CivilizationEvent
{
    public GameEntityId RouteId { get; init; }
    public GameEntityId FromSettlementId { get; init; }
    public string FromSettlementName { get; init; } = string.Empty;
    public GameEntityId ToSettlementId { get; init; }
    public string ToSettlementName { get; init; } = string.Empty;
}

// RFC-015 (specification/RFC/RFC-015-technology-independent-discovery.md):
// 1 of TG-670_Science_Technology.md's 10 named events - only observable
// now that technology discovery is per-settlement (ADR-004), since under
// the old shared-state model no two settlements could ever hold different
// technology sets to diverge in the first place.
public record TechnologicalDivergenceEvent : CivilizationEvent
{
    public GameEntityId SettlementAId { get; init; }
    public string SettlementAName { get; init; } = string.Empty;
    public GameEntityId SettlementBId { get; init; }
    public string SettlementBName { get; init; } = string.Empty;
    public int DivergentTechnologyCount { get; init; }
}

// RFC-016 (specification/RFC/RFC-016-legends-myth-formation.md): first
// increment of TG-STRY-040_Legends_Myths.md - a High-importance
// HistoricalRecord, once old enough, grows a distorted narrative alongside
// (never overwriting) the original record. LegendSystem owns this.
public record LegendFormedEvent : CivilizationEvent
{
    public GameEntityId LegendId { get; init; }
    public GameEntityId SourceRecordId { get; init; }
    public string Title { get; init; } = string.Empty;
}
