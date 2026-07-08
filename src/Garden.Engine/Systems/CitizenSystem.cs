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
    private readonly SettlementManager _settlementManager;
    private readonly ConstructionSystem _constructionSystem;
    private long _nextExecutionTick;

    public string Name => "CitizenSystem";
    public long IntervalTicks => 1;
    public long NextExecutionTick => _nextExecutionTick;

    public CitizenSystem(
        WorldState worldState,
        IEventBus eventBus,
        ILogger<CitizenSystem> logger,
        PopulationManager populationManager,
        SettlementManager settlementManager,
        ConstructionSystem constructionSystem)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
        _populationManager = populationManager;
        _settlementManager = settlementManager;
        _constructionSystem = constructionSystem;
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

        // Health has no other source of recovery, so without this a citizen
        // whose needs are consistently well met would still ratchet toward
        // death from pure passive decay - no amount of food/water ever
        // undoes the damage. A well-rested, fed, and hydrated body heals.
        var isThriving = citizen.Needs.Hunger < HungerWarningThreshold
            && citizen.Needs.Thirst < ThirstWarningThreshold
            && citizen.Needs.Energy > EnergyWarningThreshold
            && citizen.Needs.Warmth > WarmthWarningThreshold;
        citizen.Needs.Health = isThriving
            ? Math.Min(MaxHealth, citizen.Needs.Health + 0.08)
            : Math.Max(0, citizen.Needs.Health - 0.01);

        var tile = _worldState.Map.GetTile(citizen.TileX, citizen.TileY);
        var effectiveTemperature = tile.Temperature + ShelterInsulationBonus(citizen);
        var targetWarmth = Math.Clamp(effectiveTemperature / 30.0 * 100, 0, 100);
        citizen.Needs.Warmth += (targetWarmth - citizen.Needs.Warmth) * 0.02;
    }

    private double ShelterInsulationBonus(Citizen citizen)
    {
        if (citizen.HomeSettlementId == null) return 0;

        var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
        if (settlement == null) return 0;

        var hasShelter = settlement.Buildings.Any(b =>
            b.BuildingType is BuildingTypes.Shelter or BuildingTypes.House
            && b.Status == BuildingStatus.Completed);

        // A roof and a fire take the edge off the cold even away from home -
        // representing warm clothing/supplies a settlement provides its members.
        return hasShelter ? 18.0 : 0.0;
    }

    private void CheckHealth(Citizen citizen, long tick)
    {
        var hungerLoss = citizen.Needs.Hunger >= HungerCriticalThreshold ? 1.0 : 0.0;
        var thirstLoss = citizen.Needs.Thirst >= ThirstCriticalThreshold ? 2.0 : 0.0;
        var coldLoss = citizen.Needs.Warmth <= WarmthCriticalThreshold ? 0.5 : 0.0;
        var exhaustionLoss = citizen.Needs.Energy <= EnergyCriticalThreshold ? 0.3 : 0.0;

        citizen.Needs.Health -= hungerLoss + thirstLoss + coldLoss + exhaustionLoss;

        if (citizen.Needs.Health <= 0)
        {
            // Attribute death to whichever need was doing the most damage,
            // instead of always reporting "Starvation" regardless of cause.
            var cause = new[]
            {
                (Cause: "Dehydration", Loss: thirstLoss),
                (Cause: "Starvation", Loss: hungerLoss),
                (Cause: "Exposure", Loss: coldLoss),
                (Cause: "Exhaustion", Loss: exhaustionLoss)
            }.OrderByDescending(x => x.Loss).First().Cause;

            Die(citizen, tick, cause);
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

        // Below critical but past the warning line: top needs up before
        // returning to work. Without this, settled citizens gathering or
        // building never break away until a need is fully critical - by
        // which point thirst (which rises faster than hunger) is often
        // critical too, and the compounding health drain from multiple
        // simultaneous critical needs outpaces what a single trip can fix.
        var thirstWarning = citizen.Needs.Thirst >= ThirstWarningThreshold;
        var hungerWarning = citizen.Needs.Hunger >= HungerWarningThreshold;
        var energyWarning = citizen.Needs.Energy <= EnergyWarningThreshold;

        if (thirstWarning || hungerWarning || energyWarning)
        {
            if (drinkScore >= eatScore && drinkScore >= restScore)
            {
                citizen.CurrentGoal = "FindWater";
                citizen.CurrentActivity = "Seeking Water";
            }
            else if (eatScore >= restScore)
            {
                citizen.CurrentGoal = "FindFood";
                citizen.CurrentActivity = "Foraging";
            }
            else
            {
                citizen.CurrentGoal = "Rest";
                citizen.CurrentActivity = "Resting";
            }
            return;
        }

        if (citizen.HomeSettlementId == null)
        {
            var nearby = _settlementManager.FindNearbySettlement(citizen.TileX, citizen.TileY, 15);
            if (nearby != null)
            {
                _settlementManager.JoinSettlement(nearby, citizen);
                citizen.CurrentGoal = "JoinSettlement";
                citizen.CurrentActivity = "Joining Settlement";
                return;
            }

            // Personality traits are rolled on a 0-10 scale (see SpawnSystem).
            var communityUrge = citizen.Personality.Compassion + (10 - citizen.Personality.Introversion);
            // Require water within a short walk - a settlement founded far
            // from any water source dooms its members to a losing race
            // against thirst on every supply run for the rest of its life.
            var nearWater = Pathfinder.FindNearestPath(_worldState.Map, citizen.TileX, citizen.TileY, HasWaterAt, maxRadius: 8).Count > 0;
            if (communityUrge > 11 && citizen.Needs.Energy > 30 && nearWater)
            {
                var name = GenerateSettlementName();
                var settlement = _settlementManager.FoundSettlement(
                    citizen, name, citizen.TileX, citizen.TileY,
                    _worldState.CurrentTime.Tick);
                citizen.CurrentGoal = "BuildSettlement";
                citizen.CurrentActivity = "Founding Settlement";
                return;
            }
        }

        if (citizen.HomeSettlementId != null)
        {
            var settlement = _worldState.Settlements
                .FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
            if (settlement != null)
            {
                var incomplete = settlement.Buildings
                    .FirstOrDefault(b => b.Status == BuildingStatus.Planned
                        && Math.Abs(b.TileX - citizen.TileX) + Math.Abs(b.TileY - citizen.TileY) <= 3);
                if (incomplete != null)
                {
                    _constructionSystem.AssignWorker(incomplete, citizen);
                    citizen.CurrentGoal = "Build";
                    citizen.CurrentActivity = $"Building {incomplete.BuildingType}";
                    return;
                }

                var neededBuilding = GetNextNeededBuilding(settlement);
                if (_settlementManager.HasResourcesFor(
                    new Building { BuildingType = neededBuilding }, settlement))
                {
                    _settlementManager.DeductResources(
                        new Building { BuildingType = neededBuilding }, settlement);
                    _constructionSystem.PlanBuilding(
                        settlement, neededBuilding,
                        settlement.TileX + settlement.Buildings.Count % 5 - 2,
                        settlement.TileY + settlement.Buildings.Count / 5 - 2);
                    citizen.CurrentGoal = "Build";
                    citizen.CurrentActivity = $"Planning {neededBuilding}";
                    return;
                }

                if (GetNeededMaterial(settlement) != null)
                {
                    citizen.CurrentGoal = "GatherResources";
                    citizen.CurrentActivity = "Gathering Materials";
                    return;
                }
            }
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

        if (citizen.CurrentGoal == "FindWater" && HasWaterAccess(citizen, tile))
        {
            Drink(citizen);
            return;
        }

        if (citizen.CurrentGoal == "FindFood" && HasFoodAt(tile))
        {
            Eat(citizen);
            return;
        }

        if (citizen.CurrentGoal == "GatherResources" && citizen.HomeSettlementId != null)
        {
            var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
            var material = settlement != null ? GetNeededMaterial(settlement) : null;
            if (settlement != null && material != null
                && MaterialToResource.TryGetValue(material, out var resType))
            {
                var deposit = tile.Resources.FirstOrDefault(r => r.Type == resType && r.Quantity > 0);
                if (deposit != null)
                {
                    Gather(citizen, settlement, deposit, material);
                    return;
                }
            }
        }

        var path = FindTargetPath(citizen);
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

    /// <summary>
    /// Finds a path toward the citizen's current goal using a single
    /// early-exit BFS per call (see Pathfinder.FindNearestPath), rather than
    /// scanning every tile on the map and running a full A* search against
    /// each one just to test reachability.
    /// </summary>
    private List<(int X, int Y)> FindTargetPath(Citizen citizen)
    {
        var map = _worldState.Map;

        if (citizen.CurrentGoal == "FindWater")
        {
            var path = Pathfinder.FindNearestPath(map, citizen.TileX, citizen.TileY, t => HasWaterAccess(citizen, t));
            if (path.Count > 0) return path;
        }

        if (citizen.CurrentGoal == "FindFood")
        {
            var path = Pathfinder.FindNearestPath(map, citizen.TileX, citizen.TileY, HasFoodAt);
            if (path.Count > 0) return path;
        }

        if (citizen.CurrentGoal == "GatherResources" && citizen.HomeSettlementId != null)
        {
            var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
            var material = settlement != null ? GetNeededMaterial(settlement) : null;
            if (material != null && MaterialToResource.TryGetValue(material, out var resType))
            {
                var path = Pathfinder.FindNearestPath(map, citizen.TileX, citizen.TileY,
                    t => t.Resources.Any(r => r.Type == resType && r.Quantity > 0));
                if (path.Count > 0) return path;
            }
        }

        var dx = System.Random.Shared.Next(-8, 9);
        var dy = System.Random.Shared.Next(-8, 9);
        var targetX = Math.Clamp(citizen.TileX + dx, 0, map.Width - 1);
        var targetY = Math.Clamp(citizen.TileY + dy, 0, map.Height - 1);
        return Pathfinder.FindNearestPath(map, citizen.TileX, citizen.TileY,
            t => t.X == targetX && t.Y == targetY);
    }

    private void Eat(Citizen citizen)
    {
        var tile = _worldState.Map.GetTile(citizen.TileX, citizen.TileY);

        // Prefer stored settlement food (from farming/foraging deposits) if a member.
        if (citizen.HomeSettlementId != null)
        {
            var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
            if (settlement != null && settlement.Storage.GetQuantity("Food") >= 1)
            {
                var stored = settlement.Storage.Remove("Food", 15);
                citizen.Needs.Hunger = Math.Max(0, citizen.Needs.Hunger - stored);
                citizen.Needs.Energy = Math.Min(MaxEnergy, citizen.Needs.Energy + stored * 0.3);
                citizen.CurrentActivity = "Eating (Stored Food)";

                _eventBus.Publish(new CitizenAteEvent
                {
                    Tick = _worldState.CurrentTime.Tick,
                    CitizenId = citizen.Id,
                    CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                    FoodSource = "Stored",
                    Amount = stored
                });
                return;
            }
        }

        var foodSource = tile.Resources.FirstOrDefault(r => r.Type == ResourceType.WildPlants);
        if (foodSource != null && foodSource.Quantity > 0)
        {
            var amount = Math.Min(15, foodSource.Quantity);
            foodSource.Quantity -= amount;
            citizen.Needs.Hunger = Math.Max(0, citizen.Needs.Hunger - amount);
            citizen.Needs.Energy = Math.Min(MaxEnergy, citizen.Needs.Energy + amount * 0.3);
            citizen.CurrentActivity = "Foraging";

            _eventBus.Publish(new CitizenAteEvent
            {
                Tick = _worldState.CurrentTime.Tick,
                CitizenId = citizen.Id,
                CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                FoodSource = "WildPlants",
                Amount = amount
            });
            return;
        }

        var isWater = tile.IsRiver || tile.IsLake || tile.Terrain is TerrainType.Coast;
        if (isWater || CanForageOrHunt(tile))
        {
            // Hunting/fishing/foraging - success scales with perception/dexterity,
            // yields less than a proper harvest but never fully runs out.
            var skill = (citizen.Attributes.Perception + citizen.Attributes.Dexterity) / 20.0;
            var success = System.Random.Shared.NextDouble() < Math.Clamp(0.35 + skill * 0.3, 0.2, 0.85);
            citizen.CurrentActivity = isWater ? "Fishing" : tile.Terrain == TerrainType.Forest ? "Hunting" : "Foraging";

            if (success)
            {
                var amount = 6 + System.Random.Shared.NextDouble() * 4;
                citizen.Needs.Hunger = Math.Max(0, citizen.Needs.Hunger - amount);
                citizen.Needs.Energy = Math.Min(MaxEnergy, citizen.Needs.Energy + amount * 0.2);

                _eventBus.Publish(new CitizenAteEvent
                {
                    Tick = _worldState.CurrentTime.Tick,
                    CitizenId = citizen.Id,
                    CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                    FoodSource = isWater ? "Fish" : tile.Terrain == TerrainType.Forest ? "Game" : "Forage",
                    Amount = amount
                });
            }
        }
    }

    private static readonly Dictionary<string, ResourceType> MaterialToResource = new()
    {
        ["Wood"] = ResourceType.Trees,
        ["Stone"] = ResourceType.Stone,
        ["Clay"] = ResourceType.Clay,
    };

    private static string GetNextNeededBuilding(Settlement settlement)
    {
        bool Has(string type) => settlement.Buildings
            .Any(b => b.BuildingType == type && b.Status != BuildingStatus.Ruined);

        if (!Has(BuildingTypes.Storage)) return BuildingTypes.Storage;
        if (!Has(BuildingTypes.Well)) return BuildingTypes.Well;
        if (!Has(BuildingTypes.Farm)) return BuildingTypes.Farm;
        if (!settlement.HasAvailableHousing) return BuildingTypes.House;
        if (!Has(BuildingTypes.Workshop)) return BuildingTypes.Workshop;
        return BuildingTypes.House;
    }

    private static string? GetNeededMaterial(Settlement settlement)
    {
        var neededBuilding = GetNextNeededBuilding(settlement);
        var costs = BuildingTypes.GetCost(neededBuilding);

        return costs
            .Select(c => (c.Material, Shortfall: c.Amount - settlement.Storage.GetQuantity(c.Material)))
            .Where(c => c.Shortfall > 0)
            .OrderByDescending(c => c.Shortfall)
            .Select(c => c.Material)
            .FirstOrDefault();
    }

    private void Gather(Citizen citizen, Settlement settlement, ResourceDeposit deposit, string material)
    {
        var amount = Math.Min(10, deposit.Quantity);
        deposit.Quantity -= amount;
        settlement.Storage.Add(material, amount);
        citizen.Needs.Energy = Math.Max(0, citizen.Needs.Energy - 1.0);
        citizen.CurrentActivity = $"Gathering {material}";
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
            return;
        }

        if (HasWellAccess(citizen))
        {
            var amount = 10.0;
            citizen.Needs.Thirst = Math.Max(0, citizen.Needs.Thirst - amount * 2);
            citizen.CurrentActivity = "Drinking (Well)";

            _eventBus.Publish(new CitizenDrankEvent
            {
                Tick = _worldState.CurrentTime.Tick,
                CitizenId = citizen.Id,
                CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                WaterSource = "Well",
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

    /// <summary>
    /// A tile has water access for a citizen if it's naturally wet, or if
    /// it's within a home settlement that has built a completed Well -
    /// otherwise a Well building is pointless busywork with no gameplay
    /// effect, and every settled citizen must trek back to a natural water
    /// body no matter how developed their settlement is.
    /// </summary>
    private bool HasWaterAccess(Citizen citizen, World.Entities.WorldTile tile)
    {
        if (HasWaterAt(tile)) return true;
        if (citizen.HomeSettlementId == null) return false;

        var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
        return settlement != null
            && settlement.IsWithinTerritory(tile.X, tile.Y)
            && settlement.Buildings.Any(b => b.BuildingType == BuildingTypes.Well && b.Status == BuildingStatus.Completed);
    }

    private bool HasWellAccess(Citizen citizen)
    {
        if (citizen.HomeSettlementId == null) return false;
        var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
        return settlement != null
            && settlement.IsWithinTerritory(citizen.TileX, citizen.TileY)
            && settlement.Buildings.Any(b => b.BuildingType == BuildingTypes.Well && b.Status == BuildingStatus.Completed);
    }

    private static bool HasFoodAt(World.Entities.WorldTile tile)
    {
        if (tile.Resources.Any(r => r.Type == ResourceType.WildPlants && r.Quantity > 0))
            return true;

        // Forest/grassland tiles support hunting/foraging even once formal
        // WildPlants deposits are depleted - real forests still have game.
        // Water tiles support fishing.
        if (tile.Terrain is TerrainType.Forest or TerrainType.Grassland or TerrainType.Plains)
            return CanForageOrHunt(tile);

        return tile.IsRiver || tile.IsLake || tile.Terrain is TerrainType.Coast;
    }

    private static bool CanForageOrHunt(World.Entities.WorldTile tile)
    {
        // Ambient wildlife/forage density - always available in small amounts,
        // richer where moisture supports plant/animal life.
        return tile.Moisture > 0.15;
    }

    private static string GenerateSettlementName()
    {
        var prefixes = new[] { "New", "Old", "North", "South", "East", "West", "Great", "Little", "Upper", "Lower", "Far", "Deep" };
        var stems = new[] { "haven", "dale", "ford", "brook", "ridge", "field", "wood", "gate", "ford", "bridge", "mill", "bury" };
        var p = prefixes[System.Random.Shared.Next(prefixes.Length)];
        var s = stems[System.Random.Shared.Next(stems.Length)];
        return $"{p}{s}";
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
