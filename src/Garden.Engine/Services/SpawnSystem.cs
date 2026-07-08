using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Core.World;
using Garden.Engine.Generators;
using Garden.Engine.Pathfinding;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class SpawnSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SpawnSystem> _logger;

    public SpawnSystem(WorldState worldState, IEventBus eventBus, ILogger<SpawnSystem> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public List<Citizen> SpawnInitialPopulation(int count)
    {
        var spawned = new List<Citizen>();
        var map = _worldState.Map;

        static bool IsHabitable(World.Entities.WorldTile t) => t.Terrain is not TerrainType.Ocean
            and not TerrainType.Lake
            and not TerrainType.Mountains
            and not TerrainType.River;

        var habitable = map.GetAllTiles().Where(IsHabitable).ToList();

        if (habitable.Count == 0)
        {
            _logger.LogWarning("No habitable tiles for spawning");
            return spawned;
        }

        var rng = new System.Random(_worldState.CurrentTime.GetHashCode() ^ count);
        var tick = _worldState.CurrentTime.Tick;

        // Prefer habitable tiles within a short walk of fresh water - a
        // lone citizen who spawns far from any water source is in trouble
        // before they've even made a decision.
        bool NearWater(World.Entities.WorldTile t) => Pathfinder.FindNearestPath(
            map, t.X, t.Y,
            w => w.IsRiver || w.IsLake || w.Terrain == TerrainType.Coast,
            maxRadius: 12).Count > 0;

        var goodSites = habitable.Where(NearWater).ToList();
        var siteSource = goodSites.Count > 0 ? goodSites : habitable;

        // Spawn in small clusters instead of scattering every citizen
        // independently across the whole continent - a lone citizen with
        // no one nearby has almost no chance of cooperating into a
        // settlement before something kills them. Clusters are spread out
        // with a minimum separation so several independent groups (and,
        // eventually, cultures) can emerge instead of everyone colliding
        // into one settlement immediately.
        const int targetClusterSize = 8;
        var clusterCount = Math.Max(1, (int)Math.Ceiling(count / (double)targetClusterSize));
        var minSeparation = Math.Max(12, Math.Min(map.Width, map.Height) / (clusterCount + 1));

        var shuffledSites = siteSource.OrderBy(_ => rng.Next()).ToList();
        var centers = new List<World.Entities.WorldTile>();
        foreach (var candidate in shuffledSites)
        {
            if (centers.Count >= clusterCount) break;
            if (centers.All(c => Math.Abs(c.X - candidate.X) + Math.Abs(c.Y - candidate.Y) >= minSeparation))
            {
                centers.Add(candidate);
            }
        }
        if (centers.Count == 0) centers.Add(shuffledSites[0]);
        // Not enough well-separated sites for the target cluster count (small
        // or cramped map) - fill remaining clusters ignoring the separation
        // constraint rather than leaving citizens unclustered.
        foreach (var candidate in shuffledSites)
        {
            if (centers.Count >= clusterCount) break;
            if (!centers.Contains(candidate)) centers.Add(candidate);
        }

        for (var i = 0; i < count; i++)
        {
            var center = centers[i % centers.Count];
            var tile = FindNearbyHabitableTile(center, habitable, rng) ?? center;
            var age = rng.Next(16, 50);

            var citizen = new Citizen
            {
                FirstName = NameGenerator.GenerateFirstName(),
                LastName = NameGenerator.GenerateLastName(),
                Age = age,
                BiologicalSex = rng.NextDouble() < 0.5 ? "Male" : "Female",
                TileX = tile.X,
                TileY = tile.Y,
                Stage = LifeStage.Adult,
                IsAlive = true,
                // BirthTick must agree with Age, since AgingSystem recomputes
                // Age from BirthTick every in-game day.
                BirthTick = tick - age * 24L * 30 * 12 - (long)(rng.NextDouble() * 24 * 30 * 12),
                Needs = new CitizenNeeds
                {
                    Hunger = rng.NextDouble() * 40,
                    Thirst = rng.NextDouble() * 40,
                    Energy = rng.NextDouble() * 30 + 50,
                    Warmth = rng.NextDouble() * 30 + 40,
                    Health = rng.NextDouble() * 20 + 75
                },
                Attributes = new CitizenAttributes
                {
                    Strength = rng.NextDouble() * 8 + 2,
                    Endurance = rng.NextDouble() * 8 + 2,
                    Intelligence = rng.NextDouble() * 8 + 2,
                    Dexterity = rng.NextDouble() * 8 + 2,
                    Perception = rng.NextDouble() * 8 + 2
                },
                Personality = new PersonalityTraits
                {
                    Curiosity = rng.NextDouble() * 10,
                    Patience = rng.NextDouble() * 10,
                    Aggression = rng.NextDouble() * 10,
                    Compassion = rng.NextDouble() * 10,
                    Diligence = rng.NextDouble() * 10,
                    Introversion = rng.NextDouble() * 10
                },
                CurrentActivity = "Exploring",
                CurrentGoal = "Survive"
            };

            _worldState.Citizens.Add(citizen);
            spawned.Add(citizen);

            _eventBus.Publish(new CitizenSpawnedEvent
            {
                Tick = tick,
                CitizenId = citizen.Id,
                CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                TileX = tile.X,
                TileY = tile.Y
            });
        }

        _logger.LogInformation("Spawned {Count} citizens", spawned.Count);
        return spawned;
    }

    /// <summary>
    /// Finds a habitable tile within a small radius of a cluster center, so
    /// citizens in the same cluster land near each other (never fully
    /// isolated) rather than on the exact same tile.
    /// </summary>
    private static World.Entities.WorldTile? FindNearbyHabitableTile(
        World.Entities.WorldTile center, List<World.Entities.WorldTile> habitable, System.Random rng)
    {
        var nearby = habitable
            .Where(t => Math.Abs(t.X - center.X) + Math.Abs(t.Y - center.Y) <= 5)
            .ToList();

        return nearby.Count > 0 ? nearby[rng.Next(nearby.Count)] : center;
    }
}
