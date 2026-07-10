using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 3 Days 11-12: EmotionalState + decay, per
/// specification/RFC/RFC-001-emotion-system.md. Each emotion's real trigger
/// (documented on EmotionSystem) is exercised here, plus the shared decay
/// formula converging back toward baseline so nothing gets permanently
/// pinned at an extreme.
/// </summary>
public class EmotionSystemTests
{
    private static (WorldState world, EventBus bus, EmotionSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new EmotionSystem(world, bus);
        return (world, bus, system);
    }

    private static Citizen AddCitizen(WorldState world, string firstName = "Test")
    {
        var citizen = new Citizen { FirstName = firstName, LastName = "Citizen", IsAlive = true };
        world.Citizens.Add(citizen);
        return citizen;
    }

    [Fact]
    public void Fear_RisesTowardTarget_WhenNeedIsCritical()
    {
        var (world, _, system) = CreateHarness();
        var citizen = AddCitizen(world);
        citizen.Needs.Hunger = CitizenNeeds.HungerCriticalThreshold;

        var before = citizen.Emotions.Fear;
        system.Execute();

        Assert.True(citizen.Emotions.Fear > before,
            $"Expected Fear to rise while a need is critical, but {before} -> {citizen.Emotions.Fear}");
    }

    [Fact]
    public void Fear_DecaysBackToZero_OnceNoLongerCritical()
    {
        var (world, _, system) = CreateHarness();
        var citizen = AddCitizen(world);
        citizen.Emotions.Fear = 70.0;
        // No critical needs - Fear's target is 0.

        for (var i = 0; i < 200; i++)
        {
            world.CurrentTime = SimulationTime.FromTick(i);
            system.Execute();
        }

        Assert.True(citizen.Emotions.Fear < 1.0,
            $"Expected Fear to have decayed close to 0 after 200 ticks, but it's still {citizen.Emotions.Fear}");
    }

    [Fact]
    public void Joy_GetsReliefBump_WhenRecoveringFromFear()
    {
        var (world, _, system) = CreateHarness();
        var citizen = AddCitizen(world);
        citizen.Needs.Thirst = CitizenNeeds.ThirstCriticalThreshold;

        // Fear rises gradually (half-life 6 ticks) - run enough ticks for it
        // to climb past the 20.0 "wasAfraid" bar before testing the recovery.
        for (var i = 0; i < 10; i++)
        {
            world.CurrentTime = SimulationTime.FromTick(i);
            system.Execute();
        }

        Assert.True(citizen.Emotions.Fear > 20.0);

        citizen.Needs.Thirst = 0; // no longer critical
        var joyBefore = citizen.Emotions.Joy;
        system.Execute();

        Assert.True(citizen.Emotions.Joy > joyBefore,
            "Expected a relief-driven Joy increase when a citizen stops being afraid");
    }

    [Fact]
    public void Joy_SpikesOnSettlementFounded_ForTheFounderOnly()
    {
        var (world, bus, system) = CreateHarness();
        var founder = AddCitizen(world, "Founder");
        var bystander = AddCitizen(world, "Bystander");

        bus.Publish(new SettlementFoundedEvent
        {
            Tick = 0,
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            FounderId = founder.Id,
            FounderName = "Founder Citizen",
            TileX = 5,
            TileY = 5
        });

        Assert.True(founder.Emotions.Joy > 0);
        Assert.Equal(0, bystander.Emotions.Joy);
    }

    [Fact]
    public void Joy_SpikesOnCitizenBorn_ForBothParents()
    {
        var (world, bus, system) = CreateHarness();
        var parentA = AddCitizen(world, "ParentA");
        var parentB = AddCitizen(world, "ParentB");

        bus.Publish(new CitizenBornEvent
        {
            Tick = 0,
            CitizenId = GameEntityId.New(),
            CitizenName = "Newborn",
            TileX = 1,
            TileY = 1,
            ParentAId = parentA.Id,
            ParentBId = parentB.Id
        });

        Assert.True(parentA.Emotions.Joy > 0);
        Assert.True(parentB.Emotions.Joy > 0);
    }

    [Fact]
    public void Sadness_SpikesOnCitizenDied_ForLivingSettlementMatesOnly()
    {
        var (world, bus, system) = CreateHarness();
        var settlementId = GameEntityId.New();

        var deceased = AddCitizen(world, "Deceased");
        deceased.HomeSettlementId = settlementId;

        var mate = AddCitizen(world, "Mate");
        mate.HomeSettlementId = settlementId;

        var stranger = AddCitizen(world, "Stranger");
        stranger.HomeSettlementId = GameEntityId.New(); // different settlement

        bus.Publish(new CitizenDiedEvent
        {
            Tick = 0,
            CitizenId = deceased.Id,
            CitizenName = "Deceased Citizen",
            CauseOfDeath = "Old Age",
            AgeAtDeath = 80
        });

        Assert.True(mate.Emotions.Sadness > 0);
        Assert.Equal(0, stranger.Emotions.Sadness);
    }

    [Fact]
    public void Trust_MovesToward_CitizenReputation()
    {
        var (world, _, system) = CreateHarness();
        var citizen = AddCitizen(world);
        citizen.Reputation = 90.0;
        Assert.Equal(50.0, citizen.Emotions.Trust); // default baseline

        for (var i = 0; i < 2000; i++)
        {
            world.CurrentTime = SimulationTime.FromTick(i);
            system.Execute();
        }

        Assert.True(citizen.Emotions.Trust > 80.0,
            $"Expected Trust to have drifted toward Reputation (90) over 2000 ticks, but it's {citizen.Emotions.Trust}");
    }

    [Fact]
    public void Curiosity_SettlesToward_PersonalityCuriosityTimesTen()
    {
        var (world, _, system) = CreateHarness();
        var citizen = AddCitizen(world);
        citizen.Personality.Curiosity = 8.0; // 0-10 scale -> target 80

        for (var i = 0; i < 200; i++)
        {
            world.CurrentTime = SimulationTime.FromTick(i);
            system.Execute();
        }

        Assert.True(citizen.Emotions.Curiosity > 70.0,
            $"Expected Curiosity to settle near 80, but it's {citizen.Emotions.Curiosity}");
    }

    [Fact]
    public void Loneliness_RisesForCitizensWithNoSettlement_AndFallsOnceTheyJoinOne()
    {
        var (world, _, system) = CreateHarness();
        var citizen = AddCitizen(world);
        citizen.HomeSettlementId = null;

        for (var i = 0; i < 100; i++)
        {
            world.CurrentTime = SimulationTime.FromTick(i);
            system.Execute();
        }

        var lonelyValue = citizen.Emotions.Loneliness;
        Assert.True(lonelyValue > 30.0, $"Expected Loneliness to rise without a settlement, got {lonelyValue}");

        citizen.HomeSettlementId = GameEntityId.New();
        for (var i = 100; i < 300; i++)
        {
            world.CurrentTime = SimulationTime.FromTick(i);
            system.Execute();
        }

        Assert.True(citizen.Emotions.Loneliness < lonelyValue,
            "Expected Loneliness to fall after joining a settlement");
    }

    [Fact]
    public void NoEmotion_IsPermanentlyPinnedAtMaxOrMin()
    {
        var (world, _, system) = CreateHarness();
        var citizen = AddCitizen(world);
        citizen.Emotions.Fear = 100.0;
        citizen.Emotions.Joy = 100.0;
        citizen.Emotions.Sadness = 100.0;
        citizen.Emotions.Loneliness = 100.0;
        citizen.HomeSettlementId = GameEntityId.New(); // Loneliness target becomes 0

        for (var i = 0; i < 1000; i++)
        {
            world.CurrentTime = SimulationTime.FromTick(i);
            system.Execute();
        }

        Assert.True(citizen.Emotions.Fear < 5.0);
        Assert.True(citizen.Emotions.Joy < 5.0);
        Assert.True(citizen.Emotions.Sadness < 5.0);
        Assert.True(citizen.Emotions.Loneliness < 5.0);
    }
}
