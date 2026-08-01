namespace Garden.Core.World;

public enum TerrainType
{
    Ocean,
    Coast,
    Plains,
    Grassland,
    Hills,
    Mountains,
    Forest,
    Swamp,
    River,
    Lake,
    // Added for the layered geological generator (WorldGenerator.GenerateSpecialTerrain) -
    // each has a causal rule keyed off Relief/FlowAccumulation/IsVolcanic/latitude/climate
    // rather than being a cosmetic palette swap. Crater is the sole exception (decorative,
    // not tectonically derived) - see that stage's comment.
    Desert,
    Badlands,
    Canyon,
    Glacier,
    Fjord,
    VolcanicIsland,
    GeothermalField,
    CoralReef,
    Delta,
    Crater
}

public enum BiomeType
{
    TropicalRainforest,
    TropicalSavanna,
    TemperateForest,
    TemperateGrassland,
    Mediterranean,
    Desert,
    Taiga,
    Tundra,
    Alpine,
    Wetland,
    Steppe,
    PolarDesert,
    VolcanicWasteland,
    Marine
}

/// <summary>
/// How a tile's terrain relates to the nearest tectonic plate boundary at
/// generation time (WorldGenerator.GenerateBoundaryUplift). Scratch-derived
/// sub-classification (collision vs. subduction vs. island-arc, used only to
/// pick an uplift magnitude) intentionally does not persist here - only the
/// coarse category a citizen/player would care about does.
/// </summary>
public enum PlateBoundaryType
{
    None,
    Convergent,
    Divergent,
    Transform
}

/// <summary>
/// The overall shape of a generated world, rolled once per seed
/// (WorldGenerator.GeneratePlates) and carried onto WorldState purely for
/// display/flavor - it drives plate count and land/ocean plate-type ratio
/// but nothing downstream branches on it directly.
/// </summary>
public enum WorldArchetype
{
    Normal,
    Supercontinent,
    Archipelago,
    WaterWorld
}

public enum ClimateZone
{
    Tropical,
    Temperate,
    Dry,
    Cold,
    Highland
}

public enum WeatherState
{
    Clear,
    Cloudy,
    Rain,
    HeavyRain,
    Storm,
    Fog,
    Snow
}

public enum ResourceType
{
    Trees,
    Stone,
    Clay,
    WildPlants,
    FreshWater
}
