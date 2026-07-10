using Garden.Core.Identifiers;
using Garden.Engine.Events;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// Regression tests for DEVELOPMENT_PLAN.md Week 2 Day 9: GovernanceService
/// previously only tracked a 4-item population-threshold GovernmentType
/// lookup (TG-580_Politics_Governance.md's Authority Sources and Legitimacy
/// concepts were entirely unmodeled). These tests cover the new
/// AuthoritySource progression and the Legitimacy formula documented inline
/// on GovernanceService.CalculateLegitimacy.
/// </summary>
public class GovernanceLegitimacyTests
{
    private static (WorldState world, GovernanceService governance) CreateHarness()
    {
        var world = new WorldState();
        var eventBus = new EventBus();
        var governance = new GovernanceService(world, eventBus, NullLogger<GovernanceService>.Instance);
        return (world, governance);
    }

    [Theory]
    [InlineData(4, "Informal Community", "Competence")]
    [InlineData(5, "Council", "Election")]
    [InlineData(10, "Village Chief", "Tradition")]
    [InlineData(20, "Elder Assembly", "Tradition")]
    public void EvaluateGovernance_SetsAuthoritySource_AlongsideGovernmentType(
        int population, string expectedGovernment, string expectedAuthority)
    {
        var (world, governance) = CreateHarness();
        var settlement = new Settlement { Name = "Rivermoot" };
        for (var i = 0; i < population; i++)
            settlement.MemberIds.Add(GameEntityId.New());

        governance.EvaluateGovernance(settlement, tick: 100);

        Assert.Equal(expectedGovernment, settlement.GovernmentType);
        Assert.Equal(expectedAuthority, settlement.AuthoritySource);
    }

    [Fact]
    public void EvaluateGovernance_RecordsLastGovernmentChangeTick_OnTransition()
    {
        var (world, governance) = CreateHarness();
        var settlement = new Settlement { Name = "Rivermoot" };
        for (var i = 0; i < 5; i++) settlement.MemberIds.Add(GameEntityId.New());

        governance.EvaluateGovernance(settlement, tick: 250);

        Assert.Equal(250, settlement.LastGovernmentChangeTick);
    }

    [Fact]
    public void Legitimacy_IsLow_ImmediatelyAfterGovernmentUpheaval_WithNoLeader()
    {
        var (world, governance) = CreateHarness();
        var settlement = new Settlement { Name = "Rivermoot" };
        for (var i = 0; i < 5; i++) settlement.MemberIds.Add(GameEntityId.New());

        governance.EvaluateGovernance(settlement, tick: 1000);

        // No leader yet (default competence=30, default trust=50) and zero
        // ticks of stability since the transition just happened at tick 1000:
        // 30*0.4 + 50*0.3 + 0*0.3 = 27.
        Assert.Equal(27.0, settlement.Legitimacy, precision: 1);
    }

    [Fact]
    public void Legitimacy_Increases_AsStabilityAccumulates_WithoutAnotherTransition()
    {
        var (world, governance) = CreateHarness();
        var settlement = new Settlement { Name = "Rivermoot" };
        for (var i = 0; i < 5; i++) settlement.MemberIds.Add(GameEntityId.New());

        governance.EvaluateGovernance(settlement, tick: 1000); // triggers transition, resets clock
        var justAfter = settlement.Legitimacy;

        governance.EvaluateGovernance(settlement, tick: 1500); // +500 ticks, no new transition
        var later = settlement.Legitimacy;

        Assert.True(later > justAfter,
            $"Expected legitimacy to grow as the government remains stable, but {justAfter} -> {later}");
    }

    [Fact]
    public void Legitimacy_ReflectsLeaderCompetenceAndReputation_WhenLeaderExists()
    {
        var (world, governance) = CreateHarness();
        var leader = new Citizen { FirstName = "Sela", LastName = "Vane", ContributionScore = 80, Reputation = 90 };
        world.Citizens.Add(leader);

        var settlement = new Settlement { Name = "Rivermoot", LeaderId = leader.Id };
        for (var i = 0; i < 5; i++) settlement.MemberIds.Add(GameEntityId.New());

        governance.EvaluateGovernance(settlement, tick: 1000);

        // competence=80, trust=90, stability=0 (just transitioned):
        // 80*0.4 + 90*0.3 + 0*0.3 = 59.
        Assert.Equal(59.0, settlement.Legitimacy, precision: 1);
    }

    [Fact]
    public void DiplomacyScoreChange_IsPenalized_WhenEitherSettlementLacksLegitimacy()
    {
        var world = new WorldState();
        var eventBus = new EventBus();
        var diplomacy = new DiplomacyService(world, eventBus, NullLogger<DiplomacyService>.Instance);

        var a = new Settlement { Name = "A", TileX = 0, TileY = 0, Legitimacy = 20.0 };
        var b = new Settlement { Name = "B", TileX = 3, TileY = 3, Legitimacy = 80.0 };
        world.Settlements.Add(a);
        world.Settlements.Add(b);

        diplomacy.EvaluateDiplomacy(tick: 100);

        var relation = world.DiplomaticRelations.Single();
        // Base drift for two unlead, non-kingdom, close (dist<=8) settlements
        // would be +0.03 (distance bonus only); the low-legitimacy penalty
        // (-0.03) should cancel it out to ~50 (no net change from the 50 default).
        Assert.Equal(50.0, relation.RelationScore, precision: 1);
    }
}
