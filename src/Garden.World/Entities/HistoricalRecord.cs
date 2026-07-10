using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class HistoricalRecord
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public long Tick { get; init; }
    public int Year { get; init; }
    public int Day { get; init; }
    public string Season { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int LocationX { get; init; }
    public int LocationY { get; init; }
    public string LocationName { get; init; } = string.Empty;
    public List<string> ParticipantIds { get; init; } = [];
    public List<string> ParticipantNames { get; init; } = [];
    public string RelatedSettlementId { get; init; } = string.Empty;
    public List<string> RelatedEventIds { get; init; } = [];
    public double Severity { get; init; }
    public List<string> SourceEventIds { get; init; } = [];
    public string Importance { get; init; } = "Normal";
}

public static class HistoryCategories
{
    public const string Birth = "Birth";
    public const string Death = "Death";
    public const string Settlement = "Settlement";
    public const string Building = "Building";
    public const string Disaster = "Disaster";
    public const string Harvest = "Harvest";
    public const string Discovery = "Discovery";
    public const string Migration = "Migration";
    public const string Trade = "Trade";
    public const string War = "War";
    public const string Event = "Event";
    public const string Politics = "Politics";
    public const string Diplomacy = "Diplomacy";
    public const string Religion = "Religion";
    public const string Culture = "Culture";
    public const string Technology = "Technology";
}

public class Story
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public long Tick { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Narrative { get; init; } = string.Empty;
    public List<string> ParticipantIds { get; init; } = [];
    public List<string> ParticipantNames { get; init; } = [];
    public List<string> RelatedRecordIds { get; init; } = [];
    public long GeneratedAtTick { get; init; }
}

public class CitizenMemoryRecord
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string CitizenId { get; init; } = string.Empty;
    public long Tick { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public double EmotionalImpact { get; init; }
    public List<string> RelatedRecordIds { get; init; } = [];
}

public class FamilyMemoryRecord
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string FamilyId { get; init; } = string.Empty;
    public long Tick { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> AncestorIds { get; init; } = [];
    public List<string> RelatedRecordIds { get; init; } = [];
}

public class CollectiveMemoryRecord
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string SettlementId { get; init; } = string.Empty;
    public string SettlementName { get; init; } = string.Empty;
    public long Tick { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Importance { get; init; }
    public List<string> RelatedRecordIds { get; init; } = [];
}
