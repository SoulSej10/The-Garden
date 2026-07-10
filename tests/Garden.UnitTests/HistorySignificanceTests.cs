using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Engine.Events;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 4 Day 18: audits SignificanceEvaluator against
/// TG-STRY-050's Core Principle ("consequences, not spectacle"). Before this
/// fix, "FarmHarvested" was unconditionally archived as High importance
/// regardless of actual yield, flooding HistoricalArchive with near-
/// duplicate routine harvests - directly observed live while testing Day
/// 17's faceted search (dozens of "Harvest at Upperridge" records for one
/// settlement within a few in-game days).
/// </summary>
public class HistorySignificanceTests
{
    private static (EventBus bus, HistoricalArchive archive) CreateHarness()
    {
        var worldState = new WorldState();
        var eventBus = new EventBus();
        var archive = new HistoricalArchive();
        var evaluator = new SignificanceEvaluator();
        var timeline = new TimelineService(archive);
        var memory = new MemoryService(worldState, archive);
        var storyEngine = new StoryEngine(archive);
        var historyManager = new HistoryManager(archive, evaluator, timeline, memory, storyEngine);
        _ = new HistorySystem(worldState, historyManager, eventBus, NullLogger<HistorySystem>.Instance);

        return (eventBus, archive);
    }

    [Fact]
    public void RoutineSmallHarvest_IsNoLongerArchived()
    {
        var (bus, archive) = CreateHarness();

        bus.Publish(new FarmHarvestedEvent
        {
            Tick = 100,
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            BuildingId = GameEntityId.New(),
            CropType = "Grain",
            Yield = 35.0 // severity = 35/20 = 1.75, well below the Medium threshold of 4.0
        });

        Assert.Empty(archive.Records);
    }

    [Fact]
    public void ModeratelyLargeHarvest_IsArchivedAsMediumImportance()
    {
        var (bus, archive) = CreateHarness();

        bus.Publish(new FarmHarvestedEvent
        {
            Tick = 100,
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            BuildingId = GameEntityId.New(),
            CropType = "Grain",
            Yield = 100.0 // severity = 100/20 = 5.0, above the 4.0 Medium threshold
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("Medium", record.Importance);
    }

    [Fact]
    public void ExceptionallyLargeHarvest_IsArchivedAsHighImportance()
    {
        var (bus, archive) = CreateHarness();

        bus.Publish(new FarmHarvestedEvent
        {
            Tick = 100,
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            BuildingId = GameEntityId.New(),
            CropType = "Grain",
            Yield = 200.0 // severity = 200/20 = 10.0, above the 7.0 High threshold
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("High", record.Importance);
    }
}
