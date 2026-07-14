using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class TradeRoute
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public GameEntityId FromSettlementId { get; init; }
    public string FromSettlementName { get; set; } = string.Empty;
    public GameEntityId ToSettlementId { get; init; }
    public string ToSettlementName { get; set; } = string.Empty;
    public string PrimaryGood { get; set; } = string.Empty;
    public double TotalVolumeTransported { get; set; }
    public int TripCount { get; set; }
    public double Distance { get; set; }
    public double EconomicValue { get; set; } = 1.0;
    public bool IsActive { get; set; } = true;
    public long EstablishedTick { get; set; }
    public long LastTripTick { get; set; }
    // RFC-014 (specification/RFC/RFC-014-infrastructure-route-quality.md):
    // TG-660's network-connectivity concept, applied to this already-
    // existing inter-settlement connection rather than a new Road/Network
    // entity (ADR-003) - no formula given in the spec. InfrastructureSystem
    // owns all updates.
    public double InfrastructureQuality { get; set; }
}
