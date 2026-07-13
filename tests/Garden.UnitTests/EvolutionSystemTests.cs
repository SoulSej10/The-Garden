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
/// DEVELOPMENT_PLAN.md Week 16: Evolution & Adaptation - Adaptive Drift
/// Detection per specification/RFC/RFC-010-evolution-adaptive-drift.md.
/// Observes population-level attribute drift that ReproductionSystem's
/// inheritance and CitizenSystem's differential survival already produce -
/// no new selection mechanic is added.
/// </summary>
public class EvolutionSystemTests
{
    private static (WorldState world, EventBus bus, EvolutionSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new EvolutionSystem(world, bus);
        return (world, bus, system);
    }

    private static Settlement AddSettlement(WorldState world)
    {
        var settlement = new Settlement { Name = "Rivermoot", TileX = 0, TileY = 0 };
        world.Settlements.Add(settlement);
        return settlement;
    }

    private static Citizen AddCitizen(WorldState world, Settlement settlement, double strength)
    {
        var citizen = new Citizen
        {
            FirstName = "Test", LastName = "Citizen", IsAlive = true,
            Attributes = new CitizenAttributes { Strength = strength },
            HomeSettlementId = settlement.Id
        };
        world.Citizens.Add(citizen);
        settlement.MemberIds.Add(citizen.Id);
        return citizen;
    }

    [Fact]
    public void FirstEvaluation_EstablishesBaseline_WithoutFiringAnyEvent()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world);
        AddCitizen(world, settlement, strength: 5.0);

        var fired = false;
        bus.Subscribe<AdaptiveShiftObservedEvent>(_ => fired = true);

        system.Execute();

        Assert.False(fired);
    }

    [Fact]
    public void PublishesAdaptiveShift_WhenAverageAttributeMovesMeaningfully()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world);
        var citizen = AddCitizen(world, settlement, strength: 5.0);

        AdaptiveShiftObservedEvent? published = null;
        bus.Subscribe<AdaptiveShiftObservedEvent>(e => published = e);

        system.Execute(); // baseline

        citizen.Attributes.Strength = 6.0; // +1.0, above the 0.5 threshold
        world.CurrentTime = SimulationTime.FromTick(SimulationTime.TicksPerYear);
        system.Execute();

        Assert.NotNull(published);
        Assert.Equal("Strength", published!.AttributeName);
        Assert.Equal(1.0, published.Delta, precision: 3);
    }

    [Fact]
    public void DoesNotPublishShift_WhenChangeIsBelowThreshold()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world);
        var citizen = AddCitizen(world, settlement, strength: 5.0);

        var fired = false;
        bus.Subscribe<AdaptiveShiftObservedEvent>(_ => fired = true);

        system.Execute(); // baseline

        citizen.Attributes.Strength = 5.2; // +0.2, below the 0.5 threshold
        world.CurrentTime = SimulationTime.FromTick(SimulationTime.TicksPerYear);
        system.Execute();

        Assert.False(fired);
    }

    [Fact]
    public void PublishesStagnation_AfterSeveralConsecutiveYearsWithNoShift()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world);
        AddCitizen(world, settlement, strength: 5.0);

        var stagnationCount = 0;
        bus.Subscribe<EvolutionaryStagnationEvent>(_ => stagnationCount++);

        system.Execute(); // baseline
        for (var year = 1; year <= 4; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * SimulationTime.TicksPerYear);
            system.Execute();
        }

        Assert.Equal(1, stagnationCount);
    }

    [Fact]
    public void StagnationResets_AfterARealShiftOccurs()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world);
        var citizen = AddCitizen(world, settlement, strength: 5.0);

        var stagnationCount = 0;
        bus.Subscribe<EvolutionaryStagnationEvent>(_ => stagnationCount++);

        system.Execute(); // baseline
        for (var year = 1; year <= 3; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * SimulationTime.TicksPerYear);
            system.Execute();
        }
        Assert.Equal(1, stagnationCount); // stagnation fired at year 3

        citizen.Attributes.Strength = 7.0; // real shift resets the counter
        world.CurrentTime = SimulationTime.FromTick(4 * SimulationTime.TicksPerYear);
        system.Execute();

        for (var year = 5; year <= 6; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * SimulationTime.TicksPerYear);
            system.Execute();
        }

        // Only 2 stagnant years have passed since the shift - not enough
        // to refire yet.
        Assert.Equal(1, stagnationCount);
    }
}
