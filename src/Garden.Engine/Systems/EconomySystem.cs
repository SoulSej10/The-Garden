using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Systems;

public class EconomySystem : IScheduledSystem
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<EconomySystem> _logger;
    private long _nextExecutionTick;

    public string Name => "EconomySystem";
    public long IntervalTicks => 24;
    public long NextExecutionTick => _nextExecutionTick;

    private double _totalGoodsCrafted;
    private int _totalTrades;

    public double TotalGoodsCrafted => _totalGoodsCrafted;
    public int TotalTrades => _totalTrades;

    public EconomySystem(WorldState worldState, IEventBus eventBus, ILogger<EconomySystem> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void Execute()
    {
        var tick = _worldState.CurrentTime.Tick;

        foreach (var settlement in _worldState.Settlements)
        {
            ProcessProduction(settlement, tick);
        }

        _nextExecutionTick = tick + IntervalTicks;
    }

    // Rebalancing audit finding 6: this used to also run ProcessConsumption
    // here, independently removing 1 Food + 0.5 Water per citizen per day
    // directly from Storage - entirely separate from, and unaware of,
    // CitizenSystem.Eat()/Drink(), which drains the exact same Storage
    // whenever a hungry/thirsty citizen actually eats or drinks. Two
    // uncoordinated systems taxing one shared resource meant a settlement's
    // real food burn rate was never "whatever CitizenSystem models" - it
    // was both, stacked, silently doubling consumption. CitizenSystem's
    // need-driven model is the sole consumption path now; it already
    // accounts for who's actually hungry/thirsty and by how much, which a
    // flat per-capita daily tax never did. (The removed method also had an
    // independent bug: its Water branch was gated on the Food quantity,
    // not Water - moot now that the whole method is gone.)

    private void ProcessProduction(Settlement settlement, long tick)
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

                // Week 26 leftover-consolidation sweep: _totalGoodsCrafted
                // has been tracked here since before this development cycle
                // began, and "GoodsCrafted" already sits in
                // SignificanceEvaluator's always-Medium whitelist, but
                // GoodsCraftedEvent was never actually published anywhere -
                // HistorySystem's OnGoodsCrafted subscriber existed with
                // nothing to ever call it.
                _eventBus.Publish(new GoodsCraftedEvent
                {
                    Tick = tick,
                    SettlementId = settlement.Id,
                    SettlementName = settlement.Name,
                    Product = "Planks",
                    Quantity = 3
                });

                _logger.LogDebug("Workshop in {Settlement} produced planks", settlement.Name);
            }
        }
    }

    public void RecordTrade()
    {
        _totalTrades++;
    }
}
