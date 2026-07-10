using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Core.Time;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class HistorySystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly HistoryManager _historyManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<HistorySystem> _logger;
    private long _nextExecutionTick;
    private long _lastStoryGenerationTick;

    public string Name => "HistorySystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    public HistorySystem(
        WorldState worldState,
        HistoryManager historyManager,
        IEventBus eventBus,
        ILogger<HistorySystem> logger)
    {
        _worldState = worldState;
        _historyManager = historyManager;
        _eventBus = eventBus;
        _logger = logger;

        SubscribeToEvents();
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        if (tick - _lastStoryGenerationTick >= 168)
        {
            GenerateStories();
            _lastStoryGenerationTick = tick;
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void SubscribeToEvents()
    {
        _eventBus.Subscribe<CitizenSpawnedEvent>(OnCitizenSpawned);
        _eventBus.Subscribe<CitizenBornEvent>(OnCitizenBorn);
        _eventBus.Subscribe<CitizenAgedEvent>(OnCitizenAged);
        _eventBus.Subscribe<CitizenDiedEvent>(OnCitizenDied);
        _eventBus.Subscribe<SettlementFoundedEvent>(OnSettlementFounded);
        _eventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted);
        _eventBus.Subscribe<FarmHarvestedEvent>(OnFarmHarvested);
        _eventBus.Subscribe<TradeCompletedEvent>(OnTradeCompleted);
        _eventBus.Subscribe<SettlementExpandedEvent>(OnSettlementExpanded);
        _eventBus.Subscribe<GoodsCraftedEvent>(OnGoodsCrafted);

        // Civilization events (Leadership/Governance/Kingdom/Diplomacy/Trade Route/
        // Migration/Technology/Religion/Culture services) were previously published to
        // IEventBus but had no subscriber here, so they never reached HistoricalArchive -
        // a direct violation of TG-001 Law IV ("History Is Permanent"). Wiring them in now.
        _eventBus.Subscribe<LeaderElectedEvent>(OnLeaderElected);
        _eventBus.Subscribe<GovernmentFormedEvent>(OnGovernmentFormed);
        _eventBus.Subscribe<KingdomFoundedEvent>(OnKingdomFounded);
        _eventBus.Subscribe<KingdomDissolvedEvent>(OnKingdomDissolved);
        _eventBus.Subscribe<DiplomaticRelationChangedEvent>(OnDiplomaticRelationChanged);
        _eventBus.Subscribe<TradeRouteEstablishedEvent>(OnTradeRouteEstablished);
        _eventBus.Subscribe<TradeRouteAbandonedEvent>(OnTradeRouteAbandoned);
        _eventBus.Subscribe<MigrationStartedEvent>(OnMigrationStarted);
        _eventBus.Subscribe<MigrationCompletedEvent>(OnMigrationCompleted);
        _eventBus.Subscribe<TechnologyDiscoveredEvent>(OnTechnologyDiscovered);
        _eventBus.Subscribe<ReligionEstablishedEvent>(OnReligionEstablished);
        _eventBus.Subscribe<CulturalFestivalHeldEvent>(OnCulturalFestivalHeld);
    }

    private void OnCitizenBorn(CitizenBornEvent e)
    {
        // Previously ReproductionSystem published CitizenBornEvent but
        // nothing subscribed to it - Births Recorded stayed at 0 in History
        // even while population was genuinely growing.
        Archive(HistoryCategories.Birth, "CitizenBorn", $"{e.CitizenName} Is Born",
            $"{e.CitizenName} was born at ({e.TileX}, {e.TileY}).",
            string.Empty, e.TileX, e.Tick,
            [e.CitizenId.Value.ToString(), e.ParentAId.Value.ToString(), e.ParentBId.Value.ToString()],
            [e.CitizenName], 4.0,
            e.SettlementId?.Value.ToString() ?? "");

        _historyManager.Memory.AddCitizenMemory(
            e.CitizenId.Value.ToString(), e.Tick, "Born",
            "Birth", $"You were born at ({e.TileX}, {e.TileY}).", 0.9);
    }

    private void OnCitizenAged(CitizenAgedEvent e)
    {
        // Only archive entry into a new life stage (Child/Teen/Adult/Elder),
        // not every single year of aging - a life story with a milestone
        // every year would drown out everything else in the timeline.
        if (e.LifeStage is not ("Child" or "Teen" or "Adult" or "Elder")) return;
        if (e.NewAge is not (2 or 13 or 18 or 60)) return;

        var milestone = e.LifeStage switch
        {
            "Child" => "began childhood",
            "Teen" => "became a teenager",
            "Adult" => "reached adulthood",
            "Elder" => "became an elder",
            _ => "grew older"
        };

        Archive(HistoryCategories.Discovery, "CitizenAged", $"{e.CitizenName} {milestone}",
            $"{e.CitizenName} {milestone} at age {e.NewAge}.",
            string.Empty, 0, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 1.5);
    }

    private void OnCitizenSpawned(CitizenSpawnedEvent e)
    {
        Archive(HistoryCategories.Birth, "CitizenSpawned", $"New Citizen Appears",
            $"{e.CitizenName} arrived in the world at ({e.TileX}, {e.TileY}).",
            string.Empty, e.TileX, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 0.3);

        _historyManager.Memory.AddCitizenMemory(
            e.CitizenId.Value.ToString(), e.Tick, "Spawned",
            "Arrival", $"You arrived in the world at ({e.TileX}, {e.TileY}).",
            0.8);
    }

    private void OnCitizenDied(CitizenDiedEvent e)
    {
        Archive(HistoryCategories.Death, "CitizenDied", $"{e.CitizenName} Has Passed",
            $"{e.CitizenName} died at age {e.AgeAtDeath} from {e.CauseOfDeath}.",
            string.Empty, 0, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 5.0);
    }

    private void OnSettlementFounded(SettlementFoundedEvent e)
    {
        Archive(HistoryCategories.Settlement, "SettlementFounded",
            $"Foundation of {e.SettlementName}",
            $"{e.FounderName} founded the settlement of {e.SettlementName}.",
            e.SettlementName, e.TileX, e.Tick,
            [e.FounderId.Value.ToString()], [e.FounderName],
            8.0, e.SettlementId.Value.ToString());

        _historyManager.Memory.AddCollectiveMemory(
            e.SettlementId.Value.ToString(), e.SettlementName, e.Tick,
            "Foundation", $"The settlement of {e.SettlementName} was founded.",
            $"The settlement of {e.SettlementName} was founded by {e.FounderName}.", 10.0);
    }

    private void OnBuildingCompleted(BuildingCompletedEvent e)
    {
        Archive(HistoryCategories.Building, "BuildingCompleted",
            $"{e.BuildingType} Completed",
            $"A {e.BuildingType} was completed in {e.SettlementName}.",
            e.SettlementName, 0, e.Tick,
            [], [], 4.0, e.SettlementId.Value.ToString());
    }

    private void OnFarmHarvested(FarmHarvestedEvent e)
    {
        Archive(HistoryCategories.Harvest, "FarmHarvested",
            $"Harvest at {e.SettlementName}",
            $"{e.CropType} harvested: {e.Yield} units.",
            e.SettlementName, 0, e.Tick,
            [], [], 2.0, e.SettlementId.Value.ToString());
    }

    private void OnTradeCompleted(TradeCompletedEvent e)
    {
        Archive(HistoryCategories.Trade, "TradeCompleted", "Trade Completed",
            $"Trade of {e.Quantity} {e.ItemType} completed.",
            string.Empty, 0, e.Tick,
            [e.FromCitizenId.Value.ToString(), e.ToCitizenId.Value.ToString()],
            [], 3.0);
    }

    private void OnSettlementExpanded(SettlementExpandedEvent e)
    {
        Archive(HistoryCategories.Settlement, "SettlementExpanded",
            $"{e.SettlementName} Expands",
            $"{e.SettlementName} expanded to territory size {e.NewTerritorySize}.",
            e.SettlementName, 0, e.Tick,
            [], [], 6.0, e.SettlementId.Value.ToString());
    }

    private void OnGoodsCrafted(GoodsCraftedEvent e)
    {
        Archive(HistoryCategories.Trade, "GoodsCrafted", "Goods Produced",
            $"{e.Quantity} {e.Product} crafted.",
            string.Empty, 0, e.Tick, [], [], 1.0);
    }

    private void OnLeaderElected(LeaderElectedEvent e)
    {
        // A settlement's first-ever leader is routine formation, not a historical
        // moment; an actual succession (a previous leader existed) is a political
        // transition worth remembering (TG-008 significance criteria).
        // LeadershipService.EvaluateLeadership publishes the literal string "None"
        // (not an empty string) when there was no previous leader - checked for
        // both, since the original empty-string-only check silently mislabeled
        // every settlement's first leader as "succeeding None" (found 2026-07-09
        // while cross-referencing LeadershipService for DEVELOPMENT_PLAN Day 9).
        var isSuccession = !string.IsNullOrEmpty(e.PreviousLeaderName) && e.PreviousLeaderName != "None";
        var severity = isSuccession ? 6.0 : 2.0;
        var title = isSuccession
            ? $"{e.CitizenName} Succeeds {e.PreviousLeaderName} in {e.SettlementName}"
            : $"{e.CitizenName} Becomes Leader of {e.SettlementName}";
        var description = isSuccession
            ? $"{e.CitizenName} succeeded {e.PreviousLeaderName} as leader of {e.SettlementName}."
            : $"{e.CitizenName} became the first leader of {e.SettlementName}.";

        Archive(HistoryCategories.Politics, "LeaderElected", title, description,
            e.SettlementName, 0, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName],
            severity, e.SettlementId.Value.ToString());
    }

    private void OnGovernmentFormed(GovernmentFormedEvent e)
    {
        // Same reasoning as leadership: a settlement's initial government type is
        // not yet a "transition" (nothing preceded it); an evolution from one form
        // to another is.
        var isTransition = !string.IsNullOrEmpty(e.PreviousGovernmentType);
        if (!isTransition) return;

        Archive(HistoryCategories.Politics, "GovernmentFormed",
            $"{e.SettlementName} Adopts {e.GovernmentType}",
            $"{e.SettlementName} transitioned from {e.PreviousGovernmentType} to {e.GovernmentType}.",
            e.SettlementName, 0, e.Tick,
            [], [], 6.5, e.SettlementId.Value.ToString());
    }

    private void OnKingdomFounded(KingdomFoundedEvent e)
    {
        Archive(HistoryCategories.Politics, "KingdomFounded",
            $"The Kingdom of {e.KingdomName} Is Founded",
            $"{e.LeaderName} founded the Kingdom of {e.KingdomName}, with {e.CapitalName} as its capital ({e.MemberCount} settlements).",
            e.CapitalName, 0, e.Tick,
            [e.LeaderId.Value.ToString()], [e.LeaderName],
            9.5, e.CapitalSettlementId.Value.ToString());

        _historyManager.Memory.AddCollectiveMemory(
            e.KingdomId.Value.ToString(), e.KingdomName, e.Tick,
            "Foundation", $"The Kingdom of {e.KingdomName} was founded.",
            $"The Kingdom of {e.KingdomName} was founded by {e.LeaderName}, uniting {e.MemberCount} settlements.",
            10.0);
    }

    private void OnKingdomDissolved(KingdomDissolvedEvent e)
    {
        Archive(HistoryCategories.Politics, "KingdomDissolved",
            $"The Kingdom of {e.KingdomName} Falls",
            $"The Kingdom of {e.KingdomName} was dissolved: {e.Reason}.",
            string.Empty, 0, e.Tick,
            [], [], 9.0);
    }

    private void OnDiplomaticRelationChanged(DiplomaticRelationChangedEvent e)
    {
        // Only the extremes (Allied/Hostile) are treated as historically significant;
        // routine drift between Neutral/Friendly/Suspicious is diplomatic noise, not
        // history (mirrors TG-008's "citizen blinked" non-example).
        var isExtreme = e.NewRelation is "Allied" or "Hostile";
        var severity = isExtreme ? 7.5 : 3.0;

        Archive(HistoryCategories.Diplomacy, "DiplomaticRelationChanged",
            $"{e.EntityAName} and {e.EntityBName} Become {e.NewRelation}",
            $"Relations between {e.EntityAName} and {e.EntityBName} shifted from {e.PreviousRelation} to {e.NewRelation}.",
            string.Empty, 0, e.Tick, [], [], severity);
    }

    private void OnTradeRouteEstablished(TradeRouteEstablishedEvent e)
    {
        Archive(HistoryCategories.Trade, "TradeRouteEstablished",
            $"Trade Route Opens Between {e.FromSettlementName} and {e.ToSettlementName}",
            $"A trade route in {e.PrimaryGood} was established between {e.FromSettlementName} and {e.ToSettlementName}.",
            e.FromSettlementName, 0, e.Tick,
            [], [], 5.5, e.FromSettlementId.Value.ToString());
    }

    private void OnTradeRouteAbandoned(TradeRouteAbandonedEvent e)
    {
        // Routine economic contraction - passes through evaluation but is not
        // expected to clear the archival threshold, same treatment as TradeCompleted.
        Archive(HistoryCategories.Trade, "TradeRouteAbandoned",
            $"Trade Route Between {e.FromSettlementName} and {e.ToSettlementName} Ends",
            $"The trade route between {e.FromSettlementName} and {e.ToSettlementName} was abandoned: {e.Reason}.",
            e.FromSettlementName, 0, e.Tick, [], [], 3.0);
    }

    private void OnMigrationStarted(MigrationStartedEvent e)
    {
        // Individual migration is frequent and low-stakes at the per-citizen level
        // (TG-008: not every event is History) - it still flows through the same
        // evaluation pipeline, it just doesn't clear the archival bar by itself.
        Archive(HistoryCategories.Migration, "MigrationStarted",
            $"{e.CitizenName} Leaves {e.FromSettlementName}",
            $"{e.CitizenName} departed {e.FromSettlementName}: {e.Reason}.",
            e.FromSettlementName, e.FromX, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 1.5);
    }

    private void OnMigrationCompleted(MigrationCompletedEvent e)
    {
        Archive(HistoryCategories.Migration, "MigrationCompleted",
            $"{e.CitizenName} Arrives at {e.ToSettlementName}",
            $"{e.CitizenName} settled in {e.ToSettlementName}.",
            e.ToSettlementName, e.ToX, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 1.5);
    }

    private void OnTechnologyDiscovered(TechnologyDiscoveredEvent e)
    {
        Archive(HistoryCategories.Technology, "TechnologyDiscovered",
            $"{e.TechnologyName} Is Discovered",
            $"{e.SettlementName} discovered {e.TechnologyName} ({e.Category}).",
            e.SettlementName, 0, e.Tick, [], [], 7.5,
            e.DiscoveredBySettlementId?.Value.ToString() ?? "");
    }

    private void OnReligionEstablished(ReligionEstablishedEvent e)
    {
        Archive(HistoryCategories.Religion, "ReligionEstablished",
            $"{e.ReligionName} Is Established",
            $"{e.ReligionName}, centered on {e.CoreValue}, was established in {e.OriginSettlementName} with {e.InitialFollowers} followers.",
            e.OriginSettlementName, 0, e.Tick, [], [], 8.5);

        _historyManager.Memory.AddCollectiveMemory(
            e.ReligionId, e.ReligionName, e.Tick,
            "Foundation", $"{e.ReligionName} was established.",
            $"{e.ReligionName} was founded in {e.OriginSettlementName}, centered on {e.CoreValue}.",
            9.0);
    }

    private void OnCulturalFestivalHeld(CulturalFestivalHeldEvent e)
    {
        Archive(HistoryCategories.Culture, "CulturalFestivalHeld",
            $"{e.FestivalName} Celebrated in {e.SettlementName}",
            $"{e.SettlementName} held {e.FestivalName} ({e.Occasion}) with {e.ParticipantCount} participants.",
            e.SettlementName, 0, e.Tick, [], [], 4.5, e.SettlementId.Value.ToString());
    }

    private void Archive(string category, string eventType, string title, string description,
        string locationName, int locationOffset, long tick,
        List<string> participantIds, List<string> participantNames,
        double severity, string settlementId = "")
    {
        var importance = _historyManager.Evaluator.Evaluate(eventType, severity);
        if (!_historyManager.Evaluator.ShouldArchive(eventType, importance))
            return;

        var time = _worldState.CurrentTime;
        var record = new HistoricalRecord
        {
            Tick = tick,
            Year = time.Year,
            Day = time.Day,
            Season = time.Season.ToString(),
            EventType = eventType,
            Category = category,
            Title = title,
            Description = description,
            LocationX = locationOffset != 0 ? locationOffset : _worldState.Settlements.FirstOrDefault()?.TileX ?? 0,
            LocationY = locationOffset != 0 ? locationOffset + 1 : _worldState.Settlements.FirstOrDefault()?.TileY ?? 0,
            LocationName = locationName,
            ParticipantIds = participantIds,
            ParticipantNames = participantNames,
            RelatedSettlementId = settlementId,
            Severity = severity,
            Importance = importance
        };

        _historyManager.Archive.Append(record);
        _logger.LogDebug("Archived {EventType}: {Title}", eventType, title);
    }

    private void GenerateStories()
    {
        foreach (var category in new[] {
            HistoryCategories.Settlement, HistoryCategories.Birth,
            HistoryCategories.Death, HistoryCategories.Building,
            HistoryCategories.Disaster, HistoryCategories.Harvest,
            HistoryCategories.Trade, HistoryCategories.Discovery })
        {
            _historyManager.StoryEngine.GenerateStoriesForCategory(category, 2);
        }
    }
}
