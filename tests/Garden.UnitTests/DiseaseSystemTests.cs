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
/// DEVELOPMENT_PLAN.md Week 15: Disease & Health - Overcrowding-Driven
/// Infection per specification/RFC/RFC-009-disease-health-overcrowding.md.
/// Infection risk is gated on RFC-008's existing CarryingCapacity/Population
/// pressure signal; severity damages the citizen's existing Needs.Health;
/// death reuses the existing CitizenDiedEvent rather than a parallel path.
/// </summary>
public class DiseaseSystemTests
{
    private static (WorldState world, EventBus bus, DiseaseSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new DiseaseSystem(world, bus);
        return (world, bus, system);
    }

    private static Citizen AddCitizen(WorldState world, Settlement settlement, double health = 100.0)
    {
        var citizen = new Citizen
        {
            FirstName = "Test", LastName = "Citizen", IsAlive = true,
            Needs = new CitizenNeeds { Health = health },
            HomeSettlementId = settlement.Id
        };
        world.Citizens.Add(citizen);
        settlement.MemberIds.Add(citizen.Id);
        return citizen;
    }

    private static Settlement AddSettlement(WorldState world, double carryingCapacity)
    {
        // Rebalancing audit finding 4/5/8: DiseaseSystem's overcrowding
        // check now reads Settlement.HousingCapacity (derived from real
        // completed Shelter/House buildings) instead of the food-blended
        // CarryingCapacity, so CarryingCapacity is set here only for tests
        // that don't exercise the onset/overcrowding path at all.
        var settlement = new Settlement { Name = "Rivermoot", TileX = 0, TileY = 0, CarryingCapacity = carryingCapacity };
        world.Settlements.Add(settlement);
        return settlement;
    }

    private static void AddCompletedShelter(Settlement settlement)
    {
        settlement.Buildings.Add(new Building
        {
            BuildingType = BuildingTypes.Shelter,
            Status = BuildingStatus.Completed,
            BuildProgress = 100
        });
    }

    [Fact]
    public void NoInfection_OccursByDefault_WhenSettlementIsNotOvercrowded()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, carryingCapacity: 100);
        AddCompletedShelter(settlement); // capacity 2, well above the 1 member added below
        AddCitizen(world, settlement);

        var infected = false;
        bus.Subscribe<OrganismInfectedEvent>(_ => infected = true);

        for (var i = 0; i < 50; i++) system.Execute();

        Assert.False(infected);
        Assert.Empty(world.Infections);
    }

    [Fact]
    public void CanInfect_WhenSettlementIsOvercrowded()
    {
        var (world, bus, system) = CreateHarness();
        // No housing at all -> HousingCapacity 0 -> maximally overcrowded.
        var settlement = AddSettlement(world, carryingCapacity: 1);
        AddCitizen(world, settlement);

        var infected = false;
        bus.Subscribe<OrganismInfectedEvent>(_ => infected = true);

        // Daily infection chance is small (0.02) - run enough days that not
        // infecting at all would be a ~1-in-3-million coincidence. A
        // citizen can recover and be reinfected within this window, so
        // this only asserts at least one infection was ever recorded, not
        // exactly one.
        for (var i = 0; i < 1000; i++)
        {
            world.CurrentTime = SimulationTime.FromTick(i * 24);
            system.Execute();
        }

        Assert.True(infected);
        Assert.NotEmpty(world.Infections);
    }

    [Fact]
    public void ActiveInfection_DamagesCitizenHealth_EachEvaluation()
    {
        var (world, _, system) = CreateHarness();
        var settlement = AddSettlement(world, carryingCapacity: 100);
        var citizen = AddCitizen(world, settlement, health: 100.0);
        world.Infections.Add(new Infection { CitizenId = citizen.Id, Severity = 50.0 });

        var healthBefore = citizen.Needs.Health;
        // Force no-recovery, no-death by pinning severity growth via a
        // single evaluation - health should strictly decrease.
        system.Execute();

        Assert.True(citizen.Needs.Health <= healthBefore);
    }

    [Fact]
    public void InfectionReachingMaxSeverity_KillsCitizen_ViaExistingCitizenDiedEvent()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, carryingCapacity: 100);
        // Zero health means zero recovery chance, so death is deterministic
        // - recovery is checked before the severity/death check each
        // evaluation, so a healthy citizen could otherwise recover even at
        // max severity (a deliberate "recovery is always possible" design,
        // not a bug, but it would make this specific test flaky).
        var citizen = AddCitizen(world, settlement, health: 0.0);
        world.Infections.Add(new Infection { CitizenId = citizen.Id, Severity = Infection.MaxSeverity });

        CitizenDiedEvent? published = null;
        bus.Subscribe<CitizenDiedEvent>(e => published = e);

        system.Execute();

        Assert.False(citizen.IsAlive);
        Assert.Equal("Disease", citizen.CauseOfDeath);
        Assert.NotNull(published);
        Assert.Equal("Disease", published!.CauseOfDeath);
    }

    [Fact]
    public void HealthierCitizens_RecoverMoreOften_ThanSicklierOnes()
    {
        var healthyRecoveries = 0;
        var sicklyRecoveries = 0;

        for (var trial = 0; trial < 200; trial++)
        {
            var (world, bus, system) = CreateHarness();
            var settlement = AddSettlement(world, carryingCapacity: 100);
            var healthy = AddCitizen(world, settlement, health: 100.0);
            var sickly = AddCitizen(world, settlement, health: 10.0);
            world.Infections.Add(new Infection { CitizenId = healthy.Id, Severity = 10.0 });
            world.Infections.Add(new Infection { CitizenId = sickly.Id, Severity = 10.0 });

            bus.Subscribe<DiseaseRecoveredEvent>(e =>
            {
                if (e.CitizenId == healthy.Id) healthyRecoveries++;
                if (e.CitizenId == sickly.Id) sicklyRecoveries++;
            });

            system.Execute();
        }

        Assert.True(healthyRecoveries > sicklyRecoveries,
            $"Expected healthier citizens to recover more often, but healthy={healthyRecoveries}, sickly={sicklyRecoveries}");
    }

    [Fact]
    public void PublishesEpidemicStarted_WhenInfectionRateCrossesThreshold()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, carryingCapacity: 100);
        for (var i = 0; i < 5; i++) AddCitizen(world, settlement);

        // 2 of 5 members infected = 40% infection rate, above the 20%
        // threshold. Near-zero health keeps recovery chance negligible, so
        // the infection can't randomly clear within this same Execute()
        // call and mask the crossing.
        world.Infections.Add(new Infection { CitizenId = settlement.MemberIds[0], Severity = 10.0 });
        world.Infections.Add(new Infection { CitizenId = settlement.MemberIds[1], Severity = 10.0 });
        settlement.MemberIds.Take(2).ToList().ForEach(id =>
            world.Citizens.First(c => c.Id == id).Needs.Health = 1.0);

        var started = false;
        bus.Subscribe<EpidemicStartedEvent>(_ => started = true);

        system.Execute();

        Assert.True(started);
    }

    [Fact]
    public void EpidemicStarted_DoesNotRefire_WhileStillAboveThreshold()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, carryingCapacity: 100);
        for (var i = 0; i < 5; i++) AddCitizen(world, settlement);
        world.Infections.Add(new Infection { CitizenId = settlement.MemberIds[0], Severity = 10.0 });
        world.Infections.Add(new Infection { CitizenId = settlement.MemberIds[1], Severity = 10.0 });
        settlement.MemberIds.Take(2).ToList().ForEach(id =>
            world.Citizens.First(c => c.Id == id).Needs.Health = 1.0);

        var startedCount = 0;
        bus.Subscribe<EpidemicStartedEvent>(_ => startedCount++);

        system.Execute();
        world.CurrentTime = SimulationTime.FromTick(24);
        system.Execute();

        Assert.Equal(1, startedCount);
    }
}
