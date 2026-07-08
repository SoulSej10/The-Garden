using Garden.Core.World;
using Garden.Engine.Generation;
using Garden.Engine.Pathfinding;
using Garden.World.Entities;
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

        // Regression guard: terrain type used to be classified against
        // FIXED absolute elevation cutoffs. A single low-octave noise field
        // only has ~1.5 wavelengths across a 100-tile map, so a particular
        // seed could easily produce one dominant high-elevation region
        // covering half the map (up to 62.8% mountains was observed across
        // 60 random seeds). Elevation is now classified by PERCENTILE rank
        // within each generated map, which guarantees terrain-type
        // proportions stay consistent regardless of seed - mountains should
        // always land close to their ~12% target share.
        foreach (var (terrain, pct) in byTerrain)
        {
            Assert.True(pct < 30.0, $"{terrain} covers {pct:F1}% of the map - terrain generation is not balanced");
        }

        Assert.True(byTerrain.GetValueOrDefault(TerrainType.Mountains) < 16.0,
            $"Mountains cover {byTerrain.GetValueOrDefault(TerrainType.Mountains):F1}% - should be a sparse percentile-capped share, not dominate the map");
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
    public void Generate_MountainShareStaysBounded_AcrossManyRandomSeeds()
    {
        var rng = new Random(12345);
        var worstMountainPct = 0.0;
        var worstSeed = 0;
        var failures = 0;

        for (var i = 0; i < 60; i++)
        {
            var seed = rng.Next();
            var generator = new WorldGenerator(seed);
            var map = generator.Generate(100, 100);
            var tiles = map.GetAllTiles().ToList();
            var total = tiles.Count;
            var mountainPct = tiles.Count(t => t.Terrain == TerrainType.Mountains) * 100.0 / total;

            if (mountainPct > worstMountainPct)
            {
                worstMountainPct = mountainPct;
                worstSeed = seed;
            }
            if (mountainPct > 20.0) failures++;
        }

        _output.WriteLine($"Worst seed: {worstSeed} with {worstMountainPct:F1}% mountains. Failures (>20%): {failures}/60");

        Assert.True(failures == 0,
            $"{failures}/60 random seeds produced >20% mountain coverage (worst: {worstMountainPct:F1}% on seed {worstSeed})");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(9999)]
    [InlineData(777)]
    [InlineData(555555)]
    public void Generate_HabitableTilesCanReachWater(int seed)
    {
        var generator = new WorldGenerator(seed);
        var map = generator.Generate(100, 100);

        bool HasWaterAt(WorldTile t) =>
            t.IsRiver || t.IsLake || t.Terrain == TerrainType.Coast
            || t.Resources.Any(r => r.Type == ResourceType.FreshWater && r.Quantity > 0);

        var habitable = map.GetAllTiles()
            .Where(t => t.Terrain is not (TerrainType.Ocean or TerrainType.Lake or TerrainType.Mountains))
            .ToList();

        // Sample habitable tiles (checking all ~8000 would be slow) and
        // verify most of them can actually reach a water source within a
        // practical walking distance - the same BFS radius CitizenSystem
        // uses. If mountains or other unwalkable terrain wall off large
        // regions from any water, citizens there are doomed regardless of
        // how good their AI is.
        var rng = new Random(seed);
        var sample = habitable.OrderBy(_ => rng.Next()).Take(150).ToList();
        var reachable = sample.Count(t => Pathfinder.FindNearestPath(map, t.X, t.Y, HasWaterAt, maxRadius: 60).Count > 0);
        var pct = reachable * 100.0 / sample.Count;

        _output.WriteLine($"Seed {seed}: {reachable}/{sample.Count} sampled habitable tiles ({pct:F1}%) can reach water within 60 tiles");

        Assert.True(pct > 90.0,
            $"Only {pct:F1}% of habitable tiles can reach water - mountains or terrain may be blocking access");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(9999)]
    public void Diagnostic_WaterDistanceDistribution(int seed)
    {
        var generator = new WorldGenerator(seed);
        var map = generator.Generate(100, 100);

        bool HasWaterAt(WorldTile t) =>
            t.IsRiver || t.IsLake || t.Terrain == TerrainType.Coast
            || t.Resources.Any(r => r.Type == ResourceType.FreshWater && r.Quantity > 0);

        var habitable = map.GetAllTiles()
            .Where(t => t.Terrain is not (TerrainType.Ocean or TerrainType.Lake or TerrainType.Mountains))
            .ToList();

        var rng = new Random(seed);
        var sample = habitable.OrderBy(_ => rng.Next()).Take(150).ToList();
        var distances = sample
            .Select(t => Pathfinder.FindNearestPath(map, t.X, t.Y, HasWaterAt, maxRadius: 60).Count - 1)
            .Where(d => d >= 0)
            .OrderBy(d => d)
            .ToList();

        var avg = distances.Average();
        var median = distances[distances.Count / 2];
        var p90 = distances[(int)(distances.Count * 0.9)];
        var max = distances.Max();

        _output.WriteLine($"Seed {seed}: water path-distance (tiles) - avg={avg:F1} median={median} p90={p90} max={max}");
        _output.WriteLine($"  Ticks-to-critical-thirst from warning(60): {(80.0 - 60.0) / 0.7:F0} ticks before critical, {(100.0 - 60.0) / 0.7:F0} ticks to death-range");
    }

    [Fact]
    public void Diagnostic_InspectStuckTile()
    {
        var generator = new WorldGenerator(seed: 42);
        var map = generator.Generate(100, 100);

        var tile = map.GetTile(4, 4);
        _output.WriteLine($"Tile (4,4): terrain={tile.Terrain} elevation={tile.Elevation:F3} isRiver={tile.IsRiver} isLake={tile.IsLake} moisture={tile.Moisture:F2}");

        _output.WriteLine("Neighborhood (9x9 around 4,4):");
        for (var y = 0; y <= 8; y++)
        {
            var row = "";
            for (var x = 0; x <= 8; x++)
            {
                if (x >= map.Width || y >= map.Height) { row += " ?"; continue; }
                var t = map.GetTile(x, y);
                var symbol = t.Terrain switch
                {
                    TerrainType.Ocean => " ~",
                    TerrainType.Coast => " c",
                    TerrainType.Mountains => " M",
                    TerrainType.Hills => " h",
                    TerrainType.Plains => " p",
                    TerrainType.Grassland => " g",
                    TerrainType.Forest => " f",
                    TerrainType.River => " R",
                    TerrainType.Lake => " L",
                    TerrainType.Swamp => " s",
                    _ => " ."
                };
                row += symbol;
            }
            _output.WriteLine(row);
        }

        bool HasWaterAt(WorldTile t) =>
            t.IsRiver || t.IsLake || t.Terrain == TerrainType.Coast
            || t.Resources.Any(r => r.Type == ResourceType.FreshWater && r.Quantity > 0);

        var path = Pathfinder.FindNearestPath(map, 4, 4, HasWaterAt, maxRadius: 60);
        _output.WriteLine($"Path from (4,4) to water (maxRadius 60): {(path.Count > 0 ? $"found, {path.Count} steps, ends at ({path[^1].X},{path[^1].Y})" : "NOT FOUND")}");

        var path100 = Pathfinder.FindNearestPath(map, 4, 4, HasWaterAt, maxRadius: 200);
        _output.WriteLine($"Path from (4,4) to water (maxRadius 200): {(path100.Count > 0 ? $"found, {path100.Count} steps, ends at ({path100[^1].X},{path100[^1].Y})" : "NOT FOUND")}");
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
