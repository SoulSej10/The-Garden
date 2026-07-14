using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Generation;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.IntegrationTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 3 Day 15: EmotionSystem and RelationshipSystem
/// running alongside the full existing system stack (not in isolation like
/// the Garden.UnitTests coverage for these two systems), over a realistic
/// multi-week span, to prove the Week 3 work integrates cleanly with
/// citizen/settlement/economy simulation rather than only working in a
/// hand-built unit-test harness.
/// </summary>
public class EmotionRelationshipIntegrationTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public EmotionRelationshipIntegrationTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void EmotionsAndRelationships_EvolveAlongsideTheFullSimulation_WithoutErrors()
    {
        var worldState = new WorldState();
        var eventBus = new EventBus();

        var generator = new WorldGenerator(seed: 42);
        worldState.Map = generator.Generate(80, 80);
        worldState.IsInitialized = true;

        var populationManager = new PopulationManager();
        var settlementManager = new SettlementManager(worldState, eventBus, NullLogger<SettlementManager>.Instance);
        var constructionSystem = new ConstructionSystem(worldState, settlementManager, eventBus, NullLogger<ConstructionSystem>.Instance);
        var citizenSystem = new CitizenSystem(worldState, eventBus, NullLogger<CitizenSystem>.Instance, populationManager, settlementManager, constructionSystem);
        var emotionSystem = new EmotionSystem(worldState, eventBus);
        var relationshipSystem = new RelationshipSystem(worldState, eventBus);
        var agingSystem = new AgingSystem(worldState, eventBus, NullLogger<AgingSystem>.Instance);
        var reproductionSystem = new ReproductionSystem(worldState, eventBus, NullLogger<ReproductionSystem>.Instance, populationManager);
        var resourceSystem = new ResourceSystem(worldState, eventBus, NullLogger<ResourceSystem>.Instance);
        var agricultureSystem = new AgricultureSystem(worldState, eventBus, NullLogger<AgricultureSystem>.Instance);
        var economySystem = new EconomySystem(worldState, eventBus, NullLogger<EconomySystem>.Instance);

        var spawnSystem = new SpawnSystem(worldState, eventBus, NullLogger<SpawnSystem>.Instance);
        spawnSystem.SpawnInitialPopulation(50);

        const int twoMonthsInTicks = 24 * 60;
        for (long tick = 1; tick <= twoMonthsInTicks; tick++)
        {
            worldState.CurrentTime = SimulationTime.FromTick(tick);

            resourceSystem.Execute();
            citizenSystem.Execute();
            emotionSystem.Execute();
            if (tick % relationshipSystem.IntervalTicks == 0) relationshipSystem.Execute();
            agingSystem.Execute();
            reproductionSystem.Execute();
            constructionSystem.Execute();
            agricultureSystem.Execute();
            economySystem.Execute();

            eventBus.ClearPendingEvents();
        }

        var alive = worldState.Citizens.Where(c => c.IsAlive).ToList();
        Assert.True(alive.Count > 0, "Population should not have gone fully extinct over 2 months");

        // At least one citizen's emotions should have moved from the
        // all-default starting state - proves EmotionSystem actually ran
        // against real per-tick Needs/Personality/HomeSettlementId data, not
        // just the synthetic single-citizen scenarios in Garden.UnitTests.
        var anyEmotionsMoved = alive.Any(c =>
            c.Emotions.Fear != 0 || c.Emotions.Joy != 0 || c.Emotions.Sadness != 0
            || c.Emotions.Trust != 50.0 || c.Emotions.Curiosity != 0 || c.Emotions.Loneliness != 0);
        Assert.True(anyEmotionsMoved, "Expected at least one citizen's Emotions to have diverged from defaults after 2 months");

        _output.WriteLine($"Alive: {alive.Count}, Settlements: {worldState.Settlements.Count}, " +
            $"Relationships formed: {worldState.Relationships.Count}");
        _output.WriteLine($"Sample citizen emotions: {string.Join(", ", alive.Take(3).Select(c =>
            $"{c.FirstName}: Fear={c.Emotions.Fear:F1} Joy={c.Emotions.Joy:F1} Trust={c.Emotions.Trust:F1} Loneliness={c.Emotions.Loneliness:F1}"))}");
    }
}
