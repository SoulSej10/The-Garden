using Garden.Engine.Systems;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class EconomyController : ControllerBase
{
    private readonly WorldState _worldState;
    private readonly EconomySystem _economySystem;

    public EconomyController(WorldState worldState, EconomySystem economySystem)
    {
        _worldState = worldState;
        _economySystem = economySystem;
    }

    [HttpGet]
    public IActionResult GetEconomy()
    {
        var settlements = _worldState.Settlements.Select(s => new
        {
            s.Name,
            s.Population,
            s.FoundedTick,
            TotalFood = s.Storage.GetQuantity("Food"),
            TotalWater = s.Storage.GetQuantity("Water"),
            TotalWood = s.Storage.GetQuantity("Wood"),
            TotalStone = s.Storage.GetQuantity("Stone"),
            BuildingCount = s.CompletedBuildings,
            BuildingsByType = s.Buildings
                .Where(b => b.Status == Garden.World.Entities.BuildingStatus.Completed)
                .GroupBy(b => b.BuildingType)
                .ToDictionary(g => g.Key, g => g.Count())
        }).ToList();

        return Ok(new
        {
            Tick = _worldState.CurrentTime.Tick,
            Settlements = settlements,
            GlobalGoodsCrafted = _economySystem.TotalGoodsCrafted,
            GlobalTradeCount = _economySystem.TotalTrades,
            TotalSettlements = _worldState.Settlements.Count
        });
    }

    [HttpGet("resources")]
    public IActionResult GetResources()
    {
        var allItems = _worldState.Settlements
            .SelectMany(s => s.Storage.Items)
            .GroupBy(i => i.ItemType)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        return Ok(allItems);
    }
}
