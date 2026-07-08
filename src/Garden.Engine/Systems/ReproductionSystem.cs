using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Engine.Generators;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class ReproductionSystem : IScheduledSystem
{
    private const int MinParentAge = 18;
    private const int MaxParentAge = 45;

    // Deliberately conservative: population growth needs to stay well behind
    // a settlement's ability to build housing and gather food, or growth
    // outpaces its own food supply and the settlement starves itself via
    // overpopulation instead of via a missing survival mechanic.
    private const double DailyConceptionChance = 0.006;

    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ReproductionSystem> _logger;
    private readonly PopulationManager _populationManager;
    private long _nextExecutionTick;

    public string Name => "ReproductionSystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    public ReproductionSystem(
        WorldState worldState,
        IEventBus eventBus,
        ILogger<ReproductionSystem> logger,
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

        foreach (var settlement in _worldState.Settlements)
        {
            if (!settlement.HasAvailableHousing) continue;

            var members = settlement.MemberIds
                .Select(id => _worldState.Citizens.FirstOrDefault(c => c.Id == id))
                .Where(c => c != null && c.IsAlive && IsEligible(c))
                .Select(c => c!)
                .ToList();

            var males = members.Where(c => c.BiologicalSex == "Male").ToList();
            var females = members.Where(c => c.BiologicalSex == "Female").ToList();
            if (males.Count == 0 || females.Count == 0) continue;

            foreach (var mother in females)
            {
                if (System.Random.Shared.NextDouble() >= DailyConceptionChance) continue;

                var father = males[System.Random.Shared.Next(males.Count)];
                var child = CreateChild(mother, father, settlement, tick);

                _worldState.Citizens.Add(child);
                settlement.MemberIds.Add(child.Id);
                settlement.Population = settlement.MemberIds.Count;
                _populationManager.RecordBirth();

                _eventBus.Publish(new CitizenBornEvent
                {
                    Tick = tick,
                    CitizenId = child.Id,
                    CitizenName = $"{child.FirstName} {child.LastName}",
                    TileX = child.TileX,
                    TileY = child.TileY,
                    ParentAId = mother.Id,
                    ParentBId = father.Id,
                    SettlementId = settlement.Id
                });

                _logger.LogInformation("{Child} was born to {Mother} and {Father} in {Settlement}",
                    $"{child.FirstName} {child.LastName}",
                    $"{mother.FirstName} {mother.LastName}",
                    $"{father.FirstName} {father.LastName}",
                    settlement.Name);

                if (!settlement.HasAvailableHousing) break;
            }
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private static bool IsEligible(Citizen? citizen)
    {
        if (citizen == null) return false;
        return citizen.Age is >= MinParentAge and <= MaxParentAge
            && citizen.Needs.Health > 50
            && citizen.Needs.Hunger < 60
            && citizen.Needs.Thirst < 60
            && citizen.Needs.Energy > 30;
    }

    private static Citizen CreateChild(Citizen mother, Citizen father, Settlement settlement, long tick)
    {
        return new Citizen
        {
            FirstName = NameGenerator.GenerateFirstName(),
            LastName = System.Random.Shared.NextDouble() < 0.5 ? mother.LastName : father.LastName,
            Age = 0,
            BiologicalSex = System.Random.Shared.NextDouble() < 0.5 ? "Male" : "Female",
            TileX = mother.TileX,
            TileY = mother.TileY,
            Stage = LifeStage.Newborn,
            IsAlive = true,
            BirthTick = tick,
            HomeSettlementId = settlement.Id,
            Needs = new CitizenNeeds
            {
                Hunger = 10,
                Thirst = 10,
                Energy = 80,
                Warmth = 50,
                Health = 100
            },
            Attributes = new CitizenAttributes
            {
                Strength = Average(mother.Attributes.Strength, father.Attributes.Strength),
                Endurance = Average(mother.Attributes.Endurance, father.Attributes.Endurance),
                Intelligence = Average(mother.Attributes.Intelligence, father.Attributes.Intelligence),
                Dexterity = Average(mother.Attributes.Dexterity, father.Attributes.Dexterity),
                Perception = Average(mother.Attributes.Perception, father.Attributes.Perception)
            },
            Personality = new PersonalityTraits
            {
                Curiosity = Average(mother.Personality.Curiosity, father.Personality.Curiosity),
                Patience = Average(mother.Personality.Patience, father.Personality.Patience),
                Aggression = Average(mother.Personality.Aggression, father.Personality.Aggression),
                Compassion = Average(mother.Personality.Compassion, father.Personality.Compassion),
                Diligence = Average(mother.Personality.Diligence, father.Personality.Diligence),
                Introversion = Average(mother.Personality.Introversion, father.Personality.Introversion)
            },
            CurrentActivity = "Idle",
            CurrentGoal = "Survive"
        };
    }

    private static double Average(double a, double b)
    {
        var mid = (a + b) / 2.0;
        var variance = (System.Random.Shared.NextDouble() - 0.5) * 2.0;
        return Math.Clamp(mid + variance, 0, 10);
    }
}
