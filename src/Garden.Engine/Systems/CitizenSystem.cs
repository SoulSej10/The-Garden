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

        // Growth rebalancing finding: nothing anywhere removed a dead
        // citizen's Id from their settlement's MemberIds - not this
        // system's own Die() below, and not DiseaseSystem's separate death
        // path. Settlement.MemberIds was a permanently-growing "everyone
        // who ever lived here" count, not "current living population," and
        // HousingCapacity/HasAvailableHousing and ReproductionSystem's
        // food-per-capita divisor both read MemberIds.Count directly. Any
        // settlement with real mortality history (i.e. nearly all of them)
        // had its housing permanently reported "at capacity" and its real
        // food-per-capita permanently diluted by corpses still counted as
        // residents - a structural block on reproduction independent of
        // actual food or housing. Subscribing here (not duplicating the
        // cleanup in every death path) catches every CitizenDiedEvent
        // regardless of which system published it.
        eventBus.Subscribe<CitizenDiedEvent>(OnCitizenDied);
    }

    private void OnCitizenDied(CitizenDiedEvent e)
    {
        var settlement = _worldState.Settlements.FirstOrDefault(s => s.MemberIds.Contains(e.CitizenId));
        if (settlement == null) return;

        settlement.MemberIds.Remove(e.CitizenId);
        settlement.Population = settlement.MemberIds.Count;
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

    // How far a citizen will travel to join an existing settlement before
    // giving up and considering founding their own.
    private const int SettlementJoinTravelCap = 45;

    private void MakeDecision(Citizen citizen)
    {
        var tile = _worldState.Map.GetTile(citizen.TileX, citizen.TileY);

        var drinkScore = Math.Max(0, citizen.Needs.Thirst - ThirstWarningThreshold) * 1.5;
        var eatScore = Math.Max(0, citizen.Needs.Hunger - HungerWarningThreshold) * 1.2;
        var restScore = Math.Max(0, EnergyWarningThreshold - citizen.Needs.Energy);
        var exploreScore = citizen.Personality.Curiosity * 0.1 + (citizen.Needs.Energy > 50 ? 5 : 0);

        // RFC-001 (Week 3 Day 14): a citizen who is already afraid acts on
        // physiological needs sooner rather than waiting until they're fully
        // critical - a narrow, additive read of EmotionalState.Fear, not a
        // rewrite of this decision chain. 10 points is enough to matter
        // (thresholds are 15-80 apart) without ever making a merely-anxious
        // citizen treat a non-issue as an emergency.
        var fearUrgency = citizen.Emotions.Fear > 50.0 ? 10.0 : 0.0;
        var thirstCritical = citizen.Needs.Thirst >= ThirstCriticalThreshold - fearUrgency;
        var hungerCritical = citizen.Needs.Hunger >= HungerCriticalThreshold - fearUrgency;
        var energyCritical = citizen.Needs.Energy <= EnergyCriticalThreshold + fearUrgency;

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

        // Audit finding 3c: a completed Farm with no one ever planting seeds
        // just sits idle forever, because the warning-level need checks
        // below used to return before this settlement-work was ever
        // reached - any citizen under even mild hunger/thirst stress (which
        // is most of them, most of the time, once a settlement is
        // food-insecure) could never be steered toward planting, which is
        // exactly the situation planting exists to fix. Bounded to once per
        // citizen per day so it can't crowd out a citizen's own needs for
        // the rest of the day once seeds fall back below the 20 threshold.
        if (citizen.HomeSettlementId != null)
        {
            var day = _worldState.CurrentTime.Tick / 24;
            if (citizen.LastFarmWorkDay != day)
            {
                var homeSettlement = _worldState.Settlements
                    .FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
                var farmNeedingSeeds = homeSettlement?.Buildings
                    .FirstOrDefault(b => b.BuildingType == BuildingTypes.Farm
                        && b.Status == BuildingStatus.Completed
                        && b.Storage.GetQuantity("Seeds") < 20);
                if (farmNeedingSeeds != null)
                {
                    citizen.CurrentGoal = "FarmWork";
                    citizen.CurrentActivity = "Planting Crops";
                    return;
                }
            }
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
            // Always prefer an existing settlement over founding a new one -
            // a 15-tile join radius with no fallback meant most citizens on
            // a large map never discovered any existing settlement before
            // independently satisfying the founding condition themselves,
            // fragmenting a population of 50 into dozens of one- or
            // two-person camps too small to build, farm, or reproduce.
            var nearest = _settlementManager.FindNearestSettlement(citizen.TileX, citizen.TileY);
            if (nearest != null && nearest.IsWithinTerritory(citizen.TileX, citizen.TileY))
            {
                _settlementManager.JoinSettlement(nearest, citizen);
                citizen.CurrentGoal = "JoinSettlement";
                citizen.CurrentActivity = "Joining Settlement";
                return;
            }

            var distanceToNearest = nearest != null
                ? Math.Abs(nearest.TileX - citizen.TileX) + Math.Abs(nearest.TileY - citizen.TileY)
                : int.MaxValue;

            if (nearest != null && distanceToNearest <= SettlementJoinTravelCap)
            {
                citizen.CurrentGoal = "TravelToSettlement";
                citizen.CurrentActivity = $"Traveling to {nearest.Name}";
                return;
            }

            // No settlement is reachable within a practical travel distance -
            // consider founding a new one here.
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
                if (neededBuilding != null && _settlementManager.HasResourcesFor(
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

                // A completed Farm building with no one ever planting seeds
                // in it just sits idle forever - AgricultureSystem only
                // turns Seeds into Food, it never creates Seeds. Without
                // this, settlements have no real collective food source and
                // every citizen depends entirely on individual chance-based
                // foraging, which cannot support more than a couple of
                // people sharing the same small patch of land.
                var needsPlanting = settlement.Buildings
                    .FirstOrDefault(b => b.BuildingType == BuildingTypes.Farm
                        && b.Status == BuildingStatus.Completed
                        && b.Storage.GetQuantity("Seeds") < 20);
                if (needsPlanting != null)
                {
                    citizen.CurrentGoal = "FarmWork";
                    citizen.CurrentActivity = "Planting Crops";
                    return;
                }

                // Rebalancing audit finding 7: "care for family" and "heal
                // the sick" didn't exist as citizen behaviors at all - a
                // critically-ill citizen relied entirely on their own
                // MakeDecision, with no one else ever acting on their
                // behalf. Bounded to once per citizen per day, same pattern
                // as farm-planting, so it can't crowd out a caregiver's own
                // needs for the rest of the day.
                var careDay = _worldState.CurrentTime.Tick / 24;
                if (citizen.LastCareForFamilyDay != careDay)
                {
                    var sickRelative = GetSickFamilyMember(citizen);
                    if (sickRelative != null)
                    {
                        citizen.CurrentGoal = "CareForFamily";
                        citizen.CurrentActivity = $"Caring for {sickRelative.FirstName}";
                        return;
                    }
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
            // RFC-001 (Week 3 Day 14): Loneliness (EmotionSystem) targets a
            // high value specifically for citizens with no HomeSettlementId
            // - this is the fallback reached when such a citizen couldn't
            // find or found a settlement this tick either (see the
            // HomeSettlementId == null block above), so "give up looking for
            // a home for now and seek company instead" is a coherent low-
            // priority alternative to blind wandering, not a rewrite of the
            // settlement-seeking logic above it.
            if (citizen.Emotions.Loneliness > 40.0)
            {
                citizen.CurrentGoal = "Socialize";
                citizen.CurrentActivity = "Socializing";
            }
            else
            {
                citizen.CurrentGoal = "Explore";
                citizen.CurrentActivity = "Exploring";
            }
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

        if (citizen.CurrentGoal == "CareForFamily")
        {
            var sickRelative = GetSickFamilyMember(citizen);
            if (sickRelative != null && sickRelative.TileX == citizen.TileX && sickRelative.TileY == citizen.TileY)
            {
                sickRelative.Needs.Health = Math.Min(CitizenNeeds.MaxHealth, sickRelative.Needs.Health + 5.0);
                citizen.Needs.Energy = Math.Max(0, citizen.Needs.Energy - 1.0);
                citizen.LastCareForFamilyDay = _worldState.CurrentTime.Tick / 24;
                citizen.CurrentActivity = $"Caring for {sickRelative.FirstName}";
                return;
            }
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

        if (citizen.CurrentGoal == "FarmWork" && citizen.HomeSettlementId != null)
        {
            var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
            var farm = settlement?.Buildings.FirstOrDefault(b => b.BuildingType == BuildingTypes.Farm
                && b.Status == BuildingStatus.Completed
                && b.TileX == citizen.TileX && b.TileY == citizen.TileY);
            if (farm != null)
            {
                farm.Storage.Add("Seeds", 15);
                citizen.Needs.Energy = Math.Max(0, citizen.Needs.Energy - 1.5);
                citizen.CurrentActivity = "Planting Crops";
                citizen.LastFarmWorkDay = _worldState.CurrentTime.Tick / 24;
                return;
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

        if (citizen.CurrentGoal == "TravelToSettlement")
        {
            var nearest = _settlementManager.FindNearestSettlement(citizen.TileX, citizen.TileY);
            if (nearest != null)
            {
                var path = Pathfinder.FindNearestPath(map, citizen.TileX, citizen.TileY,
                    t => nearest.IsWithinTerritory(t.X, t.Y), maxRadius: 70);
                if (path.Count > 0) return path;
            }
        }

        if (citizen.CurrentGoal == "CareForFamily")
        {
            var sickRelative = GetSickFamilyMember(citizen);
            if (sickRelative != null)
            {
                var path = Pathfinder.FindNearestPath(map, citizen.TileX, citizen.TileY,
                    t => t.X == sickRelative.TileX && t.Y == sickRelative.TileY, maxRadius: 60);
                if (path.Count > 0) return path;
            }
        }

        if (citizen.CurrentGoal == "FarmWork" && citizen.HomeSettlementId != null)
        {
            // Audit finding 3c (companion fix): this case was previously
            // missing entirely, so a citizen given the FarmWork goal had no
            // way to actually travel to the farm - MoveTowardGoal's FarmWork
            // handling only fires when the citizen is already standing on
            // the farm's exact tile, and every other path fell through to
            // the random-wander fallback below, making a real planting trip
            // pure chance.
            var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
            var farm = settlement?.Buildings.FirstOrDefault(b => b.BuildingType == BuildingTypes.Farm
                && b.Status == BuildingStatus.Completed);
            if (farm != null)
            {
                var path = Pathfinder.FindNearestPath(map, citizen.TileX, citizen.TileY,
                    t => t.X == farm.TileX && t.Y == farm.TileY, maxRadius: 40);
                if (path.Count > 0) return path;
            }
        }

        if (citizen.CurrentGoal == "GatherResources" && citizen.HomeSettlementId != null)
        {
            var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId);
            var material = settlement != null ? GetNeededMaterial(settlement) : null;
            if (settlement != null && material != null && MaterialToResource.TryGetValue(material, out var resType))
            {
                // Search from the settlement's home base with a bounded
                // radius, not an unbounded search from the citizen's
                // current position - otherwise a gatherer chasing a scarce
                // resource can drift 50+ tiles from any known water source
                // over successive gather cycles, and by the time thirst
                // forces them to turn back it's already too late.
                var nearHome = Pathfinder.FindNearestPath(map, settlement.TileX, settlement.TileY,
                    t => t.Resources.Any(r => r.Type == resType && r.Quantity > 0), maxRadius: 30);
                if (nearHome.Count > 0)
                {
                    var target = nearHome[^1];
                    var path = Pathfinder.FindNearestPath(map, citizen.TileX, citizen.TileY,
                        t => t.X == target.X && t.Y == target.Y, maxRadius: 70);
                    if (path.Count > 0) return path;
                }
            }
        }

        // Audit finding (population collapse, live verification): this
        // wander target used to be offset from the citizen's OWN current
        // tile every tick with no pull toward home - for a settled citizen
        // that repeatedly falls back to Explore, each tick's random step
        // compounds on the last with nothing ever biasing them back,
        // producing a genuine random walk. Over the hundreds of ticks a
        // settlement survives, this let settled citizens end up dozens of
        // tiles from their settlement's stored food/water and any built
        // infrastructure, where they then starved or died of exposure alone
        // - directly observed live after the Phase 1 food-chain fixes still
        // left population collapsing by Year 2. Anchoring the wander
        // target on the home settlement (not the citizen's drifting
        // position) keeps a settled citizen's exploring bounded near home
        // and self-correcting if they've already drifted away.
        var homeSettlement = citizen.HomeSettlementId != null
            ? _worldState.Settlements.FirstOrDefault(s => s.Id == citizen.HomeSettlementId)
            : null;
        var anchorX = homeSettlement?.TileX ?? citizen.TileX;
        var anchorY = homeSettlement?.TileY ?? citizen.TileY;

        var dx = System.Random.Shared.Next(-8, 9);
        var dy = System.Random.Shared.Next(-8, 9);
        var targetX = Math.Clamp(anchorX + dx, 0, map.Width - 1);
        var targetY = Math.Clamp(anchorY + dy, 0, map.Height - 1);
        return Pathfinder.FindNearestPath(map, citizen.TileX, citizen.TileY,
            t => t.X == targetX && t.Y == targetY, maxRadius: 70);
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

    // Audit finding 3d: HasAvailableHousing only counts Completed houses,
    // so with no cap here the settlement queued (and paid Wood/Stone/Clay
    // for) another House the instant materials were affordable, even while
    // several previous House plans sat unbuilt and unstaffed - one
    // settlement in the audit's Year 2 run reached 164 total buildings
    // against 76 completed, starving Farm/Storage of the same materials.
    private const int MaxUnbuiltHousePlans = 2;
    private const int MaxUnbuiltFarmPlans = 1;

    private static string? GetNextNeededBuilding(Settlement settlement)
    {
        bool Has(string type) => settlement.Buildings
            .Any(b => b.BuildingType == type && b.Status != BuildingStatus.Ruined);

        if (!Has(BuildingTypes.Storage)) return BuildingTypes.Storage;

        // Audit finding 3b: Farm is the only building that actually
        // produces food, but previously sat third in this queue behind
        // Storage and Well - a fresh settlement's entire early materials
        // budget went to buildings that don't feed anyone.
        //
        // Growth rebalancing finding: this used to check `!Has(Farm)`,
        // meaning exactly ONE Farm forever, regardless of population - a
        // 22-person settlement had the identical single-Farm food ceiling
        // as a 7-person one. Even after fixing the SoilHealth ratchet and
        // the disease/economy double-consumption bugs, one Farm's daily
        // yield is nowhere near enough to both feed a grown population AND
        // accumulate the 3-Food-per-capita surplus ReproductionSystem
        // requires - production was structurally incapable of scaling with
        // population, which is why the world could stabilize but never
        // grow. Scaling Farm count with population (capped like House
        // plans, so it can't runaway) closes that ceiling.
        var farmCount = settlement.Buildings.Count(b => b.BuildingType == BuildingTypes.Farm && b.Status != BuildingStatus.Ruined);
        var targetFarms = Math.Max(1, (settlement.MemberIds.Count + 9) / 10);
        var unbuiltFarms = settlement.Buildings.Count(b =>
            b.BuildingType == BuildingTypes.Farm
            && b.Status is BuildingStatus.Planned or BuildingStatus.UnderConstruction);
        // Every settlement needs its first Farm regardless of soil - but
        // adding a SECOND or THIRD one when SoilHealth is already
        // critically depleted only piles more simultaneous harvests onto
        // the same shared, barely-recovering settlement-level SoilHealth
        // number, making the depletion worse rather than scaling
        // production. Expansion beyond one Farm is gated on the existing
        // farmland actually being healthy enough to support more.
        var canExpandFarms = farmCount == 0 || settlement.SoilHealth >= 50.0;
        if (farmCount < targetFarms && unbuiltFarms < MaxUnbuiltFarmPlans && canExpandFarms) return BuildingTypes.Farm;

        if (!Has(BuildingTypes.Well)) return BuildingTypes.Well;
        // Rebalancing audit finding 4/5/8: a Healer is now a core-infra
        // building (DiseaseSystem reads its presence as a real recovery
        // bonus), so it belongs alongside Storage/Farm/Well ahead of
        // House/Workshop rather than never being queued at all.
        if (!Has(BuildingTypes.Healer)) return BuildingTypes.Healer;

        var unbuiltHouses = settlement.Buildings.Count(b =>
            b.BuildingType == BuildingTypes.House
            && b.Status is BuildingStatus.Planned or BuildingStatus.UnderConstruction);
        if (!settlement.HasAvailableHousing && unbuiltHouses < MaxUnbuiltHousePlans)
            return BuildingTypes.House;

        if (!Has(BuildingTypes.Workshop)) return BuildingTypes.Workshop;

        return null;
    }

    // Rebalancing audit finding 7: immediate family only (parents, children,
    // siblings via shared parent) - walked directly off ParentAId/ParentBId
    // rather than the Relationship/Affection graph, since that graph tracks
    // interaction strength, not who's actually kin.
    private Citizen? GetSickFamilyMember(Citizen citizen)
    {
        var infectedIds = _worldState.Infections
            .Where(i => i.IsActive)
            .Select(i => i.CitizenId)
            .ToHashSet();
        if (infectedIds.Count == 0) return null;

        bool IsImmediateFamily(Citizen other)
        {
            if (other.Id == citizen.ParentAId || other.Id == citizen.ParentBId) return true;
            if (other.ParentAId == citizen.Id || other.ParentBId == citizen.Id) return true;
            if (citizen.ParentAId != null && (other.ParentAId == citizen.ParentAId || other.ParentBId == citizen.ParentAId)) return true;
            if (citizen.ParentBId != null && (other.ParentAId == citizen.ParentBId || other.ParentBId == citizen.ParentBId)) return true;
            return false;
        }

        return _worldState.Citizens.FirstOrDefault(c =>
            c.IsAlive && c.Id != citizen.Id && infectedIds.Contains(c.Id) && IsImmediateFamily(c));
    }

    private static string? GetNeededMaterial(Settlement settlement)
    {
        var neededBuilding = GetNextNeededBuilding(settlement);
        if (neededBuilding == null) return null;

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

        // Natural water bodies (river/lake/coast) are never exhausted by
        // people drinking from them - treat them as infinite regardless of
        // whether a FreshWater ResourceDeposit object happens to sit on the
        // tile. Previously the tile's FreshWater deposit (added by world
        // generation with MaxCapacity=1000) was drained on every drink and
        // only regenerated once every 24 ticks; a settlement's own citizens
        // could drain their adjacent coastline/river dry faster than it
        // could refill. HasWaterAt/HasWaterAccess still reported the tile
        // as "has water" (they don't check quantity for natural bodies),
        // so citizens would stand at (what looked like) water forever,
        // goal stuck on "Seeking Water," silently failing to drink while
        // thirst climbed straight to critical and killed them - a citizen
        // could die of dehydration standing directly on the coast.
        if (tile.IsRiver || tile.IsLake || tile.Terrain == TerrainType.Coast)
        {
            const double amount = 10.0;
            citizen.Needs.Thirst = Math.Max(0, citizen.Needs.Thirst - amount * 2);
            citizen.CurrentActivity = "Drinking";

            _eventBus.Publish(new CitizenDrankEvent
            {
                Tick = _worldState.CurrentTime.Tick,
                CitizenId = citizen.Id,
                CitizenName = $"{citizen.FirstName} {citizen.LastName}",
                WaterSource = tile.IsRiver ? "River" : tile.IsLake ? "Lake" : "Coast",
                Amount = amount
            });
            return;
        }

        var waterSource = tile.Resources.FirstOrDefault(r => r.Type == ResourceType.FreshWater);

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
                WaterSource = "Ground",
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
