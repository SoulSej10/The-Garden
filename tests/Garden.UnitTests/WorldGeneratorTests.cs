using Garden.Core.World;
using Garden.Engine.Generation;
using Xunit;
using Xunit.Abstractions;

namespace Garden.UnitTests;

public class WorldGeneratorTests
{
    private readonly ITestOutputHelper _output;

    public WorldGeneratorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(9999)]
    public void Generate_ProducesBalancedTerrainDistribution_NoSingleTypeDominates(int seed)
    {
        var generator = new WorldGenerator(seed);
        var map = generator.Generate(100, 100);
        var tiles = map.GetAllTiles().ToList();
        var total = tiles.Count;

        var byTerrain = tiles.GroupBy(t => t.Terrain)
            .ToDictionary(g => g.Key, g => g.Count() * 100.0 / total);

        _output.WriteLine($"Seed {seed}: {string.Join(", ", byTerrain.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value:F1}%"))}");

        // Regression guard: elevation used to be re-normalized (min/max)
        // across the whole grid after subtracting an edge falloff, which
        // dragged the grid-wide minimum down and pushed almost the entire
        // interior above the mountain threshold (~80% mountains, the
        // reported bug). No single terrain type should dominate the map.
        foreach (var (terrain, pct) in byTerrain)
        {
            Assert.True(pct < 45.0, $"{terrain} covers {pct:F1}% of the map - terrain generation is not balanced");
        }

        Assert.True(byTerrain.GetValueOrDefault(TerrainType.Mountains) < 35.0,
            $"Mountains cover {byTerrain.GetValueOrDefault(TerrainType.Mountains):F1}% - should form ranges, not dominate the map");
    }

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
