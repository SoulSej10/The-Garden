using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class TechnologyService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<TechnologyService> _logger;
    private bool _initialized;

    public TechnologyService(WorldState worldState, IEventBus eventBus, ILogger<TechnologyService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void InitializeTechnologies()
    {
        if (_initialized) return;
        _worldState.Technologies.Clear();
        foreach (var tech in Technology.AllTechnologies)
        {
            _worldState.Technologies.Add(new Technology
            {
                Name = tech.Name,
                Category = tech.Category,
                Description = tech.Description,
                ProgressRequired = tech.ProgressRequired
            });
        }
        _initialized = true;
    }

    public void EvaluateTechnology(long tick)
    {
        if (_worldState.Technologies.Count == 0) InitializeTechnologies();

        foreach (var settlement in _worldState.Settlements.Where(s => s.MemberIds.Count >= 2))
        {
            var progress = CalculateProgress(settlement);
            settlement.TechnologyProgress += progress;

            var undiscoved = _worldState.Technologies
                .Where(t => !t.IsDiscovered)
                .ToList();

            foreach (var tech in undiscoved)
            {
                var contribution = progress * 0.1;
                if (tech.Category == "Agriculture" && settlement.Buildings.Any(b => b.BuildingType == "Farm" && b.Status == BuildingStatus.Completed))
                    contribution *= 1.5;
                if (tech.Category == "Construction" && settlement.CompletedBuildings > 0)
                    contribution *= 1.3;

                tech.CurrentProgress += contribution;

                if (tech.CurrentProgress >= tech.ProgressRequired && !tech.IsDiscovered)
                {
                    tech.IsDiscovered = true;
                    tech.DiscoveredTick = tick;
                    tech.DiscoveredBySettlementId = settlement.Id;
                    tech.DiscoveredBySettlementName = settlement.Name;

                    var smartest = _worldState.Citizens
                        .Where(c => settlement.MemberIds.Contains(c.Id) && c.IsAlive)
                        .OrderByDescending(c => c.Attributes.Intelligence)
                        .FirstOrDefault();
                    if (smartest != null)
                    {
                        tech.DiscoveredByCitizenId = smartest.Id;
                        tech.DiscoveredByCitizenName = $"{smartest.FirstName} {smartest.LastName}";
                    }

                    _eventBus.Publish(new TechnologyDiscoveredEvent
                    {
                        Tick = tick,
                        TechnologyId = tech.Id.Value.ToString(),
                        TechnologyName = tech.Name,
                        Category = tech.Category,
                        DiscoveredBySettlementId = settlement.Id,
                        SettlementName = settlement.Name
                    });

                    _logger.LogInformation("Technology '{Tech}' discovered by {Settlement}",
                        tech.Name, settlement.Name);
                }
            }
        }
    }

    private static double CalculateProgress(Settlement settlement)
    {
        var baseProgress = settlement.MemberIds.Count * 0.01;
        var intelligenceFactor = 0.0;

        var members = settlement.MemberIds.Count;
        baseProgress += members * 0.02;

        return Math.Max(0.01, baseProgress + intelligenceFactor);
    }

    public IReadOnlyList<Technology> GetDiscoveredTechnologies() =>
        _worldState.Technologies.Where(t => t.IsDiscovered).ToList();

    public IReadOnlyList<Technology> GetUndiscoveredTechnologies() =>
        _worldState.Technologies.Where(t => !t.IsDiscovered).ToList();
}
