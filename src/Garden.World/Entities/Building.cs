using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class Building
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string BuildingType { get; set; } = string.Empty;
    public int TileX { get; set; }
    public int TileY { get; set; }
    public BuildingStatus Status { get; set; } = BuildingStatus.Planned;
    public int BuildProgress { get; set; }
    public int BuildTimeRequired { get; set; } = 48;
    public GameEntityId? AssignedWorkerId { get; set; }
    public Inventory Storage { get; set; } = new();
}

public enum BuildingStatus
{
    Planned,
    UnderConstruction,
    Completed,
    Damaged,
    Ruined
}

public static class BuildingTypes
{
    public const string Shelter = "Shelter";
    public const string House = "House";
    public const string Storage = "Storage";
    public const string Farm = "Farm";
    public const string Well = "Well";
    public const string Workshop = "Workshop";

    public static string[] All => [Shelter, House, Storage, Farm, Well, Workshop];

    public static int GetBuildTime(string type) => type switch
    {
        Shelter => 24,
        House => 48,
        Storage => 36,
        Farm => 30,
        Well => 24,
        Workshop => 60,
        _ => 48
    };

    public static (string Material, int Amount)[] GetCost(string type) => type switch
    {
        Shelter => [("Wood", 10), ("Stone", 5)],
        House => [("Wood", 25), ("Stone", 15), ("Clay", 10)],
        Storage => [("Wood", 20), ("Stone", 10)],
        Farm => [("Wood", 15)],
        Well => [("Stone", 20), ("Clay", 10)],
        Workshop => [("Wood", 30), ("Stone", 20), ("Clay", 15)],
        _ => [("Wood", 10)]
    };

    public static int GetMaxOccupants(string type) => type switch
    {
        Shelter => 2,
        House => 4,
        _ => 0
    };
}
