using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Generation;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.IntegrationTests;

public class SurvivalSimulationTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public SurvivalSimulationTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Regression guard for the reported collapse: all 50 citizens starved to
    /// death within the first 10 days, no settlement ever formed. This test
    /// simulates the first ~2 in-game months and checks the population is
    /// still mostly alive, healthy, and organized into settlements - it does
    /// not assert long-term (multi-year) population balance, which is a
    /// separate tuning concern from the structural bugs that caused total
    /// early collapse.
    /// </summary>
    [Fact]
    public void Population_SurvivesEarlyMonths_InsteadOfCollapsing()
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
        var agingSystem = new AgingSystem(worldState, eventBus, NullLogger<AgingSystem>.Instance);
        var reproductionSystem = new ReproductionSystem(worldState, eventBus, NullLogger<ReproductionSystem>.Instance, populationManager);
        var resourceSystem = new ResourceSystem(worldState, eventBus, NullLogger<ResourceSystem>.Instance);
        var agricultureSystem = new AgricultureSystem(worldState, eventBus, NullLogger<AgricultureSystem>.Instance);
        var economySystem = new EconomySystem(worldState, NullLogger<EconomySystem>.Instance);

        var spawnSystem = new SpawnSystem(worldState, eventBus, NullLogger<SpawnSystem>.Instance);
        spawnSystem.SpawnInitialPopulation(50);

        const int twoMonthsInTicks = 24 * 60;
        for (long tick = 1; tick <= twoMonthsInTicks; tick++)
        {
            worldState.CurrentTime = SimulationTime.FromTick(tick);

            resourceSystem.Execute();
            citizenSystem.Execute();
            agingSystem.Execute();
            reproductionSystem.Execute();
            constructionSystem.Execute();
            agricultureSystem.Execute();
            economySystem.Execute();

            eventBus.ClearPendingEvents();

            if (tick is 24 or 240 or 960 or 1440)
            {
                var snapshot = worldState.Citizens.Where(c => c.IsAlive).ToList();
                _output.WriteLine($"Tick {tick}: alive={snapshot.Count} " +
                    (snapshot.Count > 0
                        ? $"avgHealth={snapshot.Average(c => c.Needs.Health):F1}"
                        : ""));
            }
        }

        var alive = worldState.Citizens.Where(c => c.IsAlive).ToList();
        var survivalRate = alive.Count / 50.0;

        Assert.True(survivalRate > 0.6,
            $"Expected most of the population to survive the first 2 months, but only {alive.Count}/50 did. " +
            $"Death causes: {string.Join(", ", populationManager.DeathCauses.Select(kv => $"{kv.Key}={kv.Value}"))}");

        Assert.True(alive.Average(c => c.Needs.Health) > 60,
            "Expected survivors to be in reasonably good health, not just barely alive");

        Assert.True(worldState.Settlements.Count > 0,
            "Expected at least one settlement to form within 2 months");
    }
}
