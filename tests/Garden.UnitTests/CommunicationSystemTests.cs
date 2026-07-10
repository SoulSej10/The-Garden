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
/// DEVELOPMENT_PLAN.md Week 5 Days 22-23: Knowledge Diffusion per
/// specification/RFC/RFC-002-communication-knowledge-diffusion.md. Milestone
/// events that seed initial knowledge are rare/chance-gated in a live
/// simulation (Kingdom/Religion formation) or currently unreachable within
/// any realistic run (see backlog note on TechnologyService's progress
/// scaling), so - same as RFC-001's EmotionSystemTests - this is verified by
/// publishing the domain events directly rather than waiting on organic
/// emergence.
/// </summary>
public class CommunicationSystemTests
{
    private static (WorldState world, EventBus bus, CommunicationSystem system) CreateHarness()
    {
        var world = new WorldState { CurrentTime = SimulationTime.FromTick(0) };
        var bus = new EventBus();
        var system = new CommunicationSystem(world, bus);
        return (world, bus, system);
    }

    private static Citizen AddCitizen(WorldState world, string firstName = "Test")
    {
        var citizen = new Citizen { FirstName = firstName, LastName = "Citizen", IsAlive = true };
        world.Citizens.Add(citizen);
        return citizen;
    }

    [Fact]
    public void KingdomFounded_MarksLeaderAsKnowing()
    {
        var (world, bus, _) = CreateHarness();
        var leader = AddCitizen(world, "Leader");

        bus.Publish(new KingdomFoundedEvent
        {
            Tick = 0,
            KingdomId = GameEntityId.New(),
            KingdomName = "Testland",
            CapitalSettlementId = GameEntityId.New(),
            CapitalName = "Capital",
            LeaderId = leader.Id,
            LeaderName = "Leader",
            MemberCount = 1
        });

        Assert.Single(leader.KnownEventIds);
    }

    [Fact]
    public void TechnologyDiscovered_MarksDiscovererAsKnowing()
    {
        var (world, bus, _) = CreateHarness();
        var discoverer = AddCitizen(world, "Discoverer");

        bus.Publish(new TechnologyDiscoveredEvent
        {
            Tick = 0,
            TechnologyId = "tech-1",
            TechnologyName = "Stone Tools",
            Category = "Tools",
            DiscoveredBySettlementId = GameEntityId.New(),
            SettlementName = "Settlement",
            DiscoveredByCitizenId = discoverer.Id
        });

        Assert.Single(discoverer.KnownEventIds);
    }

    [Fact]
    public void ReligionEstablished_MarksFounderAsKnowing()
    {
        var (world, bus, _) = CreateHarness();
        var founder = AddCitizen(world, "Founder");

        bus.Publish(new ReligionEstablishedEvent
        {
            Tick = 0,
            ReligionId = "religion-1",
            ReligionName = "Testism",
            CoreValue = "Harmony",
            OriginSettlementName = "Settlement",
            InitialFollowers = 1,
            FounderCitizenId = founder.Id
        });

        Assert.Single(founder.KnownEventIds);
    }

    [Fact]
    public void EventWithNoCitizenId_DoesNotThrow_AndMarksNoOne()
    {
        var (world, bus, _) = CreateHarness();
        AddCitizen(world, "Bystander");

        var ex = Record.Exception(() => bus.Publish(new TechnologyDiscoveredEvent
        {
            Tick = 0,
            TechnologyId = "tech-1",
            TechnologyName = "Stone Tools",
            Category = "Tools",
            DiscoveredBySettlementId = null,
            SettlementName = "Settlement",
            DiscoveredByCitizenId = null
        }));

        Assert.Null(ex);
        Assert.Empty(world.Citizens.Single().KnownEventIds);
    }

    [Fact]
    public void Diffuses_ToCloseTrustedRelationship_WhenSpreadRollSucceeds()
    {
        var (world, bus, system) = CreateHarness();
        var knower = AddCitizen(world, "Knower");
        var listener = AddCitizen(world, "Listener");
        knower.KnownEventIds.Add("Kingdom:test");

        // SocialDistance well under the 40 threshold, Trust well over 30 on
        // both the Relationship and the listener's own EmotionalState -
        // guarantees the gate passes; only the random spread roll remains,
        // so run Execute enough times that a near-certain roll (spreadChance
        // = (100-5)/100 = 0.95) succeeds at least once.
        world.Relationships.Add(new Relationship
        {
            EntityAId = knower.Id,
            EntityBId = listener.Id,
            SocialDistance = 5.0,
            Trust = 80.0,
            Affection = 80.0
        });
        listener.Emotions.Trust = 80.0;

        var diffused = false;
        for (var i = 0; i < 20 && !diffused; i++)
        {
            system.Execute();
            diffused = listener.KnownEventIds.Contains("Kingdom:test");
        }

        Assert.True(diffused, "Expected knowledge to diffuse across a close, trusted relationship within 20 daily cycles");
    }

    [Fact]
    public void DoesNotDiffuse_WhenSocialDistanceTooFar()
    {
        var (world, _, system) = CreateHarness();
        var knower = AddCitizen(world, "Knower");
        var listener = AddCitizen(world, "Listener");
        knower.KnownEventIds.Add("Kingdom:test");

        world.Relationships.Add(new Relationship
        {
            EntityAId = knower.Id,
            EntityBId = listener.Id,
            SocialDistance = 90.0, // well over the 40 threshold - strangers
            Trust = 80.0,
            Affection = 80.0
        });
        listener.Emotions.Trust = 80.0;

        for (var i = 0; i < 20; i++) system.Execute();

        Assert.Empty(listener.KnownEventIds);
    }

    [Fact]
    public void DoesNotDiffuse_WhenTrustTooLow()
    {
        var (world, _, system) = CreateHarness();
        var knower = AddCitizen(world, "Knower");
        var listener = AddCitizen(world, "Listener");
        knower.KnownEventIds.Add("Kingdom:test");

        world.Relationships.Add(new Relationship
        {
            EntityAId = knower.Id,
            EntityBId = listener.Id,
            SocialDistance = 5.0,
            Trust = 10.0, // well under the 30 threshold
            Affection = 80.0
        });
        listener.Emotions.Trust = 10.0;

        for (var i = 0; i < 20; i++) system.Execute();

        Assert.Empty(listener.KnownEventIds);
    }

    [Fact]
    public void DoesNotDiffuse_ToDeadCitizen()
    {
        var (world, _, system) = CreateHarness();
        var knower = AddCitizen(world, "Knower");
        var listener = AddCitizen(world, "Listener");
        listener.IsAlive = false;
        knower.KnownEventIds.Add("Kingdom:test");

        world.Relationships.Add(new Relationship
        {
            EntityAId = knower.Id,
            EntityBId = listener.Id,
            SocialDistance = 5.0,
            Trust = 80.0,
            Affection = 80.0
        });

        for (var i = 0; i < 20; i++) system.Execute();

        Assert.Empty(listener.KnownEventIds);
    }

    [Fact]
    public void EventTitles_AreTrackedForObservatorySurfacing()
    {
        var (world, bus, system) = CreateHarness();
        var leader = AddCitizen(world, "Leader");

        bus.Publish(new KingdomFoundedEvent
        {
            Tick = 0,
            KingdomId = GameEntityId.New(),
            KingdomName = "Testland",
            CapitalSettlementId = GameEntityId.New(),
            CapitalName = "Capital",
            LeaderId = leader.Id,
            LeaderName = "Leader",
            MemberCount = 1
        });

        var key = leader.KnownEventIds.Single();
        Assert.True(system.EventTitles.ContainsKey(key));
        Assert.Equal("The Kingdom of Testland Is Founded", system.EventTitles[key]);
    }
}
