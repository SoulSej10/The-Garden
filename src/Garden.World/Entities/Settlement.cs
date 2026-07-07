using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class Settlement
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string Name { get; set; } = string.Empty;
    public int Population { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int TerritoryRadius { get; set; } = 5;
    public Inventory Storage { get; set; } = new() { MaxCapacity = 500 };
    public List<Building> Buildings { get; init; } = [];
    public List<GameEntityId> MemberIds { get; init; } = [];
    public long FoundedTick { get; set; }
    public GameEntityId? LeaderId { get; set; }
    public string LeaderName { get; set; } = string.Empty;
    public string GovernmentType { get; set; } = "Informal Community";
    public List<CulturalTrait> CulturalTraits { get; init; } = [];
    public GameEntityId? ReligionId { get; set; }
    public string ReligionName { get; set; } = string.Empty;
    public double TechnologyProgress { get; set; }
    public GameEntityId? KingdomId { get; set; }

    public int CompletedBuildings => Buildings.Count(b => b.Status == BuildingStatus.Completed);
    public int UnderConstructionBuildings => Buildings.Count(b => b.Status == BuildingStatus.UnderConstruction);

    public bool IsWithinTerritory(int x, int y) =>
        Math.Abs(x - TileX) <= TerritoryRadius && Math.Abs(y - TileY) <= TerritoryRadius;

    public bool HasAvailableHousing =>
        Buildings
            .Where(b => b.BuildingType is "Shelter" or "House" && b.Status == BuildingStatus.Completed)
            .Sum(b => BuildingTypes.GetMaxOccupants(b.BuildingType)) > MemberIds.Count;
}
