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
