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
/// DEVELOPMENT_PLAN.md Week 9: Dispute Resolution per
/// specification/RFC/RFC-005-law-dispute-resolution.md. A dispute (a
/// Relationship with Trust below the threshold, within one settlement)
/// opens a LegalCase, resolved with a chance proportional to the
/// settlement's existing Legitimacy score, or closed as a JusticeFailure
/// after the settlement's leader fails to resolve it in time.
/// </summary>
public class LawSystemTests
{
    private static (WorldState world, EventBus bus, LawSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new LawSystem(world, bus);
        return (world, bus, system);
    }

    private static Settlement AddSettlement(WorldState world, double legitimacy)
    {
        var settlement = new Settlement { Name = "Rivermoot", Legitimacy = legitimacy };
        world.Settlements.Add(settlement);
        return settlement;
    }

    private static Citizen AddCitizen(WorldState world, string name, GameEntityId? homeSettlementId)
    {
        var citizen = new Citizen { FirstName = name, LastName = "Citizen", IsAlive = true, HomeSettlementId = homeSettlementId };
        world.Citizens.Add(citizen);
        return citizen;
    }

    private static Relationship AddDispute(WorldState world, Citizen a, Citizen b, double trust = 5.0)
    {
        var rel = new Relationship { EntityAId = a.Id, EntityBId = b.Id, Trust = trust, Affection = 20.0, SocialDistance = 30.0 };
        world.Relationships.Add(rel);
        return rel;
    }

    [Fact]
    public void NoCase_ExistsByDefault_WithNoLowTrustRelationship()
    {
        var (world, _, system) = CreateHarness();
        var settlement = AddSettlement(world, legitimacy: 50.0);
        var a = AddCitizen(world, "A", settlement.Id);
        var b = AddCitizen(world, "B", settlement.Id);
        AddDispute(world, a, b, trust: 60.0); // above the dispute threshold

        system.Execute();

        Assert.Empty(world.LegalCases);
    }

    [Fact]
    public void OpensCase_WhenTrustBelowThreshold_WithinSameSettlement()
    {
        var (world, _, system) = CreateHarness();
        var settlement = AddSettlement(world, legitimacy: 0.0); // guarantees no chance-based resolution this tick
        var a = AddCitizen(world, "A", settlement.Id);
        var b = AddCitizen(world, "B", settlement.Id);
        AddDispute(world, a, b);

        system.Execute();

        var legalCase = Assert.Single(world.LegalCases);
        Assert.True(legalCase.IsOpen);
        Assert.Equal(settlement.Id, legalCase.SettlementId);
    }

    [Fact]
    public void DoesNotOpen_WhenCitizensAreInDifferentSettlements()
    {
        var (world, _, system) = CreateHarness();
        var settlementA = AddSettlement(world, legitimacy: 50.0);
        var settlementB = AddSettlement(world, legitimacy: 50.0);
        var a = AddCitizen(world, "A", settlementA.Id);
        var b = AddCitizen(world, "B", settlementB.Id);
        AddDispute(world, a, b);

        system.Execute();

        Assert.Empty(world.LegalCases);
    }

    [Fact]
    public void DoesNotOpen_WhenACitizenHasNoHomeSettlement()
    {
        var (world, _, system) = CreateHarness();
        var a = AddCitizen(world, "A", homeSettlementId: null);
        var b = AddCitizen(world, "B", homeSettlementId: null);
        AddDispute(world, a, b);

        system.Execute();

        Assert.Empty(world.LegalCases);
    }

    [Fact]
    public void Resolves_WhenLegitimacyIsMaximal_AndRestoresTrust()
    {
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, legitimacy: 100.0); // resolutionChance = 1.0, guaranteed
        var a = AddCitizen(world, "A", settlement.Id);
        var b = AddCitizen(world, "B", settlement.Id);
        var rel = AddDispute(world, a, b, trust: 5.0);

        var resolved = false;
        bus.Subscribe<CaseResolvedEvent>(_ => resolved = true);

        system.Execute(); // opens the case
        system.Execute(); // resolves it (guaranteed at Legitimacy 100)

        var legalCase = Assert.Single(world.LegalCases);
        Assert.False(legalCase.IsOpen);
        Assert.True(legalCase.WasResolvedFairly);
        Assert.True(resolved);
        Assert.True(rel.Trust > 5.0, $"Expected Trust to be restored on resolution, but stayed at {rel.Trust}");
    }

    [Fact]
    public void FailsAsJusticeFailure_WhenLegitimacyIsZero_AfterUnresolvedWindow()
    {
        // A dispute that never resolves and never fixes the underlying low
        // Trust will keep re-opening new cases after each failure - RFC-005
        // doesn't specify a cooldown, and a persisting real dispute
        // resurfacing is a defensible emergent behavior, not a bug. This
        // test only asserts the *first* case fails correctly, not that
        // exactly one case exists in total.
        var (world, bus, system) = CreateHarness();
        var settlement = AddSettlement(world, legitimacy: 0.0); // resolutionChance = 0.0, never resolves by chance
        var a = AddCitizen(world, "A", settlement.Id);
        var b = AddCitizen(world, "B", settlement.Id);
        AddDispute(world, a, b);

        var failedCount = 0;
        bus.Subscribe<JusticeFailureEvent>(_ => failedCount++);

        for (var year = 0; year <= 4; year++)
        {
            world.CurrentTime = SimulationTime.FromTick(year * SimulationTime.TicksPerYear);
            system.Execute();
        }

        var firstCase = world.LegalCases.OrderBy(c => c.OpenedTick).First();
        Assert.False(firstCase.IsOpen);
        Assert.False(firstCase.WasResolvedFairly);
        Assert.True(failedCount >= 1);
    }

    [Fact]
    public void DoesNotDuplicate_AnAlreadyOpenCase_ForTheSamePair()
    {
        var (world, _, system) = CreateHarness();
        var settlement = AddSettlement(world, legitimacy: 0.0);
        var a = AddCitizen(world, "A", settlement.Id);
        var b = AddCitizen(world, "B", settlement.Id);
        AddDispute(world, a, b);

        system.Execute();
        system.Execute();

        Assert.Single(world.LegalCases);
    }
}
