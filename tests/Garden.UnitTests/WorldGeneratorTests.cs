using Garden.Core.World;
using Garden.Engine.Generation;
using Xunit;

namespace Garden.UnitTests;

public class WorldGeneratorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(9999)]
    public void Generate_ProducesDiverseTerrain_NotASingleCenteredOcean(int seed)
    {
        var generator = new WorldGenerator(seed);
        var map = generator.Generate(60, 60);
        var tiles = map.GetAllTiles().ToList();

        var byTerrain = tiles.GroupBy(t => t.Terrain).ToDictionary(g => g.Key, g => g.Count());

        // A healthy world has multiple terrain types represented, not just
        // ocean-in-the-middle-of-a-land-ring.
        Assert.True(byTerrain.Count >= 5, $"Expected varied terrain, got: {string.Join(",", byTerrain.Keys)}");
        Assert.True(byTerrain.GetValueOrDefault(TerrainType.Plains) + byTerrain.GetValueOrDefault(TerrainType.Grassland) > 0,
            "Expected habitable plains/grassland tiles to exist");

        // The dead center of the map should not deterministically be ocean -
        // that was the old "bullseye" bug (elevation = distance from center).
        var center = map.GetTile(30, 30);
        var centerRegionOceanCount = new List<(int, int)>
        {
            (28, 28), (30, 30), (32, 32), (28, 32), (32, 28)
        }.Count(p => map.GetTile(p.Item1, p.Item2).Terrain == TerrainType.Ocean);
        Assert.True(centerRegionOceanCount < 5, "Center of map should not be entirely ocean for every seed");
    }

    [Fact]
    public void Generate_ForestTilesHaveEdibleResources()
    {
        var generator = new WorldGenerator(7);
        var map = generator.Generate(60, 60);

        var forestTiles = map.GetAllTiles().Where(t => t.Terrain == TerrainType.Forest).ToList();
        Assert.NotEmpty(forestTiles);

        // At least some forest tiles must carry an actual WildPlants deposit -
        // otherwise citizens treating "Forest" as food is a false positive.
        var withFood = forestTiles.Count(t => t.Resources.Any(r => r.Type == ResourceType.WildPlants));
        Assert.True(withFood > 0, "Expected some forest tiles to have WildPlants deposits");
    }
}
