using Garden.Core.Identifiers;

namespace Garden.Core.Events;

public abstract record SettlementEvent : DomainEvent
{
    public GameEntityId SettlementId { get; init; }
    public string SettlementName { get; init; } = string.Empty;
}

public record SettlementFoundedEvent : SettlementEvent
{
    public GameEntityId FounderId { get; init; }
    public string FounderName { get; init; } = string.Empty;
    public int TileX { get; init; }
    public int TileY { get; init; }
}

public record BuildingPlannedEvent : SettlementEvent
{
    public GameEntityId BuildingId { get; init; }
    public string BuildingType { get; init; } = string.Empty;
}

public record BuildingCompletedEvent : SettlementEvent
{
    public GameEntityId BuildingId { get; init; }
    public string BuildingType { get; init; } = string.Empty;
}

public record ResourceGatheredEvent : SettlementEvent
{
    public GameEntityId CitizenId { get; init; }
    public string ResourceType { get; init; } = string.Empty;
    public double Amount { get; init; }
}

public record FarmPlantedEvent : SettlementEvent
{
    public GameEntityId BuildingId { get; init; }
    public string CropType { get; init; } = string.Empty;
    public int TileX { get; init; }
    public int TileY { get; init; }
}

public record FarmHarvestedEvent : SettlementEvent
{
    public GameEntityId BuildingId { get; init; }
    public string CropType { get; init; } = string.Empty;
    public double Yield { get; init; }
}

public record GoodsCraftedEvent : SettlementEvent
{
    public string Product { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public record TradeCompletedEvent : SettlementEvent
{
    public GameEntityId FromCitizenId { get; init; }
    public GameEntityId ToCitizenId { get; init; }
    public string ItemType { get; init; } = string.Empty;
    public double Quantity { get; init; }
}

public record SettlementExpandedEvent : SettlementEvent
{
    public int NewTerritorySize { get; init; }
}
