using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class Kingdom
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string Name { get; set; } = string.Empty;
    public GameEntityId CapitalSettlementId { get; set; }
    public string CapitalName { get; set; } = string.Empty;
    public List<GameEntityId> MemberSettlementIds { get; init; } = [];
    public GameEntityId LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    public string GovernmentType { get; set; } = "Informal";
    public long FoundedTick { get; set; }
    public long? DissolvedTick { get; set; }
    public bool IsActive { get; set; } = true;

    public int Population { get; set; }
    public int TerritoryRadius { get; set; } = 10;
    public double Stability { get; set; } = 75.0;
}
