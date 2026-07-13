using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Weeks 19-20: Warfare & Military Organization -
/// Dispute Escalation per specification/RFC/RFC-013-warfare-dispute-escalation.md.
/// Escalates an already-detected TerritorySystem border dispute (RFC-007)
/// between settlements with a Hostile DiplomaticRelation into a real,
/// resolvable war.
/// </summary>
public class WarfareSystemTests
{
    private static (WorldState world, EventBus bus, TerritorySystem territorySystem, WarfareSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var settlementManager = new SettlementManager(world, bus, NullLogger<SettlementManager>.Instance);
        var territorySystem = new TerritorySystem(world, settlementManager, bus);
        var system = new WarfareSystem(world, bus, territorySystem);
        return (world, bus, territorySystem, system);
    }

    private static Settlement AddSettlement(WorldState world, string name, int x, int y,
        int population, double legitimacy, int territoryRadius = 10)
    {
        var settlement = new Settlement
        {
            Name = name, TileX = x, TileY = y,
            Population = population, Legitimacy = legitimacy, TerritoryRadius = territoryRadius
        };
        world.Settlements.Add(settlement);
        return settlement;
    }

    private static DiplomaticRelation MakeHostile(WorldState world, Settlement a, Settlement b)
    {
        var relation = new DiplomaticRelation
        {
            EntityAId = a.Id, EntityAName = a.Name, EntityAIsSettlement = true,
            EntityBId = b.Id, EntityBName = b.Name, EntityBIsSettlement = true,
            CurrentRelation = RelationType.Hostile,
            RelationScore = 10.0
        };
        world.DiplomaticRelations.Add(relation);
        return relation;
    }

    // Mirrors TerritorySystemTests.DetectsDispute_WhenOverlapping_AndInfluenceComparable's
    // exact setup, since WarDeclaration depends on TerritorySystem actually
    // detecting a real dispute first.
    private static (Settlement a, Settlement b) SetUpOverlappingDispute(WorldState world, TerritorySystem territorySystem)
    {
        var a = AddSettlement(world, "A", 0, 0, population: 20, legitimacy: 50.0);
        var b = AddSettlement(world, "B", 15, 0, population: 20, legitimacy: 50.0);
        territorySystem.Execute();
        return (a, b);
    }

    [Fact]
    public void DeclaresWar_WhenDisputeIsActive_AndRelationIsHostile()
    {
        var (world, bus, territorySystem, system) = CreateHarness();
        var (a, b) = SetUpOverlappingDispute(world, territorySystem);
        MakeHostile(world, a, b);

        var declared = false;
        bus.Subscribe<WarDeclaredEvent>(_ => declared = true);

        system.Execute();

        Assert.True(declared);
        Assert.Single(world.Wars);
    }

    [Fact]
    public void DoesNotDeclareWar_WhenRelationIsNotHostile()
    {
        var (world, bus, territorySystem, system) = CreateHarness();
        var (a, b) = SetUpOverlappingDispute(world, territorySystem);
        world.DiplomaticRelations.Add(new DiplomaticRelation
        {
            EntityAId = a.Id, EntityAName = a.Name, EntityAIsSettlement = true,
            EntityBId = b.Id, EntityBName = b.Name, EntityBIsSettlement = true,
            CurrentRelation = RelationType.Neutral,
            RelationScore = 50.0
        });

        var declared = false;
        bus.Subscribe<WarDeclaredEvent>(_ => declared = true);

        system.Execute();

        Assert.False(declared);
        Assert.Empty(world.Wars);
    }

    [Fact]
    public void DoesNotDeclareWar_WhenNoDisputeExists()
    {
        var (world, bus, _, system) = CreateHarness();
        var a = AddSettlement(world, "A", 0, 0, population: 20, legitimacy: 50.0, territoryRadius: 1);
        var b = AddSettlement(world, "B", 500, 500, population: 20, legitimacy: 50.0, territoryRadius: 1);
        MakeHostile(world, a, b);

        var declared = false;
        bus.Subscribe<WarDeclaredEvent>(_ => declared = true);

        system.Execute();

        Assert.False(declared);
    }

    [Fact]
    public void DoesNotRedeclareWar_WhileAlreadyActive()
    {
        var (world, bus, territorySystem, system) = CreateHarness();
        var (a, b) = SetUpOverlappingDispute(world, territorySystem);
        MakeHostile(world, a, b);

        var declaredCount = 0;
        bus.Subscribe<WarDeclaredEvent>(_ => declaredCount++);

        system.Execute();
        world.CurrentTime = SimulationTime.FromTick(SimulationTime.TicksPerYear);
        territorySystem.Execute(); // dispute still active
        system.Execute();

        Assert.Equal(1, declaredCount);
    }

    [Fact]
    public void BattleDamagesLoser_PopulationAndLegitimacy()
    {
        var (world, _, territorySystem, system) = CreateHarness();
        var (a, b) = SetUpOverlappingDispute(world, territorySystem);
        MakeHostile(world, a, b);

        system.Execute(); // declares war and fights the first battle in the same evaluation

        var totalPopulationAfter = a.Population + b.Population;
        Assert.True(totalPopulationAfter < 40); // one side lost population
    }

    [Fact]
    public void PublishesPeaceNegotiated_AfterMaxBattlesFought()
    {
        // Populations must stay comparable enough that TerritorySystem
        // keeps detecting the dispute across evaluations (its own 20%
        // influence-gap requirement), so this drives peace via the
        // max-battles path rather than a single lopsided loss.
        var (world, bus, territorySystem, system) = CreateHarness();
        var (a, b) = SetUpOverlappingDispute(world, territorySystem);
        MakeHostile(world, a, b);

        var peaceNegotiated = false;
        bus.Subscribe<PeaceNegotiatedEvent>(_ => peaceNegotiated = true);

        for (var year = 0; year < 5; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * SimulationTime.TicksPerYear);
            territorySystem.Execute();
            system.Execute();
        }

        Assert.True(peaceNegotiated);
        Assert.False(world.Wars.Single().IsActive);
    }

    [Fact]
    public void PeaceRestoresSomeRelationScore_ButNotFully()
    {
        var (world, _, territorySystem, system) = CreateHarness();
        var (a, b) = SetUpOverlappingDispute(world, territorySystem);
        var relation = MakeHostile(world, a, b);
        var scoreBeforePeace = relation.RelationScore;

        for (var year = 0; year < 5; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * SimulationTime.TicksPerYear);
            territorySystem.Execute();
            system.Execute();
        }

        Assert.True(relation.RelationScore > scoreBeforePeace);
        Assert.True(relation.RelationScore < 50.0); // restored, not fully reset to neutral
    }
}
