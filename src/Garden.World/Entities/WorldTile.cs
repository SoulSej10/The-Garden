using Garden.Core.World;

namespace Garden.World.Entities;

public class WorldTile
{
    public int X { get; init; }
    public int Y { get; init; }
    public TerrainType Terrain { get; set; }
    public BiomeType Biome { get; set; }
    public ClimateZone Climate { get; set; }
    public double Elevation { get; set; }
    public double Moisture { get; set; }
    public double Temperature { get; set; }
    public bool IsRiver { get; set; }
    public bool IsLake { get; set; }
    // Geological generator outputs (WorldGenerator) - see Garden.Core.World.PlateBoundaryType
    // for BoundaryType's meaning. Relief is post-erosion local elevation variance (drives
    // Badlands/Canyon/Fjord classification); FlowAccumulation is the D8 upstream-drainage
    // count (log-normalized 0..1), used both for river tiering and as a Flood-event input.
    public PlateBoundaryType BoundaryType { get; set; }
    public bool IsVolcanic { get; set; }
    public double Relief { get; set; }
    public double FlowAccumulation { get; set; }
    // Rebalancing audit finding 2: tracks consecutive weeks this tile's
    // Trees deposit has sat near-depleted from harvesting - EcologySystem
    // uses sustained depletion as a chance to revert Forest back to
    // Grassland, closing the previously-missing harvesting-pressure ->
    // forest-density feedback loop (spread/conversion used to be completely
    // unaffected by how heavily a forest was actually being logged).
    public int HarvestDepletedWeeks { get; set; }
    public OccupancyState Occupancy { get; set; } = new();
    public List<ResourceDeposit> Resources { get; init; } = [];
}

public class OccupancyState
{
    public bool IsOccupied { get; set; }
    public string? OccupiedBy { get; set; }
    public string? StructureType { get; set; }
}

public class ResourceDeposit
{
    public ResourceType Type { get; init; }
    public double Quantity { get; set; }
    public double MaxCapacity { get; init; }
    public double RegenerationRate { get; init; }
}
