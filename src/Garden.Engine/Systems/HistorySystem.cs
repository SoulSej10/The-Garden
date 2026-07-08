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
