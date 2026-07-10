using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Time;
using Garden.Engine.Events;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 3 Day 13: pairwise Relationship entity per
/// TG-380_Relationships.md, created/updated on real interaction events
/// (trade, having a child together) rather than existing between every pair
/// of citizens by default.
/// </summary>
public class RelationshipSystemTests
{
    private static (WorldState world, EventBus bus, RelationshipSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new RelationshipSystem(world, bus);
        return (world, bus, system);
    }

    [Fact]
    public void NoRelationship_ExistsByDefault_BetweenTwoCitizens()
    {
        var (world, _, _) = CreateHarness();
        Assert.Empty(world.Relationships);
    }

    [Fact]
    public void TradeCompleted_CreatesRelationship_AndImprovesTrustAffectionCloseness()
    {
        var (world, bus, _) = CreateHarness();
        var a = GameEntityId.New();
        var b = GameEntityId.New();

        bus.Publish(new TradeCompletedEvent
        {
            Tick = 10,
            FromCitizenId = a,
            ToCitizenId = b,
            ItemType = "Wood",
            Quantity = 5
        });

        var rel = Assert.Single(world.Relationships);
        Assert.True(rel.Trust > 50.0);
        Assert.True(rel.Affection > 50.0);
        Assert.True(rel.SocialDistance < 50.0);
        Assert.Equal(1, rel.InteractionCount);
        Assert.Equal(10, rel.EstablishedTick);
    }

    [Fact]
    public void Relationship_IsStoredOnce_RegardlessOfWhichCitizenIsPassedFirst()
    {
        var (world, bus, system) = CreateHarness();
        var a = GameEntityId.New();
        var b = GameEntityId.New();

        bus.Publish(new TradeCompletedEvent { Tick = 1, FromCitizenId = a, ToCitizenId = b, ItemType = "Wood", Quantity = 1 });
        bus.Publish(new TradeCompletedEvent { Tick = 2, FromCitizenId = b, ToCitizenId = a, ItemType = "Stone", Quantity = 1 });

        var rel = Assert.Single(world.Relationships);
        Assert.Equal(2, rel.InteractionCount);
    }

    [Fact]
    public void CitizenBorn_CreatesStrongerBond_BetweenParents_ThanASingleTrade()
    {
        var (world, bus, _) = CreateHarness();
        var parentA = GameEntityId.New();
        var parentB = GameEntityId.New();

        bus.Publish(new CitizenBornEvent
        {
            Tick = 5,
            CitizenId = GameEntityId.New(),
            CitizenName = "Newborn",
            TileX = 0,
            TileY = 0,
            ParentAId = parentA,
            ParentBId = parentB
        });

        var rel = Assert.Single(world.Relationships);
        Assert.True(rel.Trust > 60.0);
        Assert.True(rel.Affection > 65.0);
    }

    [Fact]
    public void Decay_PullsTrustAndAffectionBackTowardNeutral_AndDistanceBackTowardStrangers_WithoutFurtherContact()
    {
        var (world, bus, system) = CreateHarness();
        var a = GameEntityId.New();
        var b = GameEntityId.New();

        bus.Publish(new TradeCompletedEvent { Tick = 0, FromCitizenId = a, ToCitizenId = b, ItemType = "Wood", Quantity = 1 });
        var rel = world.Relationships.Single();
        var trustAfterTrade = rel.Trust;
        var distanceAfterTrade = rel.SocialDistance;

        for (var i = 24; i < 24 * 500; i += 24)
        {
            world.CurrentTime = SimulationTime.FromTick(i);
            system.Execute();
        }

        Assert.True(rel.Trust < trustAfterTrade,
            $"Expected Trust to decay back toward neutral without further contact, but {trustAfterTrade} -> {rel.Trust}");
        Assert.True(rel.SocialDistance > distanceAfterTrade,
            $"Expected SocialDistance to drift back toward strangers without further contact, but {distanceAfterTrade} -> {rel.SocialDistance}");
    }

    [Fact]
    public void RepeatedInteractions_Accumulate_RatherThanReset()
    {
        var (world, bus, _) = CreateHarness();
        var a = GameEntityId.New();
        var b = GameEntityId.New();

        for (var i = 0; i < 5; i++)
        {
            bus.Publish(new TradeCompletedEvent { Tick = i, FromCitizenId = a, ToCitizenId = b, ItemType = "Wood", Quantity = 1 });
        }

        var rel = world.Relationships.Single();
        Assert.Equal(5, rel.InteractionCount);
        Assert.Equal(4, rel.LastInteractionTick);
    }
}
