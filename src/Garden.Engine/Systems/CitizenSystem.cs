using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Core.World;
using Garden.Engine.Pathfinding;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;
using static Garden.World.Entities.CitizenNeeds;

namespace Garden.Engine.Systems;

public class CitizenSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<CitizenSystem> _logger;
    private readonly PopulationManager _populationManager;
    private long _nextExecutionTick;

    public string Name => "CitizenSystem";
    public long IntervalTicks => 1;
    public long NextExecutionTick => _nextExecutionTick;

    public CitizenSystem(
        WorldState worldState,
        IEventBus eventBus,
        ILogger<CitizenSystem> logger,
        PopulationManager populationManager)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
        _populationManager = populationManager;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;
        var alive = _worldState.Citizens.Where(c => c.IsAlive).ToList();

        foreach (var citizen in alive)
        {
            UpdateNeeds(citizen);
            CheckHealth(citizen, tick);
            if (!citizen.IsAlive) continue;

            MakeDecision(citizen);
            MoveTowardGoal(citizen);
        }

        _populationManager.UpdatePopulation(alive.Count(c => c.IsAlive));
        _nextExecutionTick = tick + IntervalTicks;
    }

    private void UpdateNeeds(Citizen citizen)
    {
        citizen.Needs.Hunger = Math.Min(MaxHunger, citizen.Needs.Hunger + 0.5);
        citizen.Needs.Thirst = Math.Min(MaxThirst, citizen.Needs.Thirst + 0.7);
        citizen.Needs.Energy = Math.Max(0, citizen.Needs.Energy - 0.3);
        citizen.Needs.Health = Math.Max(0, citizen.Needs.Health - 0.01);

        var tile = _worldState.Map.GetTile(citizen.TileX, citizen.TileY);
        var targetWarmth = Math.Clamp(tile.Temperature / 30.0 * 100, 0, 100);
        citizen.Needs.Warmth += (targetWarmth - citizen.Needs.Warmth) * 0.02;
    }

    private void CheckHealth(Citizen citizen, long tick)
    {
        if (citizen.Needs.Hunger >= HungerCriticalThreshold)
            citizen.Needs.Health -= 1.0;
        if (citizen.Needs.Thirst >= ThirstCriticalThreshold)
            citizen.Needs.Health -= 2.0;
        if (citizen.Needs.Warmth <= WarmthCriticalThreshold)
            citizen.Needs.Health -= 0.5;
        if (citizen.Needs.Energy <= EnergyCriticalThreshold)
            citizen.Needs.Health -= 0.3;

        if (citizen.Needs.Health <= 0)
        {
            Die(citizen, tick, "Starvation");
            return;
        }

        if (citizen.Age > 70 && System.Random.Shared.NextDouble() < 0.001 * (citizen.Age - 70))
        {
            Die(citizen, tick, "Old Age");
        }
    }

    private void MakeDecision(Citizen citizen)
    {
        var tile = _worldState.Map.GetTile(citizen.TileX, citizen.TileY);

        var drinkScore = Math.Max(0, citizen.Needs.Thirst - ThirstWarningThreshold) * 1.5;
        var eatScore = Math.Max(0, citizen.Needs.Hunger - HungerWarningThreshold) * 1.2;
        var restScore = Math.Max(0, EnergyWarningThreshold - citizen.Needs.Energy);
        var exploreScore = citizen.Personality.Curiosity * 0.1 + (citizen.Needs.Energy > 50 ? 5 : 0);

        var thirstCritical = citizen.Needs.Thirst >= ThirstCriticalThreshold;
        var hungerCritical = citizen.Needs.Hunger >= HungerCriticalThreshold;
        var energyCritical = citizen.Needs.Energy <= EnergyCriticalThreshold;

        if (thirstCritical)
        {
            citizen.CurrentGoal = "FindWater";
            citizen.CurrentActivity = "Seeking Water";
            return;
        }
        if (hungerCritical)
        {
            citizen.CurrentGoal = "FindFood";
            citizen.CurrentActivity = "Foraging";
            return;
        }
        if (energyCritical)
        {
            citizen.CurrentGoal = "Rest";
            citizen.CurrentActivity = "Resting";
            return;
        }

        if (drinkScore > eatScore && drinkScore > restScore && drinkScore > exploreScore)
        {
            citizen.CurrentGoal = "FindWater";
            citizen.CurrentActivity = "Seeking Water";
        }
        else if (eatScore > restScore && eatScore > exploreScore)
        {
            citizen.CurrentGoal = "FindFood";
            citizen.CurrentActivity = "Foraging";
        }
        else if (restScore > exploreScore)
        {
            citizen.CurrentGoal = "Rest";
            citizen.CurrentActivity = "Resting";
        }
        else
        {
            citizen.CurrentGoal = "Explore";
            citizen.CurrentActivity = "Exploring";
        }
    }

    private void MoveTowardGoal(Citizen citizen)
    {
        var tile = _worldState.Map.GetTile(citizen.TileX, citizen.TileY);

        if (citizen.CurrentGoal == "Rest")
        {
            Rest(citizen);
            return;
        }

        if (citizen.CurrentGoal == "FindWater" && HasWaterAt(tile))
        {
            Drink(citizen);
            return;
        }

        if (citizen.CurrentGoal == "FindFood" && HasFoodAt(tile))
        {
            Eat(citizen);
            return;
        }

        var target = FindTargetTile(citizen);
        if (target == null) return;

        var path = Pathfinder.FindPath(
            _worldState.Map, citizen.TileX, citizen.TileY,
            target.Value.X, target.Value.Y);

        if (path.Count > 1)
        {
            var next = path[1];
            var prevX = citizen.TileX;
            var prevY = citizen.TileY;
            citizen.TileX = next.X;
            citizen.TileY = next.Y;

            _eventBus.Publish(new CitizenMovedEvent
            {
                Tick = _worldState.CurrentTime.Tick,
                CitizenId = citizen.Id,
                CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                FromX = prevX,
                FromY = prevY,
                ToX = next.X,
                ToY = next.Y
            });
        }
    }

    private (int X, int Y)? FindTargetTile(Citizen citizen)
    {
        var tiles = _worldState.Map.GetAllTiles().ToList();

        if (citizen.CurrentGoal == "FindWater")
        {
            var water = tiles
                .Where(t => HasWaterAt(t) && Pathfinder.FindPath(
                    _worldState.Map, citizen.TileX, citizen.TileY, t.X, t.Y).Count > 0)
                .OrderBy(t => Math.Abs(t.X - citizen.TileX) + Math.Abs(t.Y - citizen.TileY))
                .FirstOrDefault();
            if (water != null) return (water.X, water.Y);
        }

        if (citizen.CurrentGoal == "FindFood")
        {
            var food = tiles
                .Where(t => HasFoodAt(t) && Pathfinder.FindPath(
                    _worldState.Map, citizen.TileX, citizen.TileY, t.X, t.Y).Count > 0)
                .OrderBy(t => Math.Abs(t.X - citizen.TileX) + Math.Abs(t.Y - citizen.TileY))
                .FirstOrDefault();
            if (food != null) return (food.X, food.Y);
        }

        var explore = tiles
            .Where(t => Math.Abs(t.X - citizen.TileX) + Math.Abs(t.Y - citizen.TileY) > 5)
            .OrderBy(_ => System.Random.Shared.Next())
            .FirstOrDefault();
        return explore != null ? (explore.X, explore.Y) : null;
    }

    private void Eat(Citizen citizen)
    {
        var tile = _worldState.Map.GetTile(citizen.TileX, citizen.TileY);

        var foodSource = tile.Resources.FirstOrDefault(r => r.Type == ResourceType.WildPlants);
        if (foodSource != null && foodSource.Quantity > 0)
        {
            var amount = Math.Min(15, foodSource.Quantity);
            foodSource.Quantity -= amount;
            citizen.Needs.Hunger = Math.Max(0, citizen.Needs.Hunger - amount);
            citizen.Needs.Energy = Math.Min(MaxEnergy, citizen.Needs.Energy + amount * 0.3);
            citizen.CurrentActivity = "Eating";

            _eventBus.Publish(new CitizenAteEvent
            {
                Tick = _worldState.CurrentTime.Tick,
                CitizenId = citizen.Id,
                CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                FoodSource = "WildPlants",
                Amount = amount
            });
        }
    }

    private void Drink(Citizen citizen)
    {
        var tile = _worldState.Map.GetTile(citizen.TileX, citizen.TileY);

        var waterSource = tile.Resources.FirstOrDefault(r => r.Type == ResourceType.FreshWater)
            ?? (tile.IsRiver || tile.IsLake || tile.Terrain == TerrainType.Coast
                ? new ResourceDeposit { Quantity = 1000, Type = ResourceType.FreshWater }
                : null);

        if (waterSource != null && waterSource.Quantity > 0)
        {
            var amount = Math.Min(10, waterSource.Quantity);
            if (waterSource.MaxCapacity > 0)
                waterSource.Quantity -= amount;
            citizen.Needs.Thirst = Math.Max(0, citizen.Needs.Thirst - amount * 2);
            citizen.CurrentActivity = "Drinking";

            _eventBus.Publish(new CitizenDrankEvent
            {
                Tick = _worldState.CurrentTime.Tick,
                CitizenId = citizen.Id,
                CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                WaterSource = tile.IsRiver ? "River" : tile.IsLake ? "Lake" : "Ground",
                Amount = amount
            });
        }
    }

    private void Rest(Citizen citizen)
    {
        citizen.Needs.Energy = Math.Min(MaxEnergy, citizen.Needs.Energy + 2.0);
        citizen.Needs.Hunger += 0.2;
        citizen.Needs.Thirst += 0.3;
        citizen.CurrentActivity = "Resting";

        _eventBus.Publish(new CitizenRestedEvent
        {
            Tick = _worldState.CurrentTime.Tick,
            CitizenId = citizen.Id,
            CitizenName = $"{citizen.FirstName} {citizen.LastName}"
        });
    }

    private static bool HasWaterAt(World.Entities.WorldTile tile)
    {
        return tile.IsRiver || tile.IsLake || tile.Terrain == TerrainType.Coast
            || tile.Resources.Any(r => r.Type == ResourceType.FreshWater && r.Quantity > 0);
    }

    private static bool HasFoodAt(World.Entities.WorldTile tile)
    {
        return tile.Resources.Any(r => r.Type == ResourceType.WildPlants && r.Quantity > 0)
            || tile.Terrain == TerrainType.Forest;
    }

    private void Die(Citizen citizen, long tick, string cause)
    {
        citizen.IsAlive = false;
        citizen.DeathTick = tick;
        citizen.CauseOfDeath = cause;
        citizen.CurrentActivity = "Dead";
        citizen.CurrentGoal = "None";

        _populationManager.RecordDeath(cause);

        _eventBus.Publish(new CitizenDiedEvent
        {
            Tick = tick,
            CitizenId = citizen.Id,
            CitizenName = $"{citizen.FirstName} {citizen.LastName}",
            CauseOfDeath = cause,
            AgeAtDeath = citizen.Age
        });

        _logger.LogInformation("{Name} died at age {Age} from {Cause}",
            $"{citizen.FirstName} {citizen.LastName}", citizen.Age, cause);
    }
}
