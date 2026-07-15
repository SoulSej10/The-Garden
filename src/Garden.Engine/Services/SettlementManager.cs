using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class SettlementManager
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SettlementManager> _logger;

    public SettlementManager(WorldState worldState, IEventBus eventBus, ILogger<SettlementManager> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public Settlement? FindSettlementAt(int x, int y)
    {
        return _worldState.Settlements.FirstOrDefault(s => s.IsWithinTerritory(x, y));
    }

    public Settlement? FindNearbySettlement(int x, int y, int range = 10)
    {
        return _worldState.Settlements
            .Where(s => Math.Abs(s.TileX - x) + Math.Abs(s.TileY - y) <= range)
            .OrderBy(s => Math.Abs(s.TileX - x) + Math.Abs(s.TileY - y))
            .FirstOrDefault();
    }

    /// <summary>
    /// Nearest settlement anywhere in the world, regardless of distance.
    /// Unsettled citizens should always prefer travelling to an existing
    /// settlement over founding a redundant new one - a small join radius
    /// was letting most citizens on a large map never discover any existing
    /// settlement before independently founding their own, fragmenting a
    /// population of 50 into dozens of one- or two-person camps too small
    /// to sustain themselves.
    /// </summary>
    public Settlement? FindNearestSettlement(int x, int y)
    {
        return _worldState.Settlements
            .OrderBy(s => Math.Abs(s.TileX - x) + Math.Abs(s.TileY - y))
            .FirstOrDefault();
    }

    public Settlement FoundSettlement(Citizen founder, string name, int x, int y, long tick)
    {
        var settlement = new Settlement
        {
            Name = name,
            TileX = x,
            TileY = y,
            FoundedTick = tick,
            Population = 1
        };
        settlement.MemberIds.Add(founder.Id);

        var shelter = new Building
        {
            BuildingType = BuildingTypes.Shelter,
            TileX = x,
            TileY = y,
            Status = BuildingStatus.Completed,
            BuildProgress = 100
        };
        settlement.Buildings.Add(shelter);

        // Audit finding 3a: a settlement founded with zero food is
        // food-insecure from tick zero, before a single Farm exists or has
        // had time to be planted and harvested - a small buffer buys the
        // days needed for Storage/Farm construction and the first planting
        // cycle to happen before hunger forces every citizen into pure
        // survival foraging.
        settlement.Storage.Add("Food", 25);

        _worldState.Settlements.Add(settlement);

        _eventBus.Publish(new SettlementFoundedEvent
        {
            Tick = tick,
            SettlementId = settlement.Id,
            SettlementName = name,
            FounderId = founder.Id,
            FounderName = $"{founder.FirstName} {founder.LastName}",
            TileX = x,
            TileY = y
        });

        _logger.LogInformation("Settlement '{Name}' founded by {Founder} at ({X},{Y})",
            name, $"{founder.FirstName} {founder.LastName}", x, y);

        return settlement;
    }

    public void JoinSettlement(Settlement settlement, Citizen citizen)
    {
        if (!settlement.MemberIds.Contains(citizen.Id))
        {
            settlement.MemberIds.Add(citizen.Id);
            settlement.Population = settlement.MemberIds.Count;
            citizen.HomeSettlementId = settlement.Id;
        }
    }

    public void AddBuilding(Settlement settlement, Building building)
    {
        settlement.Buildings.Add(building);
    }

    public bool HasResourcesFor(Building building, Settlement settlement)
    {
        var costs = BuildingTypes.GetCost(building.BuildingType);
        return costs.All(c => settlement.Storage.GetQuantity(c.Material) >= c.Amount);
    }

    public void DeductResources(Building building, Settlement settlement)
    {
        var costs = BuildingTypes.GetCost(building.BuildingType);
        foreach (var cost in costs)
        {
            settlement.Storage.Remove(cost.Material, cost.Amount);
        }
    }

    public void ExpandTerritory(Settlement settlement)
    {
        settlement.TerritoryRadius++;

        _eventBus.Publish(new SettlementExpandedEvent
        {
            Tick = _worldState.CurrentTime.Tick,
            SettlementId = settlement.Id,
            SettlementName = settlement.Name,
            NewTerritorySize = settlement.TerritoryRadius
        });
    }
}
