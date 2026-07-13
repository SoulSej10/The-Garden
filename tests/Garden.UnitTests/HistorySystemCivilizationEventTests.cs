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

    [Fact]
    public void ArchivedRecord_StoresTheRealLocationY_NotLocationXPlusOne()
    {
        // Regression test found while live-verifying Week 10 (Day 48):
        // Archive()'s LocationY used to be hardcoded to locationX + 1,
        // completely ignoring the real Y coordinate - a bug affecting
        // every one of HistorySystem's ~23 pre-existing Archive() call
        // sites, not just the two Forest handlers added this week. No
        // prior test asserted on LocationY, which is exactly why this
        // went unnoticed for nine weeks.
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new ForestExpandedEvent
        {
            Tick = 1000,
            TileX = 9,
            TileY = 34,
            AreaExpanded = 1
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal(9, record.LocationX);
        Assert.Equal(34, record.LocationY);
    }

    [Fact]
    public void ForestExpanded_IsArchived()
    {
        // Regression test per RFC-006: ForestExpandedEvent already existed
        // and was already published by EcologySystem, but had zero
        // subscribers anywhere - the same TG-001 Law IV violation Day 1
        // fixed, predating this development cycle rather than introduced by it.
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new ForestExpandedEvent
        {
            Tick = 2000,
            TileX = 15,
            TileY = 22,
            AreaExpanded = 1
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("ForestExpanded", record.EventType);
        Assert.Equal(HistoryCategories.Nature, record.Category);
    }

    [Fact]
    public void ForestDeclined_IsArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new ForestDeclinedEvent
        {
            Tick = 3000,
            TileX = 8,
            TileY = 9,
            AreaLost = 1
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("ForestDeclined", record.EventType);
        Assert.Equal(HistoryCategories.Nature, record.Category);
    }

    [Fact]
    public void BorderContracted_IsArchived()
    {
        // Regression test per RFC-007: subscribed at introduction time this
        // time, rather than being found missing later like DialectFormed
        // (Week 7) and ForestExpanded/Declined (Week 10) both were.
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new BorderContractedEvent
        {
            Tick = 4000,
            SettlementId = GameEntityId.New(),
            SettlementName = "Rivermoot",
            NewTerritorySize = 4
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("BorderContracted", record.EventType);
        Assert.Equal(HistoryCategories.Settlement, record.Category);
    }

    [Fact]
    public void BorderDisputeBegins_IsArchived()
    {
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new BorderDisputeBeginsEvent
        {
            Tick = 4500,
            SettlementAId = GameEntityId.New(),
            SettlementAName = "Upperridge",
            SettlementBId = GameEntityId.New(),
            SettlementBName = "Newdale"
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("BorderDisputeBegins", record.EventType);
        Assert.Equal(HistoryCategories.Diplomacy, record.Category);
    }

    [Fact]
    public void SeasonChanged_IsArchived()
    {
        // Regression test, Week 12 Day 61 (leftover consolidation sweep):
        // SeasonChangedEvent has been published by SeasonSystem since
        // before this development cycle began, but - unlike
        // ForestExpanded/Declined, which shared this exact gap and were
        // fixed Week 10 - it had never been noticed as unsubscribed here.
        var (bus, archive, _) = CreateHarness();

        bus.Publish(new SeasonChangedEvent
        {
            Tick = 5000,
            PreviousSeason = Garden.Core.Time.Season.Spring,
            NewSeason = Garden.Core.Time.Season.Summer
        });

        var record = Assert.Single(archive.Records);
        Assert.Equal("SeasonChanged", record.EventType);
        Assert.Equal(HistoryCategories.Nature, record.Category);
    }
}
