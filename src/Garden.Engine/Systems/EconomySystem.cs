using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class EconomySystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly ILogger<EconomySystem> _logger;
    private long _nextExecutionTick;

    public string Name => "EconomySystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    private double _totalGoodsCrafted;
    private int _totalTrades;

    public double TotalGoodsCrafted => _totalGoodsCrafted;
    public int TotalTrades => _totalTrades;

    public EconomySystem(WorldState worldState, ILogger<EconomySystem> logger)
    {
        _worldState = worldState;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var settlement in _worldState.Settlements)
        {
            ProcessConsumption(settlement);
            ProcessProduction(settlement);
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    private void ProcessConsumption(Settlement settlement)
    {
        foreach (var memberId in settlement.MemberIds.ToList())
        {
            var citizen = _worldState.Citizens.FirstOrDefault(c => c.Id == memberId && c.IsAlive);
            if (citizen == null) continue;

            var foodAvailable = settlement.Storage.GetQuantity("Food");
            if (foodAvailable >= 1)
            {
                settlement.Storage.Remove("Food", 1);
                citizen.Needs.Hunger = System.Math.Max(0, citizen.Needs.Hunger - 15);
                citizen.Needs.Energy = System.Math.Min(100, citizen.Needs.Energy + 5);
            }

            if (foodAvailable >= 0.5)
            {
                settlement.Storage.Remove("Water", 0.5);
                citizen.Needs.Thirst = System.Math.Max(0, citizen.Needs.Thirst - 20);
            }
        }
    }

    private void ProcessProduction(Settlement settlement)
    {
        var workshops = settlement.Buildings
            .Where(b => b.BuildingType == "Workshop" && b.Status == World.Entities.BuildingStatus.Completed)
            .ToList();

        foreach (var workshop in workshops)
        {
            var woodAvailable = settlement.Storage.GetQuantity("Wood");
            if (woodAvailable >= 5)
            {
                settlement.Storage.Remove("Wood", 5);
                settlement.Storage.Add("Planks", 3);
                _totalGoodsCrafted += 3;

                _logger.LogDebug("Workshop in {Settlement} produced planks", settlement.Name);
            }
        }
    }

    public void RecordTrade()
    {
        _totalTrades++;
    }
}
