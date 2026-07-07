using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class Technology
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double ProgressRequired { get; set; } = 100.0;
    public double CurrentProgress { get; set; }
    public bool IsDiscovered { get; set; }
    public long? DiscoveredTick { get; set; }
    public GameEntityId? DiscoveredBySettlementId { get; set; }
    public string DiscoveredBySettlementName { get; set; } = string.Empty;
    public GameEntityId? DiscoveredByCitizenId { get; set; }
    public string DiscoveredByCitizenName { get; set; } = string.Empty;

    public static IReadOnlyList<Technology> AllTechnologies => _all;
    private static readonly List<Technology> _all = CreateAll();

    private static List<Technology> CreateAll()
    {
        return
        [
            new() { Name = "Basic Farming", Category = "Agriculture", Description = "Simple crop cultivation", ProgressRequired = 50.0 },
            new() { Name = "Crop Rotation", Category = "Agriculture", Description = "Rotating crops to maintain soil fertility", ProgressRequired = 80.0 },
            new() { Name = "Irrigation", Category = "Agriculture", Description = "Channeling water to fields", ProgressRequired = 120.0 },
            new() { Name = "Advanced Plowing", Category = "Agriculture", Description = "Improved tilling techniques", ProgressRequired = 180.0 },

            new() { Name = "Thatched Roof", Category = "Construction", Description = "Basic waterproof roofing", ProgressRequired = 40.0 },
            new() { Name = "Timber Framing", Category = "Construction", Description = "Strong wooden building frames", ProgressRequired = 70.0 },
            new() { Name = "Stone Masonry", Category = "Construction", Description = "Durable stone construction", ProgressRequired = 110.0 },
            new() { Name = "Road Building", Category = "Construction", Description = "Connecting settlements with roads", ProgressRequired = 150.0 },

            new() { Name = "Stone Tools", Category = "Tools", Description = "Basic stone implements", ProgressRequired = 30.0 },
            new() { Name = "Woodworking", Category = "Tools", Description = "Refined wood crafting", ProgressRequired = 60.0 },
            new() { Name = "Metalworking", Category = "Tools", Description = "Working with metals", ProgressRequired = 140.0 },
            new() { Name = "Carpentry", Category = "Tools", Description = "Advanced wood construction", ProgressRequired = 100.0 },

            new() { Name = "Footpaths", Category = "Transportation", Description = "Establishing walking routes", ProgressRequired = 40.0 },
            new() { Name = "Carts", Category = "Transportation", Description = "Wheeled transport for goods", ProgressRequired = 100.0 },
            new() { Name = "Water Travel", Category = "Transportation", Description = "Simple watercraft", ProgressRequired = 130.0 },

            new() { Name = "Grain Milling", Category = "Resource Processing", Description = "Processing grain into flour", ProgressRequired = 60.0 },
            new() { Name = "Pottery", Category = "Resource Processing", Description = "Clay vessels for storage", ProgressRequired = 50.0 },
            new() { Name = "Weaving", Category = "Resource Processing", Description = "Textile production", ProgressRequired = 70.0 },
            new() { Name = "Smelting", Category = "Resource Processing", Description = "Extracting metal from ore", ProgressRequired = 120.0 },
        ];
    }
}
