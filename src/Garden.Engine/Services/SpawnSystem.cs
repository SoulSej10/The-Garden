using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Core.World;
using Garden.Engine.Generators;
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
        var habitable = _worldState.Map.GetAllTiles()
            .Where(t => t.Terrain is not TerrainType.Ocean
                and not TerrainType.Lake
                and not TerrainType.Mountains
                and not TerrainType.River)
            .ToList();

        if (habitable.Count == 0)
        {
            _logger.LogWarning("No habitable tiles for spawning");
            return spawned;
        }

        var rng = new System.Random(_worldState.CurrentTime.GetHashCode() ^ count);
        var tick = _worldState.CurrentTime.Tick;

        for (var i = 0; i < count && habitable.Count > 0; i++)
        {
            var idx = rng.Next(habitable.Count);
            var tile = habitable[idx];

            var citizen = new Citizen
            {
                FirstName = NameGenerator.GenerateFirstName(),
                LastName = NameGenerator.GenerateLastName(),
                Age = rng.Next(16, 50),
                BiologicalSex = rng.NextDouble() < 0.5 ? "Male" : "Female",
                TileX = tile.X,
                TileY = tile.Y,
                Stage = LifeStage.Adult,
                IsAlive = true,
                BirthTick = tick - (long)(rng.NextDouble() * 365 * 24),
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
}
