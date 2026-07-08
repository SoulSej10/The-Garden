using Garden.Core.World;
using Garden.Engine.Random;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Generation;

public class WorldGenerator
{
    private readonly SimulationRandom _random;
    private WorldMap _map = new();
    private int _width;
    private int _height;

    public WorldGenerator(int seed)
    {
        _random = new SimulationRandom(seed);
    }

    public WorldMap Generate(int width, int height)
    {
        _width = width;
        _height = height;
        _map = new WorldMap();
        _map.Initialize(width, height);

        GenerateElevation();
        GenerateTerrain();
        GenerateRivers();
        GenerateLakes();
        AssignClimates();
        GenerateBiomes();
        GenerateResources();
        GenerateForests();

        return _map;
    }

    private int[] _perm = [];

    private void GenerateElevation()
    {
        _perm = BuildPermutationTable();

        var raw = new double[_width, _height];
        var min = double.MaxValue;
        var max = double.MinValue;

        // Continents/oceans come from low-frequency fractal noise (a handful of
        // broad landmasses), refined by higher-frequency octaves for coastline
        // and mountain-range detail - not a fixed geometric shape.
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var n = FractalNoise(x * 0.015, y * 0.015, octaves: 5, persistence: 0.5, lacunarity: 2.0);

            // Gentle falloff only within the outer margin of the map, so
            // landmasses are free to form anywhere in the interior instead of
            // radiating from a single center point, while the world still has
            // a coherent edge (mostly ocean at the border, like a real map).
            var edgeFalloff = EdgeFalloff(x, y);
            n -= edgeFalloff;

            raw[x, y] = n;
            min = Math.Min(min, n);
            max = Math.Max(max, n);
        }

        var range = Math.Max(1e-6, max - min);
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var elevation = Math.Clamp((raw[x, y] - min) / range, 0.0, 1.0);
            var tile = new WorldTile { X = x, Y = y, Elevation = elevation };
            _map.SetTile(x, y, tile);
        }
    }

    private double EdgeFalloff(int x, int y)
    {
        var marginX = _width * 0.12;
        var marginY = _height * 0.12;
        var distToEdge = Math.Min(
            Math.Min(x, _width - 1 - x) / marginX,
            Math.Min(y, _height - 1 - y) / marginY);
        var t = Math.Clamp(1.0 - distToEdge, 0.0, 1.0);
        var smooth = t * t * (3 - 2 * t);
        return smooth * 0.6;
    }

    private double FractalNoise(double x, double y, int octaves, double persistence, double lacunarity)
    {
        var total = 0.0;
        var amplitude = 1.0;
        var frequency = 1.0;
        var amplitudeSum = 0.0;

        for (var o = 0; o < octaves; o++)
        {
            total += ValueNoise(x * frequency, y * frequency) * amplitude;
            amplitudeSum += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / amplitudeSum;
    }

    private double ValueNoise(double x, double y)
    {
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = x0 + 1;
        var y1 = y0 + 1;

        var sx = x - x0;
        var sy = y - y0;
        var u = sx * sx * (3 - 2 * sx);
        var v = sy * sy * (3 - 2 * sy);

        var n00 = HashToUnit(x0, y0);
        var n10 = HashToUnit(x1, y0);
        var n01 = HashToUnit(x0, y1);
        var n11 = HashToUnit(x1, y1);

        var ix0 = n00 + u * (n10 - n00);
        var ix1 = n01 + u * (n11 - n01);
        return ix0 + v * (ix1 - ix0);
    }

    private double HashToUnit(int x, int y)
    {
        var xi = x & 255;
        var yi = y & 255;
        var h = _perm[(_perm[xi] + yi) & 255];
        return h / 255.0 * 2.0 - 1.0;
    }

    private int[] BuildPermutationTable()
    {
        var table = new int[256];
        for (var i = 0; i < 256; i++) table[i] = i;

        for (var i = 255; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (table[i], table[j]) = (table[j], table[i]);
        }

        return table;
    }

    private void GenerateTerrain()
    {
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            tile.Terrain = tile.Elevation switch
            {
                < 0.2 => TerrainType.Ocean,
                < 0.25 => TerrainType.Coast,
                < 0.4 => TerrainType.Plains,
                < 0.5 => TerrainType.Grassland,
                < 0.6 => TerrainType.Hills,
                < 0.8 => TerrainType.Mountains,
                _ => TerrainType.Mountains
            };
        }
    }

    private void GenerateRivers()
    {
        for (var i = 0; i < _width / 5; i++)
        {
            var startX = _random.Next(0, _width);
            var startY = _random.Next(0, _height);
            var tile = _map.GetTile(startX, startY);
            if (tile.Elevation < 0.3 || tile.Elevation > 0.7) continue;

            var x = startX;
            var y = startY;
            for (var step = 0; step < Math.Max(_width, _height) / 2; step++)
            {
                if (x < 0 || x >= _width || y < 0 || y >= _height) break;
                var current = _map.GetTile(x, y);
                current.IsRiver = true;
                current.Moisture = 1.0;
                if (current.Terrain != TerrainType.Ocean && current.Elevation > 0.2)
                    current.Terrain = TerrainType.River;

                var neighbors = _map.GetNeighbors(x, y)
                    .Where(n => n.Elevation < current.Elevation)
                    .ToList();

                if (neighbors.Count == 0) break;

                var next = neighbors.OrderBy(n => n.Elevation).First();
                x = next.X;
                y = next.Y;
            }
        }
    }

    private void GenerateLakes()
    {
        for (var x = 1; x < _width - 1; x++)
        for (var y = 1; y < _height - 1; y++)
        {
            var tile = _map.GetTile(x, y);
            if (tile.Elevation < 0.2 || tile.Elevation > 0.35) continue;

            var neighbors = _map.GetNeighbors(x, y).ToList();
            var avgElevation = neighbors.Average(n => n.Elevation);
            if (avgElevation > tile.Elevation + 0.05 && _random.NextDouble() < 0.02)
            {
                tile.IsLake = true;
                tile.Terrain = TerrainType.Lake;
                tile.Moisture = 1.0;
            }
        }
    }

    private void AssignClimates()
    {
        var equatorLine = _height / 2.0;

        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            var distFromEquator = Math.Abs(y - equatorLine) / equatorLine;

            tile.Climate = (tile.Elevation, distFromEquator) switch
            {
                (>= 0.7, _) => ClimateZone.Highland,
                (_, < 0.3) => ClimateZone.Tropical,
                (_, < 0.5) => ClimateZone.Dry,
                (_, < 0.7) => ClimateZone.Temperate,
                _ => ClimateZone.Cold
            };

            tile.Temperature = tile.Climate switch
            {
                ClimateZone.Tropical => 28 + _random.NextDouble(-3, 3),
                ClimateZone.Dry => 32 + _random.NextDouble(-4, 4),
                ClimateZone.Temperate => 15 + _random.NextDouble(-5, 5),
                ClimateZone.Cold => 2 + _random.NextDouble(-5, 5),
                ClimateZone.Highland => 8 + _random.NextDouble(-5, 5),
                _ => 20
            };

            if (!tile.IsRiver && !tile.IsLake)
            {
                tile.Moisture = tile.Climate switch
                {
                    ClimateZone.Tropical => _random.NextDouble(0.6, 0.9),
                    ClimateZone.Dry => _random.NextDouble(0.0, 0.2),
                    ClimateZone.Temperate => _random.NextDouble(0.4, 0.7),
                    ClimateZone.Cold => _random.NextDouble(0.3, 0.6),
                    ClimateZone.Highland => _random.NextDouble(0.3, 0.6),
                    _ => 0.5
                };
            }
        }
    }

    private void GenerateBiomes()
    {
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);

            tile.Biome = (tile.Terrain, tile.Climate, tile.Moisture) switch
            {
                (TerrainType.Ocean, _, _) => BiomeType.TemperateForest,
                (TerrainType.Coast, _, _) => BiomeType.Mediterranean,
                (TerrainType.Lake, _, _) => BiomeType.Wetland,
                (TerrainType.Swamp, _, _) => BiomeType.Wetland,
                (TerrainType.Mountains, _, _) => BiomeType.Alpine,
                (_, ClimateZone.Tropical, >= 0.7) => BiomeType.TropicalRainforest,
                (_, ClimateZone.Tropical, _) => BiomeType.TropicalSavanna,
                (_, ClimateZone.Dry, _) => BiomeType.Desert,
                (_, ClimateZone.Temperate, >= 0.5) => BiomeType.TemperateForest,
                (_, ClimateZone.Temperate, _) => BiomeType.TemperateGrassland,
                (_, ClimateZone.Cold, >= 0.4) => BiomeType.Taiga,
                (_, ClimateZone.Cold, _) => BiomeType.Tundra,
                (_, ClimateZone.Highland, _) => BiomeType.Alpine,
                _ => BiomeType.TemperateGrassland
            };
        }
    }

    private void GenerateResources()
    {
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);

            if (tile.Terrain == TerrainType.Forest || tile.Biome is BiomeType.TropicalRainforest or BiomeType.TemperateForest or BiomeType.Taiga)
            {
                tile.Resources.Add(new ResourceDeposit
                {
                    Type = ResourceType.Trees,
                    Quantity = _random.NextDouble(50, 100),
                    MaxCapacity = 100,
                    RegenerationRate = 0.1
                });
            }

            if (tile.Terrain is TerrainType.Hills or TerrainType.Mountains)
            {
                tile.Resources.Add(new ResourceDeposit
                {
                    Type = ResourceType.Stone,
                    Quantity = _random.NextDouble(200, 500),
                    MaxCapacity = 500,
                    RegenerationRate = 0.0
                });
            }

            if (tile.Terrain is TerrainType.Plains or TerrainType.Grassland && tile.Moisture > 0.3)
            {
                tile.Resources.Add(new ResourceDeposit
                {
                    Type = ResourceType.WildPlants,
                    Quantity = _random.NextDouble(20, 50),
                    MaxCapacity = 50,
                    RegenerationRate = 0.05
                });
            }

            if (tile.Terrain is TerrainType.River or TerrainType.Lake or TerrainType.Coast)
            {
                tile.Resources.Add(new ResourceDeposit
                {
                    Type = ResourceType.FreshWater,
                    Quantity = 1000,
                    MaxCapacity = 1000,
                    RegenerationRate = 1.0
                });
            }

            if (tile.Moisture > 0.4 && tile.Terrain is TerrainType.Plains or TerrainType.Grassland or TerrainType.Hills)
            {
                tile.Resources.Add(new ResourceDeposit
                {
                    Type = ResourceType.Clay,
                    Quantity = _random.NextDouble(30, 100),
                    MaxCapacity = 100,
                    RegenerationRate = 0.01
                });
            }
        }
    }

    private void GenerateForests()
    {
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            if (tile.Terrain != TerrainType.Plains && tile.Terrain != TerrainType.Grassland) continue;
            if (tile.Moisture < 0.4) continue;

            var forestChance = tile.Moisture switch
            {
                >= 0.7 => 0.3,
                >= 0.5 => 0.15,
                _ => 0.05
            };

            if (_random.NextDouble() < forestChance)
            {
                tile.Terrain = TerrainType.Forest;
            }
        }
    }
}
