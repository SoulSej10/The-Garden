using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Engine.Events;
using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// Regression tests for the Day-1 fix: civilization events (kingdom, leadership,
/// diplomacy, trade routes, migration, technology, religion, culture) were
/// previously published to IEventBus but never archived by HistorySystem - a
/// direct violation of TG-001 Law IV ("History Is Permanent"). These tests prove
/// the subscription now exists and that the significance filtering behaves as
/// documented in DEVELOPMENT_PLAN.md Day 1.
/// </summary>
public class HistorySystemCivilizationEventTests
{
    private static (EventBus bus, HistoricalArchive archive, HistorySystem system) CreateHarness()
    {
        var worldState = new WorldState();
        var eventBus = new EventBus();
        var archive = new HistoricalArchive();
        var evaluator = new SignificanceEvaluator();
        var timeline = new TimelineService(archive);
        var memory = new MemoryService(worldState, archive);
        var storyEngine = new StoryEngine(archive);
        var historyManager = new HistoryManager(archive, evaluator, timeline, memory, storyEngine);
        var system = new HistorySystem(worldState, historyManager, eventBus, NullLogger<HistorySystem>.Instance);

        return (eventBus, archive, system);
    }

    [Fact]
    public void KingdomFounded_IsArchivedAsHighImportance()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new KingdomFoundedEvent
        {
            Tick = 100,
            KingdomId = GameEntityId.New(),
            KingdomName = "Aldoria",
            CapitalSettlementId = GameEntityId.New(),
            CapitalName = "Rivermoot",
            LeaderId = GameEntityId.New(),
            LeaderName = "Elyra",
            MemberCount = 3
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("KingdomFounded", record.EventType);
        Assert.Equal("High", record.Importance);
        Assert.Contains("Aldoria", record.Title);
    }

    [Fact]
    public void KingdomDissolved_IsArchivedAsHighImportance()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new KingdomDissolvedEvent
        {
            Tick = 200,
            KingdomId = GameEntityId.New(),
            KingdomName = "Aldoria",
            Reason = "Capital abandoned"
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("KingdomDissolved", record.EventType);
        Assert.Equal("High", record.Importance);
    }

    [Fact]
    public void LeaderElected_FirstLeader_IsNotArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new LeaderElectedEvent
        {
            Tick = 10,
            CitizenId = GameEntityId.New(),
            CitizenName = "Toma",
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            PreviousLeaderName = "",
            ContributionScore = 12.0
        });

        Assert.Empty(archive.Records);
    }

    /// <summary>
    /// Regression test for a bug found 2026-07-09 while cross-referencing
    /// LeadershipService for DEVELOPMENT_PLAN Day 9: LeadershipService.
    /// EvaluateLeadership actually publishes the literal string "None" (not
    /// an empty string) as PreviousLeaderName when there was no previous
    /// leader. The original OnLeaderElected only checked IsNullOrEmpty, so
    /// every settlement's real first leader was silently mislabeled as
    /// "succeeding None" and archived. This is the real production shape of
    /// the event - the "" case above cannot actually happen from
    /// LeadershipService, only this "None" case can.
    /// </summary>
    [Fact]
    public void LeaderElected_FirstLeaderWithNoneSentinel_IsNotArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new LeaderElectedEvent
        {
            Tick = 10,
            CitizenId = GameEntityId.New(),
            CitizenName = "Toma",
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            PreviousLeaderName = "None",
            ContributionScore = 12.0
        });

        Assert.Empty(archive.Records);
    }

    [Fact]
    public void LeaderElected_Succession_IsArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new LeaderElectedEvent
        {
            Tick = 500,
            CitizenId = GameEntityId.New(),
            CitizenName = "Sela",
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            PreviousLeaderName = "Toma",
            ContributionScore = 20.0
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("LeaderElected", record.EventType);
        Assert.Equal("Medium", record.Importance);
        Assert.Contains("Succeeds", record.Title);
    }

    [Fact]
    public void GovernmentFormed_InitialFormation_IsNotArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new GovernmentFormedEvent
        {
            Tick = 10,
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            GovernmentType = "Informal Community",
            PreviousGovernmentType = ""
        });

        Assert.Empty(archive.Records);
    }

    [Fact]
    public void GovernmentFormed_Transition_IsArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new GovernmentFormedEvent
        {
            Tick = 400,
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            GovernmentType = "Council",
            PreviousGovernmentType = "Informal Community"
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("GovernmentFormed", record.EventType);
        Assert.Equal("Medium", record.Importance);
    }

    [Fact]
    public void DiplomaticRelationChanged_RoutineDrift_IsNotArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new DiplomaticRelationChangedEvent
        {
            Tick = 300,
            EntityAId = GameEntityId.New(),
            EntityAName = "Rivermoot",
            EntityBId = GameEntityId.New(),
            EntityBName = "Oakhaven",
            PreviousRelation = "Neutral",
            NewRelation = "Friendly",
            IsSettlementLevel = true
        });

        Assert.Empty(archive.Records);
    }

    [Fact]
    public void DiplomaticRelationChanged_ToHostile_IsArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new DiplomaticRelationChangedEvent
        {
            Tick = 300,
            EntityAId = GameEntityId.New(),
            EntityAName = "Rivermoot",
            EntityBId = GameEntityId.New(),
            EntityBName = "Oakhaven",
            PreviousRelation = "Suspicious",
            NewRelation = "Hostile",
            IsSettlementLevel = true
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("DiplomaticRelationChanged", record.EventType);
        Assert.Equal("High", record.Importance);
    }

    [Fact]
    public void TechnologyDiscovered_IsArchivedAsHighImportance()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new TechnologyDiscoveredEvent
        {
            Tick = 700,
            TechnologyId = "bronze-working",
            TechnologyName = "Bronze Working",
            Category = "Metallurgy",
            DiscoveredBySettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot"
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("TechnologyDiscovered", record.EventType);
        Assert.Equal("High", record.Importance);
    }

    [Fact]
    public void ReligionEstablished_IsArchivedAsHighImportance()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new ReligionEstablishedEvent
        {
            Tick = 800,
            ReligionId = "faith-of-the-river",
            ReligionName = "Faith of the River",
            CoreValue = "Renewal",
            OriginSettlementName = "Rivermoot",
            InitialFollowers = 5
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("ReligionEstablished", record.EventType);
        Assert.Equal("High", record.Importance);
    }

    [Fact]
    public void TradeRouteEstablished_IsArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new TradeRouteEstablishedEvent
        {
            Tick = 250,
            RouteId = GameEntityId.New(),
            FromSettlementId = GameEntityId.New(),
            FromSettlementName = "Rivermoot",
            ToSettlementId = GameEntityId.New(),
            ToSettlementName = "Oakhaven",
            PrimaryGood = "Grain",
            Distance = 12.0
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("TradeRouteEstablished", record.EventType);
        Assert.Equal("Medium", record.Importance);
    }

    [Fact]
    public void TradeRouteAbandoned_IsNotArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new TradeRouteAbandonedEvent
        {
            Tick = 260,
            RouteId = GameEntityId.New(),
            FromSettlementName = "Rivermoot",
            ToSettlementName = "Oakhaven",
            Reason = "No longer profitable"
        });

        Assert.Empty(archive.Records);
    }

    [Fact]
    public void MigrationEvents_AreNotArchivedIndividually()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new MigrationStartedEvent
        {
            Tick = 50,
            CitizenId = GameEntityId.New(),
            CitizenName = "Doran",
            FromSettlementId = GameEntityId.New(),
            FromSettlementName = "Rivermoot",
            Reason = "Overcrowding",
            FromX = 5,
            FromY = 5
        });
        bus.Publish(new MigrationCompletedEvent
        {
            Tick = 55,
            CitizenId = GameEntityId.New(),
            CitizenName = "Doran",
            ToSettlementId = GameEntityId.New(),
            ToSettlementName = "Oakhaven",
            ToX = 8,
            ToY = 8
        });

        Assert.Empty(archive.Records);
    }

    [Fact]
    public void CulturalFestivalHeld_IsArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new CulturalFestivalHeldEvent
        {
            Tick = 900,
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            FestivalName = "Harvest Rite",
            Occasion = "Autumn Equinox",
            ParticipantCount = 40
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("CulturalFestivalHeld", record.EventType);
        Assert.Equal("Medium", record.Importance);
    }

    [Fact]
    public void DialectFormed_IsArchived()
    {
        // Regression test for the 2026-07-10 anomaly audit: DialectFormedEvent
        // (Week 6, RFC-003) was added to CivilizationEvents.cs but never
        // subscribed by HistorySystem, unlike every other CivilizationEvent -
        // a reintroduction of the exact TG-001 Law IV violation Day 1 fixed.
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new DialectFormedEvent
        {
            Tick = 5000,
            SettlementAId = GameEntityId.New(),
            SettlementAName = "Upperridge",
            SettlementBId = GameEntityId.New(),
            SettlementBName = "Newdale",
            Divergence = 71.5
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("DialectFormed", record.EventType);
        Assert.Equal(HistoryCategories.Culture, record.Category);
        Assert.Contains("Upperridge", record.Title);
        Assert.Contains("Newdale", record.Title);
    }
}
