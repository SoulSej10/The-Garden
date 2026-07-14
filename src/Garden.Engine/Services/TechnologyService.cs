using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

/// <summary>
/// RFC-015 (specification/RFC/RFC-015-technology-independent-discovery.md):
/// each settlement now tracks its own SettlementTechnology row per named
/// technology instead of every settlement sharing one Technology.CurrentProgress/
/// IsDiscovered pair - the old shared state made Independent Discovery,
/// Parallel Inventions, and Technological Divergence (TG-670's own Edge
/// Cases) structurally impossible, not just unmodeled (ADR-004).
/// </summary>
public class TechnologyService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<TechnologyService> _logger;

    // RFC-015: two settlements' discovered-technology sets must differ by at
    // least this many technologies before TechnologicalDivergenceEvent fires
    // (invented - TG-670 gives no threshold), avoiding a flood of events for
    // trivial one-tech differences that occur constantly under independent
    // per-settlement discovery.
    private const int DivergenceThreshold = 3;

    private readonly HashSet<(Guid, Guid)> _divergenceReported = [];

    public TechnologyService(WorldState worldState, IEventBus eventBus, ILogger<TechnologyService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void EvaluateTechnology(long tick)
    {
        var settlements = _worldState.Settlements.Where(s => s.MemberIds.Count >= 2).ToList();

        foreach (var settlement in settlements)
        {
            var members = settlement.MemberIds
                .Select(id => _worldState.Citizens.FirstOrDefault(c => c.Id == id))
                .Where(c => c != null && c.IsAlive)
                .Select(c => c!)
                .ToList();

            var progress = CalculateProgress(settlement, members);
            settlement.TechnologyProgress += progress;

            foreach (var catalogTech in Technology.AllTechnologies.Where(t => !GetOrCreate(settlement.Id, t.Name).IsDiscovered))
            {
                var row = GetOrCreate(settlement.Id, catalogTech.Name);

                var contribution = progress;
                if (catalogTech.Category == "Agriculture" && settlement.Buildings.Any(b => b.BuildingType == "Farm" && b.Status == BuildingStatus.Completed))
                    contribution *= 1.5;
                if (catalogTech.Category == "Construction" && settlement.CompletedBuildings > 0)
                    contribution *= 1.3;

                row.CurrentProgress += contribution;

                if (row.CurrentProgress >= catalogTech.ProgressRequired && !row.IsDiscovered)
                {
                    row.IsDiscovered = true;
                    row.DiscoveredTick = tick;

                    var smartest = members.OrderByDescending(c => c.Attributes.Intelligence).FirstOrDefault();
                    if (smartest != null)
                    {
                        row.DiscoveredByCitizenId = smartest.Id;
                        row.DiscoveredByCitizenName = $"{smartest.FirstName} {smartest.LastName}";
                    }

                    _eventBus.Publish(new TechnologyDiscoveredEvent
                    {
                        Tick = tick,
                        TechnologyId = row.Id.Value.ToString(),
                        TechnologyName = catalogTech.Name,
                        Category = catalogTech.Category,
                        DiscoveredBySettlementId = settlement.Id,
                        SettlementName = settlement.Name,
                        DiscoveredByCitizenId = row.DiscoveredByCitizenId
                    });

                    _logger.LogInformation("Technology '{Tech}' discovered by {Settlement}",
                        catalogTech.Name, settlement.Name);
                }
            }
        }

        DetectDivergence(tick, settlements);
    }

    // RFC-015: a settlement's technology contribution is scaled by its
    // living members' average Intelligence - implementing TG-670's
    // explicitly-named Simulation Rule "Education increases research
    // capacity" for the first time, reusing the same average-Attribute
    // aggregate EvolutionSystem (Week 16) already computes. 5.0 is
    // Citizen.Intelligence's generation-time baseline (Citizen.cs), so an
    // average settlement sees no change from this factor.
    private static double CalculateProgress(Settlement settlement, List<Citizen> members)
    {
        var baseProgress = settlement.MemberIds.Count * 0.03;

        var intelligenceFactor = members.Count > 0
            ? Math.Max(0.0, 1.0 + (members.Average(c => c.Attributes.Intelligence) - 5.0) / 20.0)
            : 1.0;

        return Math.Max(0.01, baseProgress) * intelligenceFactor;
    }

    private SettlementTechnology GetOrCreate(Garden.Core.Identifiers.GameEntityId settlementId, string technologyName)
    {
        var existing = _worldState.SettlementTechnologies
            .FirstOrDefault(t => t.SettlementId == settlementId && t.TechnologyName == technologyName);
        if (existing != null) return existing;

        var created = new SettlementTechnology { SettlementId = settlementId, TechnologyName = technologyName };
        _worldState.SettlementTechnologies.Add(created);
        return created;
    }

    // RFC-015: TechnologicalDivergenceEvent - only possible to observe now
    // that discovery is per-settlement. Fires once per pair, when their
    // discovered-technology sets first differ by DivergenceThreshold or
    // more, the same "state-transition, not repeated level-crossing" shape
    // every prior RFC's event-firing logic uses.
    private void DetectDivergence(long tick, List<Settlement> settlements)
    {
        for (var i = 0; i < settlements.Count; i++)
        {
            for (var j = i + 1; j < settlements.Count; j++)
            {
                var a = settlements[i];
                var b = settlements[j];
                var key = OrderedPair(a.Id.Value, b.Id.Value);
                if (_divergenceReported.Contains(key)) continue;

                var aDiscovered = DiscoveredNames(a.Id);
                var bDiscovered = DiscoveredNames(b.Id);
                var diffCount = aDiscovered.Except(bDiscovered).Count() + bDiscovered.Except(aDiscovered).Count();

                if (diffCount < DivergenceThreshold) continue;

                _divergenceReported.Add(key);
                _eventBus.Publish(new TechnologicalDivergenceEvent
                {
                    Tick = tick,
                    SettlementAId = a.Id,
                    SettlementAName = a.Name,
                    SettlementBId = b.Id,
                    SettlementBName = b.Name,
                    DivergentTechnologyCount = diffCount
                });
            }
        }
    }

    private static (Guid, Guid) OrderedPair(Guid a, Guid b) =>
        a.CompareTo(b) <= 0 ? (a, b) : (b, a);

    private HashSet<string> DiscoveredNames(Garden.Core.Identifiers.GameEntityId settlementId) =>
        _worldState.SettlementTechnologies
            .Where(t => t.SettlementId == settlementId && t.IsDiscovered)
            .Select(t => t.TechnologyName)
            .ToHashSet();

    public IReadOnlyList<TechnologyView> GetDiscoveredTechnologies(Garden.Core.Identifiers.GameEntityId? settlementId = null)
    {
        var catalog = Technology.AllTechnologies.ToDictionary(t => t.Name);
        return _worldState.SettlementTechnologies
            .Where(t => t.IsDiscovered && (settlementId == null || t.SettlementId == settlementId) && catalog.ContainsKey(t.TechnologyName))
            .Select(t => ToView(t, catalog[t.TechnologyName]))
            .ToList();
    }

    public IReadOnlyList<TechnologyView> GetUndiscoveredTechnologies(Garden.Core.Identifiers.GameEntityId settlementId) =>
        Technology.AllTechnologies
            .Select(t => (row: GetOrCreate(settlementId, t.Name), catalog: t))
            .Where(x => !x.row.IsDiscovered)
            .Select(x => ToView(x.row, x.catalog))
            .ToList();

    // RFC-015: aggregate "in progress" view for the world-wide Civilization
    // dashboard (no single settlement selected) - the furthest-along
    // settlement's progress per technology, across whichever settlements
    // have actually started contributing to it (doesn't force-create rows
    // for settlements that haven't touched a technology at all).
    public IReadOnlyList<TechnologyView> GetUndiscoveredTechnologiesAggregate()
    {
        var catalog = Technology.AllTechnologies.ToDictionary(t => t.Name);
        return _worldState.SettlementTechnologies
            .Where(t => !t.IsDiscovered && catalog.ContainsKey(t.TechnologyName))
            .GroupBy(t => t.TechnologyName)
            .Select(g => g.OrderByDescending(t => t.CurrentProgress).First())
            .Select(t => ToView(t, catalog[t.TechnologyName]))
            .ToList();
    }

    private TechnologyView ToView(SettlementTechnology row, Technology catalog)
    {
        var settlementName = _worldState.Settlements.FirstOrDefault(s => s.Id == row.SettlementId)?.Name ?? "Unknown";
        return new(
            row.Id.Value.ToString(), catalog.Name, catalog.Category, catalog.Description,
            row.SettlementId.Value.ToString(), settlementName, row.CurrentProgress, catalog.ProgressRequired,
            row.IsDiscovered, row.DiscoveredTick, row.DiscoveredByCitizenName);
    }
}

// RFC-015: joins a live SettlementTechnology row with its static Technology
// catalog entry (name/category/description/progress-required) for callers
// (CivilizationController) that need both.
public record TechnologyView(
    string Id, string Name, string Category, string Description,
    string SettlementId, string SettlementName, double CurrentProgress, double ProgressRequired,
    bool IsDiscovered, long? DiscoveredTick, string DiscoveredByCitizenName);
