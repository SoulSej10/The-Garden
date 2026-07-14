using Garden.Core.Identifiers;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Scheduling;
using Garden.Engine.Services;
using Garden.Engine.Time;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 25: Save/Load Fidelity + Timeline Branching per
/// specification/RFC/RFC-017-save-load-timeline-branching.md. Two real
/// defects existed before this: LoadAsync never restored WorldState.CurrentTime
/// or any civilization-level collection (Kingdoms, TradeRoutes, Wars, etc.),
/// and there was no concept of save lineage at all.
/// </summary>
public class SaveLoadServiceTests
{
    private static (WorldState world, HistoricalArchive archive, SaveLoadService service) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var archive = new HistoricalArchive();
        var clock = new SimulationClock();
        var eventBus = new EventBus();
        var scheduler = new SimulationScheduler();
        var coordinator = new SimulationCoordinator(clock, eventBus, scheduler, world, NullLogger<SimulationCoordinator>.Instance);
        var service = new SaveLoadService(world, archive, coordinator, NullLogger<SaveLoadService>.Instance);
        return (world, archive, service);
    }

    private static string UniqueName() => $"test_{Guid.NewGuid():N}";

    [Fact]
    public async Task LoadAsync_RestoresCurrentTime()
    {
        var (world, _, service) = CreateHarness();
        var name = UniqueName();
        try
        {
            world.CurrentTime = SimulationTime.FromTick(50_000);
            await service.SaveAsync(name);

            world.CurrentTime = SimulationTime.FromTick(0);
            var loaded = await service.LoadAsync(name);

            Assert.True(loaded);
            Assert.Equal(50_000, world.CurrentTime.Tick);
        }
        finally { service.DeleteSave(name); }
    }

    [Fact]
    public async Task LoadAsync_RestoresCivilizationLevelCollections()
    {
        var (world, _, service) = CreateHarness();
        var name = UniqueName();
        try
        {
            world.Kingdoms.Add(new Kingdom { Name = "Saved Kingdom" });
            world.Wars.Add(new War { SettlementAId = GameEntityId.New(), SettlementBId = GameEntityId.New() });
            world.SettlementTechnologies.Add(new SettlementTechnology { SettlementId = GameEntityId.New(), TechnologyName = "Stone Tools" });
            world.Legends.Add(new Legend { Title = "The Legend of Something" });
            await service.SaveAsync(name);

            // Simulate a live world that has moved on since the save.
            world.Kingdoms.Clear();
            world.Wars.Clear();
            world.SettlementTechnologies.Clear();
            world.Legends.Clear();
            world.Kingdoms.Add(new Kingdom { Name = "Unrelated Live Kingdom" });

            var loaded = await service.LoadAsync(name);

            Assert.True(loaded);
            Assert.Single(world.Kingdoms);
            Assert.Equal("Saved Kingdom", world.Kingdoms[0].Name);
            Assert.Single(world.Wars);
            Assert.Single(world.SettlementTechnologies);
            Assert.Single(world.Legends);
        }
        finally { service.DeleteSave(name); }
    }

    [Fact]
    public async Task SaveAsync_FirstSaveInSession_HasNoParent()
    {
        var (_, _, service) = CreateHarness();
        var name = UniqueName();
        try
        {
            await service.SaveAsync(name);

            var entry = service.GetTimeline().Single(e => e.Name == name);
            Assert.Null(entry.ParentSaveId);
        }
        finally { service.DeleteSave(name); }
    }

    [Fact]
    public async Task SaveAsync_AfterLoad_RecordsLoadedSaveAsParent()
    {
        var (_, _, service) = CreateHarness();
        var original = UniqueName();
        var branch = UniqueName();
        try
        {
            await service.SaveAsync(original);
            await service.LoadAsync(original);
            await service.SaveAsync(branch);

            var originalEntry = service.GetTimeline().Single(e => e.Name == original);
            var branchEntry = service.GetTimeline().Single(e => e.Name == branch);

            Assert.Equal(originalEntry.Id, branchEntry.ParentSaveId);
        }
        finally
        {
            service.DeleteSave(original);
            service.DeleteSave(branch);
        }
    }

    [Fact]
    public async Task ResetLineage_ClearsParentForNextSave()
    {
        var (_, _, service) = CreateHarness();
        var original = UniqueName();
        var afterReset = UniqueName();
        try
        {
            await service.SaveAsync(original);
            await service.LoadAsync(original);
            service.ResetLineage();
            await service.SaveAsync(afterReset);

            var entry = service.GetTimeline().Single(e => e.Name == afterReset);
            Assert.Null(entry.ParentSaveId);
        }
        finally
        {
            service.DeleteSave(original);
            service.DeleteSave(afterReset);
        }
    }

    [Fact]
    public async Task LoadAsync_NonexistentSave_ReturnsFalse()
    {
        var (_, _, service) = CreateHarness();

        var loaded = await service.LoadAsync($"does_not_exist_{Guid.NewGuid():N}");

        Assert.False(loaded);
    }
}
