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

    /// <summary>
    /// Runs a full 3 in-game years (matching the reported "Year 3, population
    /// 0" state) to check whether the population genuinely stabilizes long
    /// term, rather than just surviving the early months before a slower
    /// terminal decline.
    /// </summary>
    [Fact]
    public void Population_RemainsViable_AcrossThreeYears()
    {
        var worldState = new WorldState();
        var eventBus = new EventBus();

        var generator = new WorldGenerator(seed: 42);
        worldState.Map = generator.Generate(100, 100);
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

        const int threeYearsInTicks = 24 * 30 * 12 * 3;
        for (long tick = 1; tick <= threeYearsInTicks; tick++)
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

            if (tick % (24 * 30 * 3) == 0) // every ~3 months
            {
                var snapshot = worldState.Citizens.Where(c => c.IsAlive).ToList();
                var time = SimulationTime.FromTick(tick);
                _output.WriteLine($"Year {time.Year} Day {time.Day}: alive={snapshot.Count} settlements={worldState.Settlements.Count} " +
                    (snapshot.Count > 0 ? $"avgHealth={snapshot.Average(c => c.Needs.Health):F1} avgAge={snapshot.Average(c => c.Age):F1}" : ""));
            }
        }

        var alive = worldState.Citizens.Where(c => c.IsAlive).ToList();
        _output.WriteLine($"Final: alive={alive.Count}/{worldState.Citizens.Count} total ever lived. " +
            $"Death causes: {string.Join(", ", populationManager.DeathCauses.Select(kv => $"{kv.Key}={kv.Value}"))}");

        Assert.True(alive.Count > 0,
            $"Population went fully extinct within 3 years. Death causes: " +
            $"{string.Join(", ", populationManager.DeathCauses.Select(kv => $"{kv.Key}={kv.Value}"))}");
    }

    /// <summary>
    /// Traces the exact tick-by-tick needs/goal/position history of the
    /// first few citizens who die of Dehydration, to see what actually
    /// happens leading up to death rather than guessing from aggregate
    /// statistics.
    /// </summary>
    [Fact]
    public void Diagnostic_TraceDehydrationDeaths()
    {
        var worldState = new WorldState();
        var eventBus = new EventBus();

        var generator = new WorldGenerator(seed: 42);
        worldState.Map = generator.Generate(100, 100);
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

        var history = new Dictionary<Garden.Core.Identifiers.GameEntityId, List<string>>();
        var tracedDeaths = 0;
        const int maxTracedDeaths = 3;

        var citizenLookup = worldState.Citizens.ToDictionary(c => c.Id);

        eventBus.Subscribe<Garden.Core.Events.CitizenDiedEvent>(e =>
        {
            if (tracedDeaths >= maxTracedDeaths) return;
            if (e.CauseOfDeath != "Dehydration") return;
            if (!history.TryGetValue(e.CitizenId, out var log)) return;

            tracedDeaths++;
            _output.WriteLine($"=== Death trace: {e.CitizenName} (cause={e.CauseOfDeath}, age={e.AgeAtDeath}) ===");
            foreach (var line in log.TakeLast(40)) _output.WriteLine(line);
            _output.WriteLine("");
        });

        const int ticksToRun = 24 * 60; // 2 months
        for (long tick = 1; tick <= ticksToRun; tick++)
        {
            worldState.CurrentTime = SimulationTime.FromTick(tick);

            resourceSystem.Execute();
            citizenSystem.Execute();
            agingSystem.Execute();
            reproductionSystem.Execute();
            constructionSystem.Execute();
            agricultureSystem.Execute();
            economySystem.Execute();

            foreach (var c in worldState.Citizens.Where(c => c.IsAlive))
            {
                if (!history.TryGetValue(c.Id, out var log))
                {
                    log = [];
                    history[c.Id] = log;
                }
                log.Add($"tick={tick} pos=({c.TileX},{c.TileY}) settlement={(c.HomeSettlementId != null ? "yes" : "no")} " +
                        $"goal={c.CurrentGoal} activity={c.CurrentActivity} hunger={c.Needs.Hunger:F0} thirst={c.Needs.Thirst:F0} " +
                        $"energy={c.Needs.Energy:F0} health={c.Needs.Health:F0}");
                if (log.Count > 60) log.RemoveAt(0);
            }

            eventBus.ClearPendingEvents();

            if (tracedDeaths >= maxTracedDeaths) break;
        }

        _output.WriteLine($"Traced {tracedDeaths} dehydration deaths.");
    }
}
