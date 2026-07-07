using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class ConstructionSystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly SettlementManager _settlementManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ConstructionSystem> _logger;
    private long _nextExecutionTick;

    public string Name => "ConstructionSystem";
    public long IntervalTicks => 1;
    public long NextExecutionTick => _nextExecutionTick;

    public ConstructionSystem(
        WorldState worldState,
        SettlementManager settlementManager,
        IEventBus eventBus,
        ILogger<ConstructionSystem> logger)
    {
        _worldState = worldState;
        _settlementManager = settlementManager;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var settlement in _worldState.Settlements)
        {
            var inProgress = settlement.Buildings
                .Where(b => b.Status == BuildingStatus.UnderConstruction)
                .ToList();

            foreach (var building in inProgress)
            {
                if (building.AssignedWorkerId != null)
                {
                    var worker = _worldState.Citizens
                        .FirstOrDefault(c => c.Id == building.AssignedWorkerId && c.IsAlive);
                    if (worker == null) continue;

                    building.BuildProgress += 5;
                    worker.CurrentActivity = "Building";
                }
                else
                {
                    building.BuildProgress += 1;
                }

                if (building.BuildProgress >= building.BuildTimeRequired)
                {
                    building.Status = BuildingStatus.Completed;
                    building.BuildProgress = building.BuildTimeRequired;

                    settlement.Storage.Add("Wood", 5);

                    _eventBus.Publish(new BuildingCompletedEvent
                    {
                        Tick = tick,
                        SettlementId = settlement.Id,
                        SettlementName = settlement.Name,
                        BuildingId = building.Id,
                        BuildingType = building.BuildingType
                    });

                    _logger.LogInformation("Building {Type} completed in {Settlement}",
                        building.BuildingType, settlement.Name);
                }
            }
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    public void PlanBuilding(Settlement settlement, string buildingType, int tileX, int tileY)
    {
        var building = new Building
        {
            BuildingType = buildingType,
            TileX = tileX,
            TileY = tileY,
            BuildTimeRequired = BuildingTypes.GetBuildTime(buildingType)
        };

        settlement.Buildings.Add(building);

        _eventBus.Publish(new BuildingPlannedEvent
        {
            Tick = _worldState.CurrentTime.Tick,
            SettlementId = settlement.Id,
            SettlementName = settlement.Name,
            BuildingId = building.Id,
            BuildingType = buildingType
        });
    }

    public void AssignWorker(Building building, Citizen citizen)
    {
        building.Status = BuildingStatus.UnderConstruction;
        building.AssignedWorkerId = citizen.Id;
    }
}
