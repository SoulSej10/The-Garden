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
/// DEVELOPMENT_PLAN.md Week 17: Decomposers & Microbiology - Soil Health
/// per specification/RFC/RFC-011-decomposers-soil-health.md. Organic
/// matter comes from existing CitizenDied/ForestDeclined events; soil
/// depletion comes from the existing FarmHarvestedEvent; SoilHealth feeds
/// back into AgricultureSystem's yield formula.
/// </summary>
public class DecomposerSystemTests
{
    private static (WorldState world, EventBus bus, DecomposerSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new DecomposerSystem(world, bus);
        return (world, bus, system);
    }

    private static Settlement AddSettlement(WorldState world)
    {
        var settlement = new Settlement { Name = "Rivermoot", TileX = 0, TileY = 0 };
        world.Settlements.Add(settlement);
        return settlement;
    }

    [Fact]
    public void SoilHealth_DefaultsToFullHealth()
    {
        var (world, _, _) = CreateHarness();
        var settlement = AddSettlement(world);

        Assert.Equal(100.0, settlement.SoilHealth);
    }

    [Fact]
    public void FarmHarvested_DepletesSoilHealth()
    {
        var (world, bus, _) = CreateHarness();
        var settlement = AddSettlement(world);

        bus.Publish(new FarmHarvestedEvent
        {
            Tick = 1, SettlementId = settlement.Id, SettlementName = "Rivermoot",
            BuildingId = GameEntityId.New(), CropType = "Grain", Yield = 100.0
        });

        Assert.True(settlement.SoilHealth < 100.0);
    }

    [Fact]
    public void CitizenDied_AddsOrganicMatter_ThatLaterRaisesSoilHealth()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world);
        var citizen = new Citizen { FirstName = "Test", LastName = "Citizen", HomeSettlementId = settlement.Id };
        world.Citizens.Add(citizen);

        // Deplete soil first so there's room for a real rise to be visible.
        bus.Publish(new FarmHarvestedEvent
        {
            Tick = 1, SettlementId = settlement.Id, SettlementName = "Rivermoot",
            BuildingId = GameEntityId.New(), CropType = "Grain", Yield = 500.0
        });
        var soilAfterDepletion = settlement.SoilHealth;

        bus.Publish(new CitizenDiedEvent
        {
            Tick = 2, CitizenId = citizen.Id, CitizenName = "Test Citizen",
            CauseOfDeath = "Old Age", AgeAtDeath = 80
        });

        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        Assert.True(settlement.SoilHealth > soilAfterDepletion);
    }

    [Fact]
    public void ForestDeclined_WithinTerritory_AddsOrganicMatter_ThatLaterRaisesSoilHealth()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world);

        bus.Publish(new FarmHarvestedEvent
        {
            Tick = 1, SettlementId = settlement.Id, SettlementName = "Rivermoot",
            BuildingId = GameEntityId.New(), CropType = "Grain", Yield = 500.0
        });
        var soilAfterDepletion = settlement.SoilHealth;

        bus.Publish(new ForestDeclinedEvent { Tick = 2, TileX = 1, TileY = 1, AreaLost = 3 });

        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        Assert.True(settlement.SoilHealth > soilAfterDepletion);
    }

    [Fact]
    public void ForestDeclined_OutsideAnySettlementTerritory_IsIgnored()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world); // default TerritoryRadius = 5, centered at (0,0)

        bus.Publish(new ForestDeclinedEvent { Tick = 1, TileX = 500, TileY = 500, AreaLost = 3 });

        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        Assert.Equal(100.0, settlement.SoilHealth); // unchanged - no depletion, no organic matter to decompose
    }

    [Fact]
    public void PublishesNutrientPulse_WhenSoilHealthRisesMeaningfully()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world);
        var citizen = new Citizen { FirstName = "Test", LastName = "Citizen", HomeSettlementId = settlement.Id };
        world.Citizens.Add(citizen);

        bus.Publish(new FarmHarvestedEvent
        {
            Tick = 1, SettlementId = settlement.Id, SettlementName = "Rivermoot",
            BuildingId = GameEntityId.New(), CropType = "Grain", Yield = 1000.0
        });

        var pulsed = false;
        bus.Subscribe<NutrientPulseOccurredEvent>(_ => pulsed = true);

        // Enough deaths to guarantee a meaningful rise once decomposed -
        // OnCitizenDied looks the citizen up in world.Citizens by id, so
        // each one must actually exist there with a HomeSettlementId set.
        for (var i = 0; i < 10; i++)
        {
            var victim = new Citizen { FirstName = "X", LastName = "Y", HomeSettlementId = settlement.Id };
            world.Citizens.Add(victim);
            bus.Publish(new CitizenDiedEvent { Tick = 1, CitizenId = victim.Id, CitizenName = "X Y", CauseOfDeath = "Old Age", AgeAtDeath = 80 });
        }
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();

        Assert.True(pulsed);
    }
}
