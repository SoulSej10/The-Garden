using Garden.Core.Identifiers;
using Garden.Engine.Events;
using Garden.Engine.Generation;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 3 Day 14: narrow additive hooks wiring
/// EmotionalState into CitizenSystem's existing decision chain (RFC-001),
/// not a rewrite of it.
/// </summary>
public class CitizenDecisionEmotionTests
{
    private static (WorldState world, CitizenSystem system) CreateHarness()
    {
        var world = new WorldState();
        world.Map = new WorldGenerator(seed: 42).Generate(20, 20);
        world.IsInitialized = true;
        var eventBus = new EventBus();
        var populationManager = new PopulationManager();
        var settlementManager = new SettlementManager(world, eventBus, NullLogger<SettlementManager>.Instance);
        var constructionSystem = new ConstructionSystem(world, settlementManager, eventBus, NullLogger<ConstructionSystem>.Instance);
        var system = new CitizenSystem(world, eventBus, NullLogger<CitizenSystem>.Instance, populationManager, settlementManager, constructionSystem);
        return (world, system);
    }

    private static Citizen AddCitizen(WorldState world)
    {
        var citizen = new Citizen { FirstName = "Test", LastName = "Citizen", IsAlive = true, Age = 30 };
        world.Citizens.Add(citizen);
        return citizen;
    }

    [Fact]
    public void HighFear_LowersCriticalThirstThreshold_TriggeringFindWaterEarlier()
    {
        var (world, system) = CreateHarness();
        var citizen = AddCitizen(world);
        // Below the normal critical threshold (80) but above the
        // fear-adjusted one (80 - 10 = 70).
        citizen.Needs.Thirst = 75.0;
        citizen.Emotions.Fear = 60.0; // > 50.0 triggers the urgency adjustment

        system.Execute();

        Assert.Equal("FindWater", citizen.CurrentGoal);
    }

    [Fact]
    public void NormalFear_LeavesTheOrdinaryWarningTierScoringInControl()
    {
        // Thirst=75 and Hunger=79 are both only "warning," not critical, and
        // scored almost identically (eatScore slightly edges out drinkScore),
        // so without elevated Fear the ordinary relative-scoring logic picks
        // FindFood. This isolates the fear-urgency branch specifically -
        // see the paired test below, where high Fear flips this same
        // starting state to FindWater by making thirst critical outright.
        var (world, system) = CreateHarness();
        var citizen = AddCitizen(world);
        citizen.Needs.Thirst = 75.0;
        citizen.Needs.Hunger = 79.3;
        citizen.Emotions.Fear = 0.0;

        system.Execute();

        Assert.Equal("FindFood", citizen.CurrentGoal);
    }

    [Fact]
    public void HighFear_OverridesOrdinaryScoring_ForTheSameNeedsState()
    {
        var (world, system) = CreateHarness();
        var citizen = AddCitizen(world);
        citizen.Needs.Thirst = 75.0;
        citizen.Needs.Hunger = 79.3;
        citizen.Emotions.Fear = 60.0; // > 50.0

        system.Execute();

        Assert.Equal("FindWater", citizen.CurrentGoal);
    }

    /// <summary>
    /// EmotionSystem (Day 12) only targets a high Loneliness for citizens
    /// with no HomeSettlementId - so the reachable place for a Socialize
    /// reaction is the bottom fallback, for a citizen who couldn't find or
    /// found a settlement this tick either (no nearby settlement, and too
    /// low a "community urge" to found one - see CreateLonelyDriftingCitizen).
    /// </summary>
    private static Citizen CreateLonelyDriftingCitizen(WorldState world)
    {
        var citizen = AddCitizen(world);
        citizen.HomeSettlementId = null;
        // communityUrge = Compassion + (10 - Introversion); keep it far
        // below the >11 founding threshold so this citizen never founds a
        // settlement regardless of nearby water.
        citizen.Personality.Compassion = 0;
        citizen.Personality.Introversion = 10;
        return citizen;
    }

    [Fact]
    public void HighLoneliness_MakesADriftingCitizenWithNoUrgentNeeds_SocializeInstead()
    {
        var (world, system) = CreateHarness();
        var citizen = CreateLonelyDriftingCitizen(world);
        citizen.Emotions.Loneliness = 60.0; // > 40.0

        system.Execute();

        Assert.Equal("Socialize", citizen.CurrentGoal);
    }

    [Fact]
    public void LowLoneliness_FallsBackToExploring_NotSocializing()
    {
        var (world, system) = CreateHarness();
        var citizen = CreateLonelyDriftingCitizen(world);
        citizen.Emotions.Loneliness = 0.0;

        system.Execute();

        Assert.Equal("Explore", citizen.CurrentGoal);
    }

    [Fact]
    public void HighLoneliness_DoesNotOverride_UrgentPhysiologicalNeeds()
    {
        var (world, system) = CreateHarness();
        var citizen = CreateLonelyDriftingCitizen(world);
        citizen.Emotions.Loneliness = 90.0;
        citizen.Needs.Thirst = CitizenNeeds.ThirstCriticalThreshold; // takes priority

        system.Execute();

        Assert.Equal("FindWater", citizen.CurrentGoal);
    }
}
