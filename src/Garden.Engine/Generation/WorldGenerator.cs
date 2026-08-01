using Garden.Core.World;
using Garden.Engine.Random;
using Garden.World.Collections;
using Garden.World.Entities;

namespace Garden.Engine.Generation;

/// <summary>
/// Layered geological world generator. Replaces the old single-pass
/// elevation-noise-with-percentile-cutoffs approach with a multi-stage,
/// plate-motivated pipeline: static Voronoi-style tectonic plates seed
/// boundary-driven uplift, domain-warped fractal noise adds detail, a
/// droplet-based hydraulic erosion pass carves the result, flow-accumulation
/// hydrology produces real dendritic river networks and causal lake basins,
/// and a stylized prevailing-wind model drives rain-shadow deserts.
///
/// This is an established procedural-terrain technique (plates as a
/// Voronoi partition with velocity vectors used once for boundary
/// classification; erosion as a particle/droplet heuristic; rivers via
/// priority-flood + D8 flow accumulation) - not a physically simulated
/// tectonic/hydrological model. Plates never move during generation.
/// </summary>
public class WorldGenerator
{
    private readonly SimulationRandom _random;
    private WorldMap _map = new();
    private int _width;
    private int _height;
    private int[] _perm = [];

    public WorldArchetype Archetype { get; private set; }
    public List<ClimateData> ClimateZones { get; private set; } = [];

    // Fixed absolute sea-level-relative terrain bands (storage elevation is
    // 0..1 with 0.5 = sea level). Land/ocean proportion now genuinely varies
    // by archetype instead of being guaranteed identical by percentile
    // cutoffs on every seed - that's an intentional behavior change.
    private const double OceanCutoff = 0.46;
    private const double CoastCutoff = 0.52;
    private const double PlainsCutoff = 0.62;
    private const double GrasslandCutoff = 0.74;
    private const double HillsCutoff = 0.86;

    private sealed class Plate
    {
        public int Id;
        public double SeedX, SeedY;
        public bool IsOceanic;
        public double BaseElevation;
        public double VelX, VelY;
    }

    private Plate[] _plates = [];
    private int[] _plateId = [];
    private List<(double X, double Y, double Radius)> _hotspots = [];
    private double[] _uplift = [];
    private double[] _boundaryProximity = [];
    private bool[] _deltaCandidate = [];

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
        _perm = BuildPermutationTable();

        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
            _map.SetTile(x, y, new WorldTile { X = x, Y = y });

        GeneratePlates();
        GenerateBoundaryUplift();
        GenerateBaseElevation();
        GenerateErosion();
        GenerateTerrain();
        GenerateHydrology();
        GenerateClimate();
        GenerateSpecialTerrain();
        GenerateSwamps();
        GenerateBiomes();
        GenerateResources();
        GenerateForests();

        return _map;
    }

    private int Idx(int x, int y) => x * _height + y;
    private (int X, int Y) FromIdx(int i) => (i / _height, i % _height);
    private static double Dist(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static int PoissonSample(SimulationRandom rnd, double mean)
    {
        if (mean <= 0) return 0;
        var l = Math.Exp(-mean);
        var k = 0;
        var p = 1.0;
        do
        {
            k++;
            p *= rnd.NextDouble();
        } while (p > l);
        return k - 1;
    }

    // ================= Stage 1: Plates =================

    private void GeneratePlates()
    {
        var archRoll = _random.NextDouble();
        Archetype = archRoll switch
        {
            < 0.15 => WorldArchetype.Supercontinent,
            < 0.30 => WorldArchetype.Archipelago,
            < 0.45 => WorldArchetype.WaterWorld,
            _ => WorldArchetype.Normal
        };

        int plateCount;
        double oceanicWeightMin, oceanicWeightMax;
        switch (Archetype)
        {
            case WorldArchetype.Supercontinent:
                plateCount = _random.Next(4, 9);
                oceanicWeightMin = 0.30; oceanicWeightMax = 0.45;
                break;
            case WorldArchetype.Archipelago:
                plateCount = _random.Next(22, 37);
                oceanicWeightMin = 0.65; oceanicWeightMax = 0.85;
                break;
            case WorldArchetype.WaterWorld:
                plateCount = _random.Next(10, 23);
                oceanicWeightMin = 0.80; oceanicWeightMax = 0.92;
                break;
            default:
                plateCount = _random.Next(10, 23);
                oceanicWeightMin = 0.45; oceanicWeightMax = 0.65;
                break;
        }
        var oceanicWeight = _random.NextDouble(oceanicWeightMin, oceanicWeightMax);

        // Poisson-disc-lite: rejection-sample candidate points, keep the
        // best-spaced candidate found within a fixed try budget so this
        // always terminates even for tight plate counts.
        var minDist = 0.6 * Math.Sqrt((double)(_width * _height) / plateCount);
        var seeds = new List<(double X, double Y)>();
        for (var i = 0; i < plateCount; i++)
        {
            var best = (X: _random.NextDouble() * _width, Y: _random.NextDouble() * _height);
            var bestDist = -1.0;
            for (var t = 0; t < 30; t++)
            {
                var cx = _random.NextDouble() * _width;
                var cy = _random.NextDouble() * _height;
                var d = seeds.Count == 0 ? double.MaxValue : seeds.Min(s => Dist(s.X, s.Y, cx, cy));
                if (d >= minDist) { best = (cx, cy); bestDist = d; break; }
                if (d > bestDist) { best = (cx, cy); bestDist = d; }
            }
            seeds.Add(best);
        }

        _plates = new Plate[plateCount];
        for (var i = 0; i < plateCount; i++)
        {
            var isOceanic = _random.NextDouble() < oceanicWeight;
            var angle = _random.NextDouble() * Math.PI * 2;
            var speed = _random.NextDouble(0.3, 1.0);
            _plates[i] = new Plate
            {
                Id = i,
                SeedX = seeds[i].X,
                SeedY = seeds[i].Y,
                IsOceanic = isOceanic,
                BaseElevation = isOceanic ? _random.NextDouble(-0.35, -0.05) : _random.NextDouble(0.08, 0.30),
                VelX = Math.Cos(angle) * speed,
                VelY = Math.Sin(angle) * speed
            };
        }

        // Nearest-seed tile assignment. Brute force is deliberate: at K<=36
        // plates and up to ~65K tiles that's a couple million distance
        // checks (<20ms) - jump-flooding only pays for itself at plate
        // counts/grid sizes an order of magnitude larger than this.
        _plateId = new int[_width * _height];
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var bestPlate = 0;
            var bestD = double.MaxValue;
            for (var p = 0; p < _plates.Length; p++)
            {
                var d = Dist(_plates[p].SeedX, _plates[p].SeedY, x, y);
                if (d < bestD) { bestD = d; bestPlate = p; }
            }
            _plateId[Idx(x, y)] = bestPlate;
        }

        var hotspotCount = PoissonSample(_random, (double)(_width * _height) / 12000.0);
        _hotspots = [];
        for (var i = 0; i < hotspotCount; i++)
        {
            _hotspots.Add((_random.NextDouble() * _width, _random.NextDouble() * _height, _random.NextDouble(4, 8)));
        }
    }

    // ================= Stage 2: Boundary-driven uplift =================

    private void GenerateBoundaryUplift()
    {
        var n = _width * _height;
        var boundaryType = new PlateBoundaryType[n];
        var boundaryMagnitude = new double[n];
        var isVolcanicSource = new bool[n];
        var dist = new int[n];
        Array.Fill(dist, -1);
        var nearestSource = new int[n];
        Array.Fill(nearestSource, -1);

        var queue = new Queue<int>();
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var i = Idx(x, y);
            var myPlate = _plateId[i];
            var otherPlate = -1;
            for (var dx = -1; dx <= 1 && otherPlate == -1; dx++)
            for (var dy = -1; dy <= 1 && otherPlate == -1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var nx = x + dx; var ny = y + dy;
                if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;
                var np = _plateId[Idx(nx, ny)];
                if (np != myPlate) otherPlate = np;
            }
            if (otherPlate == -1) continue;

            var (type, magnitude, volcanic) = ClassifyBoundary(_plates[myPlate], _plates[otherPlate]);
            boundaryType[i] = type;
            boundaryMagnitude[i] = magnitude;
            isVolcanicSource[i] = volcanic;
            dist[i] = 0;
            nearestSource[i] = i;
            queue.Enqueue(i);
        }

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            var (cx, cy) = FromIdx(cur);
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var nx = cx + dx; var ny = cy + dy;
                if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;
                var ni = Idx(nx, ny);
                if (dist[ni] != -1) continue;
                dist[ni] = dist[cur] + 1;
                nearestSource[ni] = nearestSource[cur];
                queue.Enqueue(ni);
            }
        }

        var falloffLength = 0.06 * Math.Min(_width, _height);
        _uplift = new double[n];
        _boundaryProximity = new double[n];

        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var i = Idx(x, y);
            var tile = _map.GetTile(x, y);
            var src = nearestSource[i];
            if (src == -1) continue;

            var proximity = Math.Exp(-dist[i] / falloffLength);
            _boundaryProximity[i] = proximity;
            tile.BoundaryType = boundaryType[src];
            _uplift[i] = boundaryMagnitude[src] * proximity;
            // Narrow band right at the boundary, not the whole falloff zone -
            // volcanism should read as a rare, specific feature (arcs/rifts),
            // not a broad regional tint. GenerateSpecialTerrain further gates
            // GeothermalField with its own probability roll on top of this.
            if (isVolcanicSource[src] && dist[i] <= 1) tile.IsVolcanic = true;
        }

        foreach (var (hx, hy, radius) in _hotspots)
        {
            var minX = Math.Max(0, (int)(hx - radius));
            var maxX = Math.Min(_width - 1, (int)(hx + radius));
            var minY = Math.Max(0, (int)(hy - radius));
            var maxY = Math.Min(_height - 1, (int)(hy + radius));
            for (var x = minX; x <= maxX; x++)
            for (var y = minY; y <= maxY; y++)
            {
                var d = Dist(hx, hy, x, y);
                if (d > radius) continue;
                var tile = _map.GetTile(x, y);
                tile.IsVolcanic = true;
                if (d < radius * 0.5) _uplift[Idx(x, y)] += 0.12 * (1 - d / radius);
            }
        }
    }

    private (PlateBoundaryType Type, double Magnitude, bool Volcanic) ClassifyBoundary(Plate a, Plate b)
    {
        var nx = b.SeedX - a.SeedX;
        var ny = b.SeedY - a.SeedY;
        var nlen = Math.Max(1e-6, Math.Sqrt(nx * nx + ny * ny));
        nx /= nlen; ny /= nlen;

        var relVelX = b.VelX - a.VelX;
        var relVelY = b.VelY - a.VelY;
        var dot = relVelX * nx + relVelY * ny;

        const double threshold = 0.15;
        if (dot > threshold)
        {
            if (!a.IsOceanic && !b.IsOceanic) return (PlateBoundaryType.Convergent, _random.NextDouble(0.5, 0.7), false);
            if (a.IsOceanic ^ b.IsOceanic) return (PlateBoundaryType.Convergent, _random.NextDouble(0.35, 0.5), true);
            return (PlateBoundaryType.Convergent, _random.NextDouble(0.25, 0.4), true);
        }
        if (dot < -threshold)
        {
            if (!a.IsOceanic && !b.IsOceanic) return (PlateBoundaryType.Divergent, _random.NextDouble(-0.3, -0.15), false);
            return (PlateBoundaryType.Divergent, _random.NextDouble(0.05, 0.15), false);
        }
        return (PlateBoundaryType.Transform, 0.0, false);
    }

    // ================= Stage 3: Base elevation =================

    private void GenerateBaseElevation()
    {
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var i = Idx(x, y);
            var tile = _map.GetTile(x, y);
            var plate = _plates[_plateId[i]];

            var warpX = FractalNoise(x * 0.01 + 1000, y * 0.01 + 1000, 2, 0.5, 2.0);
            var warpY = FractalNoise(x * 0.01 + 2000, y * 0.01 + 2000, 2, 0.5, 2.0);
            var wx = x + 12.0 * warpX;
            var wy = y + 12.0 * warpY;

            var detail = FractalNoise(wx * 0.02, wy * 0.02, 5, 0.5, 2.0);

            if (tile.BoundaryType == PlateBoundaryType.Convergent)
            {
                var ridge = 1.0 - Math.Abs(FractalNoise(wx * 0.045 + 500, wy * 0.045 + 500, 3, 0.55, 2.0));
                var w = _boundaryProximity[i];
                detail = detail * (1 - 0.5 * w) + (ridge * 2.0 - 1.0) * 0.5 * w;
            }

            var roughnessMult = tile.BoundaryType == PlateBoundaryType.Transform
                ? 1 + 0.6 * _boundaryProximity[i]
                : 1.0;
            var detailWeight = 0.16 * roughnessMult;

            var physical = plate.BaseElevation + _uplift[i] + detail * detailWeight;
            tile.Elevation = Math.Clamp(0.5 + physical, 0.0, 1.0);
        }
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

    // ================= Stage 4: Hydraulic erosion =================

    private void GenerateErosion()
    {
        var n = _width * _height;
        var elev = new double[n];
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
            elev[Idx(x, y)] = _map.GetTile(x, y).Elevation;

        const double inertia = 0.05;
        const double sedimentCapacityFactor = 4.0;
        const double minSlope = 0.01;
        const double erodeSpeed = 0.3;
        const double depositSpeed = 0.3;
        const double evaporateSpeed = 0.02;
        const double gravity = 4.0;
        const int erosionRadius = 2;
        const int maxLifetime = 30;

        var dropletCount = (int)(_width * _height * 1.6);
        for (var d = 0; d < dropletCount; d++)
        {
            var posX = _random.NextDouble() * (_width - 1);
            var posY = _random.NextDouble() * (_height - 1);
            double dirX = 0, dirY = 0, speed = 1, water = 1, sediment = 0;

            for (var lifetime = 0; lifetime < maxLifetime; lifetime++)
            {
                var nodeX = (int)posX;
                var nodeY = (int)posY;
                var cellX = posX - nodeX;
                var cellY = posY - nodeY;

                var (gradX, gradY, heightHere) = BilinearGradient(elev, nodeX, nodeY, cellX, cellY);

                dirX = dirX * inertia - gradX * (1 - inertia);
                dirY = dirY * inertia - gradY * (1 - inertia);
                var len = Math.Sqrt(dirX * dirX + dirY * dirY);
                if (len < 1e-8)
                {
                    dirX = _random.NextDouble() * 2 - 1;
                    dirY = _random.NextDouble() * 2 - 1;
                    len = Math.Max(1e-8, Math.Sqrt(dirX * dirX + dirY * dirY));
                }
                dirX /= len; dirY /= len;

                var newPosX = posX + dirX;
                var newPosY = posY + dirY;
                if (newPosX < 0 || newPosX >= _width - 1 || newPosY < 0 || newPosY >= _height - 1) break;

                var newNodeX = (int)newPosX;
                var newNodeY = (int)newPosY;
                var (_, _, newHeight) = BilinearGradient(elev, newNodeX, newNodeY, newPosX - newNodeX, newPosY - newNodeY);
                var deltaHeight = newHeight - heightHere;

                var capacity = Math.Max(-deltaHeight, minSlope) * speed * water * sedimentCapacityFactor;

                if (sediment > capacity || deltaHeight > 0)
                {
                    var depositAmount = deltaHeight > 0 ? Math.Min(deltaHeight, sediment) : (sediment - capacity) * depositSpeed;
                    sediment -= depositAmount;
                    DepositAt(elev, nodeX, nodeY, cellX, cellY, depositAmount);
                }
                else
                {
                    var erodeAmount = Math.Min((capacity - sediment) * erodeSpeed, -deltaHeight);
                    ErodeAt(elev, nodeX, nodeY, erodeAmount, erosionRadius);
                    sediment += erodeAmount;
                }

                speed = Math.Sqrt(Math.Max(0, speed * speed + deltaHeight * -gravity));
                water *= 1 - evaporateSpeed;
                posX = newPosX; posY = newPosY;
                if (water < 0.01) break;
            }
        }

        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
            _map.GetTile(x, y).Elevation = Math.Clamp(elev[Idx(x, y)], 0.0, 1.0);

        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            var sumSqDiff = 0.0;
            var count = 0;
            foreach (var neighbor in _map.GetNeighbors(x, y))
            {
                var dd = neighbor.Elevation - tile.Elevation;
                sumSqDiff += dd * dd;
                count++;
            }
            var variance = count > 0 ? sumSqDiff / count : 0;
            tile.Relief = Math.Clamp(Math.Sqrt(variance) * 6.0, 0.0, 1.0);
        }
    }

    private double GetElev(double[] elev, int x, int y)
    {
        x = Math.Clamp(x, 0, _width - 1);
        y = Math.Clamp(y, 0, _height - 1);
        return elev[Idx(x, y)];
    }

    private (double GradX, double GradY, double Height) BilinearGradient(double[] elev, int nodeX, int nodeY, double cellX, double cellY)
    {
        var h00 = GetElev(elev, nodeX, nodeY);
        var h10 = GetElev(elev, nodeX + 1, nodeY);
        var h01 = GetElev(elev, nodeX, nodeY + 1);
        var h11 = GetElev(elev, nodeX + 1, nodeY + 1);

        var gradX = (h10 - h00) * (1 - cellY) + (h11 - h01) * cellY;
        var gradY = (h01 - h00) * (1 - cellX) + (h11 - h10) * cellX;
        var height = h00 * (1 - cellX) * (1 - cellY) + h10 * cellX * (1 - cellY) + h01 * (1 - cellX) * cellY + h11 * cellX * cellY;
        return (gradX, gradY, height);
    }

    private void AddElev(double[] elev, int x, int y, double delta)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return;
        elev[Idx(x, y)] += delta;
    }

    private void DepositAt(double[] elev, int x, int y, double cellX, double cellY, double amount)
    {
        if (amount <= 0) return;
        AddElev(elev, x, y, amount * (1 - cellX) * (1 - cellY));
        AddElev(elev, x + 1, y, amount * cellX * (1 - cellY));
        AddElev(elev, x, y + 1, amount * (1 - cellX) * cellY);
        AddElev(elev, x + 1, y + 1, amount * cellX * cellY);
    }

    private void ErodeAt(double[] elev, int cx, int cy, double amount, int radius)
    {
        if (amount <= 0) return;
        var totalWeight = 0.0;
        var weights = new List<(int X, int Y, double W)>();
        for (var dx = -radius; dx <= radius; dx++)
        for (var dy = -radius; dy <= radius; dy++)
        {
            var x = cx + dx; var y = cy + dy;
            if (x < 0 || x >= _width || y < 0 || y >= _height) continue;
            var d = Math.Sqrt(dx * dx + dy * dy);
            if (d > radius) continue;
            var w = radius - d;
            weights.Add((x, y, w));
            totalWeight += w;
        }
        if (totalWeight <= 0) return;
        foreach (var (x, y, w) in weights)
            elev[Idx(x, y)] -= amount * (w / totalWeight);
    }

    // ================= Stage 5: Terrain bands =================

    private void GenerateTerrain()
    {
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            var e = tile.Elevation;
            tile.Terrain = e switch
            {
                _ when e < OceanCutoff => TerrainType.Ocean,
                _ when e < CoastCutoff => TerrainType.Coast,
                _ when e < PlainsCutoff => TerrainType.Plains,
                _ when e < GrasslandCutoff => TerrainType.Grassland,
                _ when e < HillsCutoff => TerrainType.Hills,
                _ => TerrainType.Mountains
            };
        }
    }

    // ================= Stage 6: Hydrology (flow-accumulation rivers/lakes) =================

    private void GenerateHydrology()
    {
        var n = _width * _height;
        var elev = new double[n];
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
            elev[Idx(x, y)] = _map.GetTile(x, y).Elevation;

        // Priority-flood depression filling (Barnes et al.): border tiles
        // are the initial drainage boundary; every interior cell is raised
        // to at least the height of the flood front that first reaches it.
        var filled = (double[])elev.Clone();
        var visited = new bool[n];
        var pq = new PriorityQueue<int, double>();
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            if (x != 0 && x != _width - 1 && y != 0 && y != _height - 1) continue;
            var i = Idx(x, y);
            visited[i] = true;
            pq.Enqueue(i, filled[i]);
        }

        while (pq.Count > 0)
        {
            var cur = pq.Dequeue();
            var (cx, cy) = FromIdx(cur);
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var nx = cx + dx; var ny = cy + dy;
                if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;
                var ni = Idx(nx, ny);
                if (visited[ni]) continue;
                filled[ni] = Math.Max(filled[ni], filled[cur]);
                visited[ni] = true;
                pq.Enqueue(ni, filled[ni]);
            }
        }

        var flowTarget = new int[n];
        Array.Fill(flowTarget, -1);
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var i = Idx(x, y);
            var best = -1;
            var bestDrop = 0.0;
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var nx = x + dx; var ny = y + dy;
                if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;
                var ni = Idx(nx, ny);
                var drop = filled[i] - filled[ni];
                if (drop > bestDrop) { bestDrop = drop; best = ni; }
            }
            flowTarget[i] = best;
        }

        // Flow accumulation via Braun & Willett linear propagation: process
        // tiles from highest to lowest filled elevation so every upstream
        // contributor is resolved before the tile that receives it.
        var order = Enumerable.Range(0, n).OrderByDescending(i => filled[i]).ToArray();
        var accum = new double[n];
        for (var i = 0; i < n; i++) accum[i] = 1.0;
        foreach (var i in order)
        {
            var target = flowTarget[i];
            if (target >= 0) accum[target] += accum[i];
        }

        const double basinEpsilon = 0.002;
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var i = Idx(x, y);
            var tile = _map.GetTile(x, y);
            if (tile.Terrain == TerrainType.Ocean) continue;
            if (filled[i] - elev[i] > basinEpsilon)
            {
                tile.IsLake = true;
                tile.Terrain = TerrainType.Lake;
                tile.Moisture = 1.0;
            }
        }

        var landLogAccum = new List<double>();
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            if (tile.Terrain is TerrainType.Ocean or TerrainType.Lake) continue;
            landLogAccum.Add(Math.Log(1 + accum[Idx(x, y)]));
        }
        var maxLog = landLogAccum.Count > 0 ? landLogAccum.Max() : 1.0;
        if (maxLog <= 0) maxLog = 1.0;

        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var i = Idx(x, y);
            var tile = _map.GetTile(x, y);
            tile.FlowAccumulation = tile.Terrain is TerrainType.Ocean or TerrainType.Lake
                ? 0
                : Math.Clamp(Math.Log(1 + accum[i]) / maxLog, 0, 1);
        }

        var landFlow = new List<double>();
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            if (tile.Terrain is TerrainType.Ocean or TerrainType.Lake) continue;
            landFlow.Add(tile.FlowAccumulation);
        }
        landFlow.Sort();
        double PercentileOf(double p) => landFlow.Count == 0 ? 1.0 : landFlow[(int)Math.Clamp(p * (landFlow.Count - 1), 0, landFlow.Count - 1)];
        var streamCutoff = PercentileOf(0.95);
        var riverCutoff = PercentileOf(0.98);

        _deltaCandidate = new bool[n];
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var i = Idx(x, y);
            var tile = _map.GetTile(x, y);
            if (tile.Terrain is TerrainType.Ocean or TerrainType.Lake) continue;
            if (tile.FlowAccumulation < streamCutoff) continue;

            tile.IsRiver = true;
            tile.Terrain = TerrainType.River;
            tile.Moisture = Math.Max(tile.Moisture, 0.8);

            if (tile.FlowAccumulation < riverCutoff) continue;
            var target = flowTarget[i];
            if (target < 0) continue;
            var (tx, ty) = FromIdx(target);
            var targetTile = _map.GetTile(tx, ty);
            if (targetTile.Terrain != TerrainType.Ocean && targetTile.Terrain != TerrainType.Coast) continue;

            for (var dx = -2; dx <= 2; dx++)
            for (var dy = -2; dy <= 2; dy++)
            {
                var mx = x + dx; var my = y + dy;
                if (mx < 0 || mx >= _width || my < 0 || my >= _height) continue;
                if (Dist(x, y, mx, my) > 2) continue;
                _deltaCandidate[Idx(mx, my)] = true;
            }
        }
    }

    // ================= Stage 7: Climate (wind bands + rain shadow) =================

    private void GenerateClimate()
    {
        var n = _width * _height;
        var distToOcean = new int[n];
        Array.Fill(distToOcean, -1);
        var q = new Queue<int>();
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            if (tile.Terrain is not (TerrainType.Ocean or TerrainType.Coast)) continue;
            var i = Idx(x, y);
            distToOcean[i] = 0;
            q.Enqueue(i);
        }
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            var (cx, cy) = FromIdx(cur);
            for (var dx = -1; dx <= 1; dx++)
            for (var dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                var nx = cx + dx; var ny = cy + dy;
                if (nx < 0 || nx >= _width || ny < 0 || ny >= _height) continue;
                var ni = Idx(nx, ny);
                if (distToOcean[ni] != -1) continue;
                distToOcean[ni] = distToOcean[cur] + 1;
                q.Enqueue(ni);
            }
        }
        var maxDist = 1;
        foreach (var d in distToOcean) if (d > maxDist) maxDist = d;

        var equatorLine = _height / 2.0;

        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            var i = Idx(x, y);
            var distFromEquator = Math.Abs(y - equatorLine) / equatorLine;

            tile.Climate = (tile.Elevation, distFromEquator) switch
            {
                (>= HillsCutoff, _) => ClimateZone.Highland,
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

            if (tile.IsRiver || tile.IsLake) continue;

            // Stylized flattened 3-band prevailing wind (not a rotating-sphere
            // model): equatorial and polar bands are easterly, mid-latitude
            // is westerly. windX/windY is the direction the wind BLOWS
            // toward; rain shadow looks upwind (the opposite direction).
            double windX = distFromEquator is < 0.33 or > 0.66 ? -1 : 1;
            const double windY = 0;

            var maxUpwind = tile.Elevation;
            double rx = x, ry = y;
            for (var step = 1; step <= 20; step++)
            {
                rx -= windX; ry -= windY;
                var ix = (int)Math.Round(rx);
                var iy = (int)Math.Round(ry);
                if (ix < 0 || ix >= _width || iy < 0 || iy >= _height) break;
                // Distant peaks cast a weaker shadow than nearby ones - without
                // this falloff, any tall mountain anywhere in a 20-tile band
                // dries out everything downwind of it regardless of distance,
                // which was turning most of the map arid instead of just the
                // terrain genuinely in a mountain's lee.
                var e = _map.GetTile(ix, iy).Elevation - step * 0.02;
                if (e > maxUpwind) maxUpwind = e;
            }
            var windwardBlock = Math.Max(0, maxUpwind - tile.Elevation);

            var oceanProximity = 1.0 - Math.Clamp((double)distToOcean[i] / maxDist, 0, 1);
            var latitudeHumidity = tile.Climate switch
            {
                ClimateZone.Tropical => 0.85,
                ClimateZone.Dry => 0.35,
                ClimateZone.Temperate => 0.6,
                ClimateZone.Cold => 0.5,
                ClimateZone.Highland => 0.5,
                _ => 0.5
            };
            var jitter = _random.NextDouble(-0.05, 0.05);
            var moisture = oceanProximity * 0.5 + latitudeHumidity * 0.5 - windwardBlock * 0.7 + jitter;
            tile.Moisture = Math.Clamp(moisture, 0, 1);
        }

        ClimateZones = _map.GetAllTiles()
            .GroupBy(t => t.Climate)
            .Select(g => new ClimateData
            {
                Zone = g.Key,
                BaseTemperature = g.Average(t => t.Temperature),
                AverageRainfall = g.Average(t => t.Moisture),
                TemperatureVariation = StdDev(g.Select(t => t.Temperature)),
                VegetationPotential = g.Average(t => t.Moisture) * (g.Key == ClimateZone.Dry ? 0.3 : 1.0)
            })
            .ToList();
    }

    private static double StdDev(IEnumerable<double> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return 0;
        var mean = list.Average();
        return Math.Sqrt(list.Average(v => (v - mean) * (v - mean)));
    }

    // ================= Stage 8: Expanded terrain taxonomy =================

    private void GenerateSpecialTerrain()
    {
        var craterCount = PoissonSample(_random, (double)(_width * _height) / 40000.0);
        for (var c = 0; c < craterCount; c++)
        {
            var cx = _random.Next(0, _width);
            var cy = _random.Next(0, _height);
            if (_map.GetTile(cx, cy).Terrain == TerrainType.Ocean) continue;
            var radius = _random.Next(2, 5);
            for (var dx = -radius; dx <= radius; dx++)
            for (var dy = -radius; dy <= radius; dy++)
            {
                var x = cx + dx; var y = cy + dy;
                if (x < 0 || x >= _width || y < 0 || y >= _height) continue;
                var d = Math.Sqrt(dx * dx + dy * dy);
                if (d > radius * 0.4) continue;
                _map.GetTile(x, y).Terrain = TerrainType.Crater;
            }
        }

        var equatorHalf = _height / 2.0;
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            if (tile.Terrain == TerrainType.Crater) continue;

            var distFromEquator = Math.Abs(y - equatorHalf) / equatorHalf;

            if (tile.Terrain != TerrainType.Ocean &&
                ((tile.Climate == ClimateZone.Cold && tile.Elevation >= HillsCutoff) ||
                 (distFromEquator > 0.85 && tile.Elevation >= CoastCutoff)))
            {
                tile.Terrain = TerrainType.Glacier;
                continue;
            }

            if (tile.IsVolcanic && tile.Elevation >= CoastCutoff && tile.Elevation < PlainsCutoff)
            {
                var oceanNeighbors = _map.GetNeighbors(x, y).Count(t => t.Terrain == TerrainType.Ocean);
                if (oceanNeighbors >= 5)
                {
                    tile.Terrain = TerrainType.VolcanicIsland;
                    continue;
                }
            }

            if (tile.IsVolcanic && tile.Elevation >= PlainsCutoff && tile.Elevation < HillsCutoff && _random.NextDouble() < 0.2)
            {
                tile.Terrain = TerrainType.GeothermalField;
                continue;
            }

            if (tile.Terrain == TerrainType.Ocean && tile.Elevation >= OceanCutoff - 0.05 && distFromEquator < 0.35)
            {
                var nearCoast = _map.GetNeighbors(x, y).Any(t => t.Terrain == TerrainType.Coast);
                if (nearCoast)
                {
                    tile.Terrain = TerrainType.CoralReef;
                    continue;
                }
            }

            if (tile.Terrain == TerrainType.Coast && tile.Relief > 0.5 && distFromEquator > 0.55)
            {
                tile.Terrain = TerrainType.Fjord;
                continue;
            }

            if (!tile.IsRiver && tile.FlowAccumulation > 0.3 && tile.Relief > 0.55 && tile.Elevation >= PlainsCutoff)
            {
                tile.Terrain = TerrainType.Canyon;
                continue;
            }

            if (tile.Climate == ClimateZone.Dry && tile.Relief > 0.5 && tile.Moisture < 0.2 &&
                tile.Terrain is TerrainType.Plains or TerrainType.Grassland or TerrainType.Hills)
            {
                tile.Terrain = TerrainType.Badlands;
                continue;
            }

            if (tile.Climate == ClimateZone.Dry && tile.Moisture < 0.15 && tile.Relief <= 0.5 &&
                tile.Terrain is TerrainType.Plains or TerrainType.Grassland)
            {
                tile.Terrain = TerrainType.Desert;
                continue;
            }

            if (_deltaCandidate[Idx(x, y)] && tile.Terrain is TerrainType.Plains or TerrainType.Grassland or TerrainType.Coast)
            {
                tile.Terrain = TerrainType.Delta;
            }
        }
    }

    // ================= Downstream stages (kept, re-tuned for the new elevation scale) =================

    private void GenerateSwamps()
    {
        for (var x = 0; x < _width; x++)
        for (var y = 0; y < _height; y++)
        {
            var tile = _map.GetTile(x, y);
            if (tile.Terrain is not (TerrainType.Plains or TerrainType.Grassland)) continue;
            if (tile.Elevation > CoastCutoff + 0.08 || tile.Moisture < 0.7) continue;

            var nearWater = _map.GetNeighbors(x, y).Any(n => n.IsRiver || n.IsLake);
            var chance = nearWater ? 0.4 : 0.05;
            if (_random.NextDouble() < chance)
            {
                tile.Terrain = TerrainType.Swamp;
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
                (TerrainType.Ocean, _, _) => BiomeType.Marine,
                (TerrainType.CoralReef, _, _) => BiomeType.Marine,
                (TerrainType.Coast, _, _) => BiomeType.Mediterranean,
                (TerrainType.Fjord, _, _) => BiomeType.Tundra,
                (TerrainType.Lake, _, _) => BiomeType.Wetland,
                (TerrainType.Swamp, _, _) => BiomeType.Wetland,
                (TerrainType.Delta, _, _) => BiomeType.Wetland,
                (TerrainType.Glacier, _, _) => BiomeType.PolarDesert,
                (TerrainType.Mountains, _, _) => BiomeType.Alpine,
                (TerrainType.VolcanicIsland, _, _) => BiomeType.VolcanicWasteland,
                (TerrainType.GeothermalField, _, _) => BiomeType.VolcanicWasteland,
                (TerrainType.Badlands, _, _) => BiomeType.Steppe,
                (TerrainType.Canyon, _, _) => BiomeType.Steppe,
                (TerrainType.Desert, _, _) => BiomeType.Desert,
                (_, ClimateZone.Tropical, >= 0.7) => BiomeType.TropicalRainforest,
                (_, ClimateZone.Tropical, _) => BiomeType.TropicalSavanna,
                (_, ClimateZone.Dry, >= 0.2) => BiomeType.Steppe,
                (_, ClimateZone.Dry, _) => BiomeType.Desert,
                (_, ClimateZone.Temperate, >= 0.5) => BiomeType.TemperateForest,
                (_, ClimateZone.Temperate, _) => BiomeType.TemperateGrassland,
                (_, ClimateZone.Cold, >= 0.4) => BiomeType.Taiga,
                (_, ClimateZone.Cold, >= 0.2) => BiomeType.Tundra,
                (_, ClimateZone.Cold, _) => BiomeType.PolarDesert,
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

            if (tile.Terrain is TerrainType.Hills or TerrainType.Mountains or TerrainType.Canyon or TerrainType.GeothermalField)
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

            if (tile.Terrain is TerrainType.River or TerrainType.Lake or TerrainType.Coast or TerrainType.Delta or TerrainType.Fjord)
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
