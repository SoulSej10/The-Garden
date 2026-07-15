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

        // 2026-07-10 anomaly audit: DialectFormedEvent (Week 6, RFC-003) was
        // added to CivilizationEvents.cs but never subscribed here, unlike
        // the 12 events above - the exact TG-001 Law IV violation Week 1
        // Day 1 was created to close, reintroduced on new code.
        _eventBus.Subscribe<DialectFormedEvent>(OnDialectFormed);

        // RFC-006 (specification/RFC/RFC-006-life-sciences-flora-history.md):
        // ForestExpandedEvent/ForestDeclinedEvent were already published by
        // EcologySystem from real, rare, gated conditions, but had zero
        // subscribers anywhere - the same TG-001 Law IV violation Week 1
        // Day 1 fixed for civilization events, this time predating this
        // whole development cycle. ResourceRegeneratedEvent is deliberately
        // NOT subscribed here - it fires too frequently and would flood the
        // archive the same way FarmHarvested did before Week 4 Day 18.
        _eventBus.Subscribe<ForestExpandedEvent>(OnForestExpanded);
        _eventBus.Subscribe<ForestDeclinedEvent>(OnForestDeclined);

        // RFC-007 (specification/RFC/RFC-007-borders-territorial-influence.md):
        // subscribed at introduction time this time, rather than being found
        // missing later - the same TG-001 Law IV pattern Week 1 Day 1 and
        // Week 7 Day 31 both had to fix after the fact for earlier RFCs.
        _eventBus.Subscribe<BorderContractedEvent>(OnBorderContracted);
        _eventBus.Subscribe<BorderDisputeBeginsEvent>(OnBorderDisputeBegins);

        // Week 12 Day 61 (leftover consolidation sweep): SeasonChangedEvent
        // has been published by SeasonSystem since before this development
        // cycle began, but - unlike ForestExpanded/Declined, which shared
        // this exact gap and were fixed Week 10 - it had never been noticed
        // as unsubscribed here. A fourth instance of the TG-001 Law IV
        // violation this project keeps finding in its own new/old code.
        _eventBus.Subscribe<SeasonChangedEvent>(OnSeasonChanged);

        // RFC-008 (specification/RFC/RFC-008-population-ecology-carrying-capacity.md):
        // subscribed at introduction time, continuing the practice
        // reinforced Week 12 Day 61 rather than risking a fifth instance of
        // the same TG-001 Law IV gap.
        _eventBus.Subscribe<PopulationDeclineEvent>(OnPopulationDecline);
        _eventBus.Subscribe<PopulationBoomEvent>(OnPopulationBoom);

        // RFC-009 (specification/RFC/RFC-009-disease-health-overcrowding.md):
        // subscribed at introduction time, continuing the practice
        // reinforced Week 12 Day 61 rather than risking a sixth instance of
        // the same TG-001 Law IV gap.
        _eventBus.Subscribe<OrganismInfectedEvent>(OnOrganismInfected);
        _eventBus.Subscribe<DiseaseRecoveredEvent>(OnDiseaseRecovered);
        _eventBus.Subscribe<EpidemicStartedEvent>(OnEpidemicStarted);
        _eventBus.Subscribe<EpidemicContainedEvent>(OnEpidemicContained);

        // RFC-010 (specification/RFC/RFC-010-evolution-adaptive-drift.md):
        // subscribed at introduction time, continuing the practice
        // reinforced Week 12 Day 61 rather than risking a seventh instance
        // of the same TG-001 Law IV gap.
        _eventBus.Subscribe<AdaptiveShiftObservedEvent>(OnAdaptiveShiftObserved);
        _eventBus.Subscribe<EvolutionaryStagnationEvent>(OnEvolutionaryStagnation);

        // RFC-011 (specification/RFC/RFC-011-decomposers-soil-health.md):
        // subscribed at introduction time, continuing the practice
        // reinforced Week 12 Day 61 rather than risking an eighth instance
        // of the same TG-001 Law IV gap.
        _eventBus.Subscribe<NutrientPulseOccurredEvent>(OnNutrientPulseOccurred);
        _eventBus.Subscribe<OrganicMatterAccumulatedEvent>(OnOrganicMatterAccumulated);
        _eventBus.Subscribe<WasteFullyDecomposedEvent>(OnWasteFullyDecomposed);

        // RFC-012 (specification/RFC/RFC-012-fauna-aggregate-wildlife.md):
        // subscribed at introduction time, continuing the same practice.
        _eventBus.Subscribe<SpeciesExpandedEvent>(OnSpeciesExpanded);
        _eventBus.Subscribe<AnimalDiedEvent>(OnAnimalDied);

        // Week 19 leftover-consolidation sweep: a full audit (comparing
        // every CivilizationEvent-derived record type against this
        // system's subscriptions) found four real, active events that had
        // been publishing since Week 8 (Education) and Week 9 (Law) with
        // no subscriber here - the same TG-001 Law IV violation found and
        // fixed four times before (Week 1, Week 7, Week 10, Week 12 Day
        // 61), just never caught for these specific events until now.
        _eventBus.Subscribe<ApprenticeshipStartedEvent>(OnApprenticeshipStarted);
        _eventBus.Subscribe<ApprenticeshipCompletedEvent>(OnApprenticeshipCompleted);
        _eventBus.Subscribe<CaseResolvedEvent>(OnCaseResolved);
        _eventBus.Subscribe<JusticeFailureEvent>(OnJusticeFailure);

        // RFC-013 (specification/RFC/RFC-013-warfare-dispute-escalation.md):
        // subscribed at introduction time, continuing the practice
        // reinforced Week 12 Day 61.
        _eventBus.Subscribe<WarDeclaredEvent>(OnWarDeclared);
        _eventBus.Subscribe<BattleFoughtEvent>(OnBattleFought);
        _eventBus.Subscribe<PeaceNegotiatedEvent>(OnPeaceNegotiated);

        // RFC-014 (specification/RFC/RFC-014-infrastructure-route-quality.md):
        // subscribed at introduction time, continuing the practice
        // reinforced Week 12 Day 61.
        _eventBus.Subscribe<RoadConstructedEvent>(OnRoadConstructed);
        _eventBus.Subscribe<InfrastructureFailureEvent>(OnInfrastructureFailure);

        // Week 23 leftover-consolidation sweep: BuildingPlannedEvent has been
        // published by ConstructionSystem.PlanBuilding since before this
        // development cycle began, and "BuildingPlanned" already sat in
        // SignificanceEvaluator's always-Medium whitelist (implying it was
        // always meant to be archived) - but nothing ever subscribed it here.
        // The same TG-001 Law IV violation found and fixed nine times before
        // now (Week 1, Week 7, Week 10, Week 12 Day 61, Week 19 Day 92 x4).
        _eventBus.Subscribe<BuildingPlannedEvent>(OnBuildingPlanned);

        // RFC-015 (specification/RFC/RFC-015-technology-independent-discovery.md):
        // subscribed at introduction time, continuing the practice
        // reinforced Week 12 Day 61.
        _eventBus.Subscribe<TechnologicalDivergenceEvent>(OnTechnologicalDivergence);

        // RFC-016 (specification/RFC/RFC-016-legends-myth-formation.md):
        // subscribed at introduction time, continuing the practice
        // reinforced Week 12 Day 61.
        _eventBus.Subscribe<LegendFormedEvent>(OnLegendFormed);
    }

    private void OnCitizenBorn(CitizenBornEvent e)
    {
        // Previously ReproductionSystem published CitizenBornEvent but
        // nothing subscribed to it - Births Recorded stayed at 0 in History
        // even while population was genuinely growing.
        Archive(HistoryCategories.Birth, "CitizenBorn", $"{e.CitizenName} Is Born",
            $"{e.CitizenName} was born at ({e.TileX}, {e.TileY}).",
            string.Empty, e.TileX, e.TileY, e.Tick,
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
            string.Empty, 0, 0, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 1.5);
    }

    private void OnCitizenSpawned(CitizenSpawnedEvent e)
    {
        Archive(HistoryCategories.Birth, "CitizenSpawned", $"New Citizen Appears",
            $"{e.CitizenName} arrived in the world at ({e.TileX}, {e.TileY}).",
            string.Empty, e.TileX, e.TileY, e.Tick,
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
            string.Empty, 0, 0, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 5.0);
    }

    private void OnSettlementFounded(SettlementFoundedEvent e)
    {
        Archive(HistoryCategories.Settlement, "SettlementFounded",
            $"Foundation of {e.SettlementName}",
            $"{e.FounderName} founded the settlement of {e.SettlementName}.",
            e.SettlementName, e.TileX, e.TileY, e.Tick,
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
            e.SettlementName, 0, 0, e.Tick,
            [], [], 4.0, e.SettlementId.Value.ToString());
    }

    private void OnBuildingPlanned(BuildingPlannedEvent e)
    {
        Archive(HistoryCategories.Building, "BuildingPlanned",
            $"{e.BuildingType} Planned",
            $"Construction of a {e.BuildingType} was planned in {e.SettlementName}.",
            e.SettlementName, 0, 0, e.Tick,
            [e.BuildingId.Value.ToString()], [], 5.0, e.SettlementId.Value.ToString());
    }

    private void OnTechnologicalDivergence(TechnologicalDivergenceEvent e)
    {
        Archive(HistoryCategories.Discovery, "TechnologicalDivergence",
            $"{e.SettlementAName} and {e.SettlementBName} Diverge Technologically",
            $"{e.SettlementAName} and {e.SettlementBName} have followed different paths of discovery, now differing by {e.DivergentTechnologyCount} technologies.",
            e.SettlementAName, 0, 0, e.Tick,
            [e.SettlementAId.Value.ToString(), e.SettlementBId.Value.ToString()],
            [e.SettlementAName, e.SettlementBName], 5.0);
    }

    private void OnLegendFormed(LegendFormedEvent e)
    {
        Archive(HistoryCategories.Culture, "LegendFormed", e.Title,
            $"{e.Title} has begun to take root in memory, distinct from the historical record it grew from.",
            string.Empty, 0, 0, e.Tick,
            [e.SourceRecordId.Value.ToString()], [], 5.0);
    }

    private void OnFarmHarvested(FarmHarvestedEvent e)
    {
        // DEVELOPMENT_PLAN.md Week 4 Day 18: severity used to be a flat 2.0
        // for every harvest regardless of size, while "FarmHarvested" was
        // separately whitelisted as always-High in SignificanceEvaluator -
        // together those meant every single harvest was archived as "High"
        // importance, contradicting TG-STRY-050's Core Principle that
        // importance comes from actual consequence, not event type. Yield is
        // the one real signal available for "Depth of consequence" here (a
        // much bigger harvest feeds the settlement far longer) - 20 units
        // was chosen as the divisor because typical observed harvests in
        // this codebase's simulations run roughly 30-100 units (see
        // AgricultureSystem), putting most routine harvests below the
        // Medium threshold (severity > 4.0, i.e. Yield > 80) and only
        // genuinely large ones above it.
        var severity = Math.Min(10.0, e.Yield / 20.0);

        Archive(HistoryCategories.Harvest, "FarmHarvested",
            $"Harvest at {e.SettlementName}",
            $"{e.CropType} harvested: {e.Yield} units.",
            e.SettlementName, 0, 0, e.Tick,
            [], [], severity, e.SettlementId.Value.ToString());
    }

    private void OnTradeCompleted(TradeCompletedEvent e)
    {
        Archive(HistoryCategories.Trade, "TradeCompleted", "Trade Completed",
            $"Trade of {e.Quantity} {e.ItemType} completed.",
            string.Empty, 0, 0, e.Tick,
            [e.FromCitizenId.Value.ToString(), e.ToCitizenId.Value.ToString()],
            [], 3.0);
    }

    private void OnSettlementExpanded(SettlementExpandedEvent e)
    {
        Archive(HistoryCategories.Settlement, "SettlementExpanded",
            $"{e.SettlementName} Expands",
            $"{e.SettlementName} expanded to territory size {e.NewTerritorySize}.",
            e.SettlementName, 0, 0, e.Tick,
            [], [], 6.0, e.SettlementId.Value.ToString());
    }

    private void OnGoodsCrafted(GoodsCraftedEvent e)
    {
        Archive(HistoryCategories.Trade, "GoodsCrafted", "Goods Produced",
            $"{e.Quantity} {e.Product} crafted.",
            string.Empty, 0, 0, e.Tick, [], [], 1.0);
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
            e.SettlementName, 0, 0, e.Tick,
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
            e.SettlementName, 0, 0, e.Tick,
            [], [], 6.5, e.SettlementId.Value.ToString());
    }

    private void OnKingdomFounded(KingdomFoundedEvent e)
    {
        Archive(HistoryCategories.Politics, "KingdomFounded",
            $"The Kingdom of {e.KingdomName} Is Founded",
            $"{e.LeaderName} founded the Kingdom of {e.KingdomName}, with {e.CapitalName} as its capital ({e.MemberCount} settlements).",
            e.CapitalName, 0, 0, e.Tick,
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
            string.Empty, 0, 0, e.Tick,
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
            string.Empty, 0, 0, e.Tick, [], [], severity);
    }

    private void OnTradeRouteEstablished(TradeRouteEstablishedEvent e)
    {
        Archive(HistoryCategories.Trade, "TradeRouteEstablished",
            $"Trade Route Opens Between {e.FromSettlementName} and {e.ToSettlementName}",
            $"A trade route in {e.PrimaryGood} was established between {e.FromSettlementName} and {e.ToSettlementName}.",
            e.FromSettlementName, 0, 0, e.Tick,
            [], [], 5.5, e.FromSettlementId.Value.ToString());
    }

    private void OnTradeRouteAbandoned(TradeRouteAbandonedEvent e)
    {
        // Routine economic contraction - passes through evaluation but is not
        // expected to clear the archival threshold, same treatment as TradeCompleted.
        Archive(HistoryCategories.Trade, "TradeRouteAbandoned",
            $"Trade Route Between {e.FromSettlementName} and {e.ToSettlementName} Ends",
            $"The trade route between {e.FromSettlementName} and {e.ToSettlementName} was abandoned: {e.Reason}.",
            e.FromSettlementName, 0, 0, e.Tick, [], [], 3.0);
    }

    private void OnMigrationStarted(MigrationStartedEvent e)
    {
        // Individual migration is frequent and low-stakes at the per-citizen level
        // (TG-008: not every event is History) - it still flows through the same
        // evaluation pipeline, it just doesn't clear the archival bar by itself.
        Archive(HistoryCategories.Migration, "MigrationStarted",
            $"{e.CitizenName} Leaves {e.FromSettlementName}",
            $"{e.CitizenName} departed {e.FromSettlementName}: {e.Reason}.",
            e.FromSettlementName, e.FromX, e.FromY, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 1.5);
    }

    private void OnMigrationCompleted(MigrationCompletedEvent e)
    {
        Archive(HistoryCategories.Migration, "MigrationCompleted",
            $"{e.CitizenName} Arrives at {e.ToSettlementName}",
            $"{e.CitizenName} settled in {e.ToSettlementName}.",
            e.ToSettlementName, e.ToX, e.ToY, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 1.5);
    }

    private void OnTechnologyDiscovered(TechnologyDiscoveredEvent e)
    {
        Archive(HistoryCategories.Technology, "TechnologyDiscovered",
            $"{e.TechnologyName} Is Discovered",
            $"{e.SettlementName} discovered {e.TechnologyName} ({e.Category}).",
            e.SettlementName, 0, 0, e.Tick, [], [], 7.5,
            e.DiscoveredBySettlementId?.Value.ToString() ?? "");
    }

    private void OnReligionEstablished(ReligionEstablishedEvent e)
    {
        Archive(HistoryCategories.Religion, "ReligionEstablished",
            $"{e.ReligionName} Is Established",
            $"{e.ReligionName}, centered on {e.CoreValue}, was established in {e.OriginSettlementName} with {e.InitialFollowers} followers.",
            e.OriginSettlementName, 0, 0, e.Tick, [], [], 8.5);

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
            e.SettlementName, 0, 0, e.Tick, [], [], 4.5, e.SettlementId.Value.ToString());
    }

    private void OnDialectFormed(DialectFormedEvent e)
    {
        // Reuses HistoryCategories.Culture - no dedicated Language category
        // exists, and CulturalFestivalHeld (also a Volume VI Social event)
        // already sets that precedent.
        Archive(HistoryCategories.Culture, "DialectFormed",
            $"A Dialect Forms Between {e.SettlementAName} and {e.SettlementBName}",
            $"{e.SettlementAName} and {e.SettlementBName} have drifted apart linguistically (divergence {e.Divergence:F0}/100) - a distinct dialect has formed.",
            e.SettlementAName, 0, 0, e.Tick,
            [e.SettlementAId.Value.ToString(), e.SettlementBId.Value.ToString()],
            [e.SettlementAName, e.SettlementBName], 5.0, e.SettlementAId.Value.ToString());
    }

    private void OnForestExpanded(ForestExpandedEvent e)
    {
        // EcologySystem now publishes one aggregated event per weekly tick
        // instead of one per tile - that alone is what stops this from
        // flooding the archive (previously 65% of all sampled history
        // records were ForestExpanded). Severity stays a flat Medium
        // (5.0), matching every other single-occurrence-per-tick event,
        // rather than being scaled by area - a lone tile changing in a
        // quiet week is exactly as archive-worthy as it always was.
        Archive(HistoryCategories.Nature, "ForestExpanded",
            $"Forest Cover Grows Near ({e.TileX}, {e.TileY})",
            $"Forest cover expanded by {e.AreaExpanded} tile(s) this week, most recently near ({e.TileX}, {e.TileY}).",
            string.Empty, e.TileX, e.TileY, e.Tick, [], [], 5.0);
    }

    private void OnForestDeclined(ForestDeclinedEvent e)
    {
        Archive(HistoryCategories.Nature, "ForestDeclined",
            $"Forest Recedes Near ({e.TileX}, {e.TileY})",
            $"Drought caused forest cover to decline by {e.AreaLost} tile(s) this week, most recently near ({e.TileX}, {e.TileY}).",
            string.Empty, e.TileX, e.TileY, e.Tick, [], [], 5.0);
    }

    private void OnBorderContracted(BorderContractedEvent e)
    {
        Archive(HistoryCategories.Settlement, "BorderContracted",
            $"{e.SettlementName}'s Territory Contracts",
            $"{e.SettlementName}'s territory shrank to radius {e.NewTerritorySize}.",
            e.SettlementName, 0, 0, e.Tick, [], [], 5.0, e.SettlementId.Value.ToString());
    }

    private void OnBorderDisputeBegins(BorderDisputeBeginsEvent e)
    {
        Archive(HistoryCategories.Diplomacy, "BorderDisputeBegins",
            $"Border Dispute Between {e.SettlementAName} and {e.SettlementBName}",
            $"{e.SettlementAName} and {e.SettlementBName} now claim overlapping territory of comparable strength.",
            e.SettlementAName, 0, 0, e.Tick,
            [e.SettlementAId.Value.ToString(), e.SettlementBId.Value.ToString()],
            [e.SettlementAName, e.SettlementBName], 6.0, e.SettlementAId.Value.ToString());
    }

    private void OnSeasonChanged(SeasonChangedEvent e)
    {
        Archive(HistoryCategories.Nature, "SeasonChanged",
            $"{e.NewSeason} Begins",
            $"The season turned from {e.PreviousSeason} to {e.NewSeason}.",
            string.Empty, 0, 0, e.Tick, [], [], 5.0);
    }

    private void OnPopulationDecline(PopulationDeclineEvent e)
    {
        Archive(HistoryCategories.Settlement, "PopulationDecline",
            $"{e.SettlementName} Outgrows Its Means",
            $"{e.SettlementName}'s population ({e.Population}) has exceeded what its food and housing can sustain ({e.CarryingCapacity:F1}).",
            e.SettlementName, 0, 0, e.Tick, [], [], 5.0, e.SettlementId.Value.ToString());
    }

    private void OnPopulationBoom(PopulationBoomEvent e)
    {
        Archive(HistoryCategories.Settlement, "PopulationBoom",
            $"{e.SettlementName} Flourishes",
            $"{e.SettlementName}'s population ({e.Population}) is growing comfortably within its means ({e.CarryingCapacity:F1}).",
            e.SettlementName, 0, 0, e.Tick, [], [], 5.0, e.SettlementId.Value.ToString());
    }

    private void OnOrganismInfected(OrganismInfectedEvent e)
    {
        Archive(HistoryCategories.Death, "OrganismInfected",
            $"{e.CitizenName} Falls Ill",
            $"{e.CitizenName} contracted an infection amid overcrowding in {e.SettlementName}.",
            e.SettlementName, 0, 0, e.Tick,
            [e.CitizenId.Value.ToString()], [e.CitizenName], 5.0, e.SettlementId.Value.ToString());
    }

    private void OnDiseaseRecovered(DiseaseRecoveredEvent e)
    {
        Archive(HistoryCategories.Death, "DiseaseRecovered",
            $"{e.CitizenName} Recovers",
            $"{e.CitizenName} recovered from illness.",
            string.Empty, 0, 0, e.Tick, [e.CitizenId.Value.ToString()], [e.CitizenName], 5.0);
    }

    private void OnEpidemicStarted(EpidemicStartedEvent e)
    {
        Archive(HistoryCategories.Disaster, "EpidemicStarted",
            $"Epidemic Strikes {e.SettlementName}",
            $"An epidemic has taken hold in {e.SettlementName}, with {e.InfectionRate:P0} of its population infected.",
            e.SettlementName, 0, 0, e.Tick, [], [], 7.0, e.SettlementId.Value.ToString());
    }

    private void OnEpidemicContained(EpidemicContainedEvent e)
    {
        Archive(HistoryCategories.Disaster, "EpidemicContained",
            $"{e.SettlementName}'s Epidemic Subsides",
            $"The epidemic in {e.SettlementName} has been contained.",
            e.SettlementName, 0, 0, e.Tick, [], [], 5.0, e.SettlementId.Value.ToString());
    }

    private void OnAdaptiveShiftObserved(AdaptiveShiftObservedEvent e)
    {
        var direction = e.Delta > 0 ? "risen" : "fallen";
        Archive(HistoryCategories.Nature, "AdaptiveShiftObserved",
            $"{e.SettlementName}'s Population Adapts",
            $"{e.SettlementName}'s average {e.AttributeName} has {direction} by {Math.Abs(e.Delta):F1} over the past year.",
            e.SettlementName, 0, 0, e.Tick, [], [], 4.5, e.SettlementId.Value.ToString());
    }

    private void OnEvolutionaryStagnation(EvolutionaryStagnationEvent e)
    {
        Archive(HistoryCategories.Nature, "EvolutionaryStagnation",
            $"{e.SettlementName} Reaches Equilibrium",
            $"{e.SettlementName}'s population has shown no meaningful attribute shift for several years.",
            e.SettlementName, 0, 0, e.Tick, [], [], 4.5, e.SettlementId.Value.ToString());
    }

    private void OnNutrientPulseOccurred(NutrientPulseOccurredEvent e)
    {
        Archive(HistoryCategories.Nature, "NutrientPulseOccurred",
            $"{e.SettlementName}'s Soil Rejuvenates",
            $"{e.SettlementName}'s soil health has risen to {e.SoilHealth:F0} as organic matter decomposes.",
            e.SettlementName, 0, 0, e.Tick, [], [], 4.5, e.SettlementId.Value.ToString());
    }

    private void OnOrganicMatterAccumulated(OrganicMatterAccumulatedEvent e)
    {
        Archive(HistoryCategories.Nature, "OrganicMatterAccumulated",
            $"Waste Piles Up in {e.SettlementName}",
            $"Organic matter is accumulating in {e.SettlementName} faster than it can decompose.",
            e.SettlementName, 0, 0, e.Tick, [], [], 4.5, e.SettlementId.Value.ToString());
    }

    private void OnWasteFullyDecomposed(WasteFullyDecomposedEvent e)
    {
        Archive(HistoryCategories.Nature, "WasteFullyDecomposed",
            $"{e.SettlementName}'s Waste Clears",
            $"The accumulated organic matter in {e.SettlementName} has fully decomposed.",
            e.SettlementName, 0, 0, e.Tick, [], [], 4.5, e.SettlementId.Value.ToString());
    }

    private void OnSpeciesExpanded(SpeciesExpandedEvent e)
    {
        Archive(HistoryCategories.Nature, "SpeciesExpanded",
            $"Wildlife Flourishes Near {e.SettlementName}",
            $"The wildlife population near {e.SettlementName} has grown to {e.WildlifePopulation:F0}.",
            e.SettlementName, 0, 0, e.Tick, [], [], 4.5, e.SettlementId.Value.ToString());
    }

    private void OnAnimalDied(AnimalDiedEvent e)
    {
        Archive(HistoryCategories.Nature, "AnimalDied",
            $"Wildlife Declines Near {e.SettlementName}",
            $"The wildlife population near {e.SettlementName} has fallen to {e.WildlifePopulation:F0}.",
            e.SettlementName, 0, 0, e.Tick, [], [], 4.5, e.SettlementId.Value.ToString());
    }

    private void OnApprenticeshipStarted(ApprenticeshipStartedEvent e)
    {
        Archive(HistoryCategories.Discovery, "ApprenticeshipStarted",
            $"{e.StudentName} Begins Learning from {e.MentorName}",
            $"{e.MentorName} took on {e.StudentName} as an apprentice.",
            string.Empty, 0, 0, e.Tick,
            [e.MentorId.Value.ToString(), e.StudentId.Value.ToString()],
            [e.MentorName, e.StudentName], 4.5);
    }

    private void OnApprenticeshipCompleted(ApprenticeshipCompletedEvent e)
    {
        Archive(HistoryCategories.Discovery, "ApprenticeshipCompleted",
            $"{e.StudentName} Completes Their Apprenticeship",
            $"{e.StudentName}'s apprenticeship under {e.MentorName} has come to an end.",
            string.Empty, 0, 0, e.Tick,
            [e.MentorId.Value.ToString(), e.StudentId.Value.ToString()],
            [e.MentorName, e.StudentName], 4.5);
    }

    private void OnCaseResolved(CaseResolvedEvent e)
    {
        Archive(HistoryCategories.Politics, "CaseResolved",
            $"Dispute Resolved in {e.SettlementName}",
            $"{e.SettlementName}'s leader resolved the dispute between {e.CitizenAName} and {e.CitizenBName}.",
            e.SettlementName, 0, 0, e.Tick,
            [e.CitizenAId.Value.ToString(), e.CitizenBId.Value.ToString()],
            [e.CitizenAName, e.CitizenBName], 4.5, e.SettlementId.Value.ToString());
    }

    private void OnJusticeFailure(JusticeFailureEvent e)
    {
        Archive(HistoryCategories.Politics, "JusticeFailure",
            $"Justice Fails in {e.SettlementName}",
            $"{e.SettlementName}'s leader could not resolve the dispute between {e.CitizenAName} and {e.CitizenBName}.",
            e.SettlementName, 0, 0, e.Tick,
            [e.CitizenAId.Value.ToString(), e.CitizenBId.Value.ToString()],
            [e.CitizenAName, e.CitizenBName], 5.0, e.SettlementId.Value.ToString());
    }

    private void OnWarDeclared(WarDeclaredEvent e)
    {
        Archive(HistoryCategories.War, "WarDeclared",
            $"War Breaks Out Between {e.SettlementAName} and {e.SettlementBName}",
            $"Years of unresolved dispute and failed trust have escalated into open war between {e.SettlementAName} and {e.SettlementBName}.",
            e.SettlementAName, 0, 0, e.Tick,
            [e.SettlementAId.Value.ToString(), e.SettlementBId.Value.ToString()],
            [e.SettlementAName, e.SettlementBName], 8.0, e.SettlementAId.Value.ToString());
    }

    private void OnBattleFought(BattleFoughtEvent e)
    {
        Archive(HistoryCategories.War, "BattleFought",
            $"{e.WinnerName} Prevails Over {e.LoserName}",
            $"{e.WinnerName} won a battle against {e.LoserName}.",
            e.WinnerName, 0, 0, e.Tick,
            [e.WinnerId.Value.ToString(), e.LoserId.Value.ToString()],
            [e.WinnerName, e.LoserName], 6.0, e.WinnerId.Value.ToString());
    }

    private void OnPeaceNegotiated(PeaceNegotiatedEvent e)
    {
        Archive(HistoryCategories.War, "PeaceNegotiated",
            $"Peace Between {e.SettlementAName} and {e.SettlementBName}",
            $"{e.SettlementAName} and {e.SettlementBName} have negotiated an end to their war.",
            e.SettlementAName, 0, 0, e.Tick,
            [e.SettlementAId.Value.ToString(), e.SettlementBId.Value.ToString()],
            [e.SettlementAName, e.SettlementBName], 7.0, e.SettlementAId.Value.ToString());
    }

    private void OnRoadConstructed(RoadConstructedEvent e)
    {
        Archive(HistoryCategories.Trade, "RoadConstructed",
            $"A Road Forms Between {e.FromSettlementName} and {e.ToSettlementName}",
            $"Sustained trade between {e.FromSettlementName} and {e.ToSettlementName} has built up a real road along their route.",
            e.FromSettlementName, 0, 0, e.Tick,
            [e.FromSettlementId.Value.ToString(), e.ToSettlementId.Value.ToString()],
            [e.FromSettlementName, e.ToSettlementName], 5.0);
    }

    private void OnInfrastructureFailure(InfrastructureFailureEvent e)
    {
        Archive(HistoryCategories.Trade, "InfrastructureFailure",
            $"The Road Between {e.FromSettlementName} and {e.ToSettlementName} Falls Into Disrepair",
            $"Neglect has reduced the route between {e.FromSettlementName} and {e.ToSettlementName} back to a mere footpath.",
            e.FromSettlementName, 0, 0, e.Tick,
            [e.FromSettlementId.Value.ToString(), e.ToSettlementId.Value.ToString()],
            [e.FromSettlementName, e.ToSettlementName], 5.0);
    }

    private void Archive(string category, string eventType, string title, string description,
        string locationName, int locationX, int locationY, long tick,
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
            LocationX = locationX != 0 ? locationX : _worldState.Settlements.FirstOrDefault()?.TileX ?? 0,
            LocationY = locationY != 0 ? locationY : _worldState.Settlements.FirstOrDefault()?.TileY ?? 0,
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
