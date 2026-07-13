using Garden.Core.Events;
using Garden.Core.Time;
using Garden.Core.World;
using Garden.Engine.Events;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 18: Fauna & Animal Behavior - Aggregate
/// Wildlife Population per specification/RFC/RFC-012-fauna-aggregate-wildlife.md.
/// A single aggregate number per settlement, driven by Forest-tile habitat
/// within its territory - no individual animal agents.
/// </summary>
public class FaunaSystemTests
{
    private static (WorldState world, EventBus bus, FaunaSystem system) CreateHarness(int mapSize = 40)
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        world.Map.Initialize(mapSize, mapSize);
        for (var x = 0; x < mapSize; x++)
            for (var y = 0; y < mapSize; y++)
                world.Map.SetTile(x, y, new WorldTile { X = x, Y = y, Terrain = TerrainType.Plains });

        var bus = new EventBus();
        var system = new FaunaSystem(world, bus);
        return (world, bus, system);
    }

    private static Settlement AddSettlement(WorldState world, int x, int y, int territoryRadius = 5)
    {
        var settlement = new Settlement { Name = "Rivermoot", TileX = x, TileY = y, TerritoryRadius = territoryRadius };
        world.Settlements.Add(settlement);
        return settlement;
    }

    private static void MakeForest(WorldState world, int x, int y) =>
        world.Map.GetTile(x, y).Terrain = TerrainType.Forest;

    [Fact]
    public void NoForestTiles_MeansZeroHabitatCapacity_AndPopulationStaysZero()
    {
        var (world, _, system) = CreateHarness();
        var settlement = AddSettlement(world, 20, 20);

        system.Execute();

        Assert.Equal(0, settlement.WildlifePopulation);
    }

    [Fact]
    public void ForestTiles_WithinTerritory_GrowWildlifePopulationTowardHabitatCapacity()
    {
        var (world, _, system) = CreateHarness();
        var settlement = AddSettlement(world, 20, 20, territoryRadius: 2);
        for (var x = 18; x <= 22; x++)
            for (var y = 18; y <= 22; y++)
                MakeForest(world, x, y);

        system.Execute();

        Assert.True(settlement.WildlifePopulation > 0);
    }

    [Fact]
    public void PublishesSpeciesExpanded_WhenPopulationGrowsMeaningfully()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, 20, 20, territoryRadius: 3);
        for (var x = 17; x <= 23; x++)
            for (var y = 17; y <= 23; y++)
                MakeForest(world, x, y);

        var expanded = false;
        bus.Subscribe<SpeciesExpandedEvent>(_ => expanded = true);

        system.Execute(); // starts at 0, real forest habitat -> meaningful growth

        Assert.True(expanded);
    }

    [Fact]
    public void PublishesAnimalDied_WhenPopulationFallsMeaningfully()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, 20, 20, territoryRadius: 3);
        for (var x = 17; x <= 23; x++)
            for (var y = 17; y <= 23; y++)
                MakeForest(world, x, y);

        system.Execute(); // grows toward a real habitat capacity
        world.CurrentTime = SimulationTime.FromTick(24 * 30);
        system.Execute();
        var populationBeforeDeforestation = settlement.WildlifePopulation;

        // Deforest the entire territory - habitat capacity collapses to 0.
        for (var x = 17; x <= 23; x++)
            for (var y = 17; y <= 23; y++)
                world.Map.GetTile(x, y).Terrain = TerrainType.Plains;

        var died = false;
        bus.Subscribe<AnimalDiedEvent>(_ => died = true);

        world.CurrentTime = SimulationTime.FromTick(24 * 60);
        system.Execute();

        Assert.True(settlement.WildlifePopulation < populationBeforeDeforestation);
        Assert.True(died);
    }
}
