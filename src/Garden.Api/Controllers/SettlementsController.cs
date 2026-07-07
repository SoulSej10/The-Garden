using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class SettlementsController : ControllerBase
{
    private readonly WorldState _worldState;
    private readonly SettlementManager _settlementManager;
    private readonly ConstructionSystem _constructionSystem;

    public SettlementsController(
        WorldState worldState,
        SettlementManager settlementManager,
        ConstructionSystem constructionSystem)
    {
        _worldState = worldState;
        _settlementManager = settlementManager;
        _constructionSystem = constructionSystem;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var result = _worldState.Settlements.Select(s => new
        {
            s.Id,
            s.Name,
            s.Population,
            s.TileX,
            s.TileY,
            s.TerritoryRadius,
            CompletedBuildings = s.CompletedBuildings,
            UnderConstruction = s.UnderConstructionBuildings,
            TotalBuildings = s.Buildings.Count,
            FoodReserves = s.Storage.GetQuantity("Food"),
            WaterReserves = s.Storage.GetQuantity("Water"),
            WoodReserves = s.Storage.GetQuantity("Wood"),
            StoneReserves = s.Storage.GetQuantity("Stone"),
            Members = s.MemberIds.Count
        }).ToList();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id)
    {
        var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id.Value == id);
        if (settlement == null) return NotFound();

        var members = _worldState.Citizens
            .Where(c => settlement.MemberIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id, c.FirstName, c.LastName, c.Age,
                c.CurrentActivity, c.CurrentGoal,
                c.TileX, c.TileY, c.IsAlive
            }).ToList();

        return Ok(new
        {
            settlement.Id,
            settlement.Name,
            settlement.Population,
            settlement.TileX,
            settlement.TileY,
            settlement.TerritoryRadius,
            settlement.FoundedTick,
            Storage = settlement.Storage.Items.Select(i => new
            {
                i.ItemType, i.Quantity, i.Weight
            }),
            Buildings = settlement.Buildings.Select(b => new
            {
                b.Id, b.BuildingType, Status = b.Status.ToString(),
                b.BuildProgress, b.TileX, b.TileY,
                b.BuildTimeRequired,
                AssignedWorkerId = b.AssignedWorkerId?.Value
            }),
            Members = members
        });
    }

    [HttpGet("{id}/buildings")]
    public IActionResult GetBuildings(Guid id)
    {
        var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id.Value == id);
        if (settlement == null) return NotFound();

        var result = settlement.Buildings.Select(b => new
        {
            b.Id, b.BuildingType, Status = b.Status.ToString(),
            b.BuildProgress, b.TileX, b.TileY,
            b.BuildTimeRequired
        }).ToList();
        return Ok(result);
    }

    [HttpPost("{id}/buildings/plan")]
    public IActionResult PlanBuilding(Guid id, [FromBody] PlanBuildingRequest request)
    {
        var settlement = _worldState.Settlements.FirstOrDefault(s => s.Id.Value == id);
        if (settlement == null) return NotFound();

        var validTypes = Garden.World.Entities.BuildingTypes.All;
        if (!validTypes.Contains(request.BuildingType))
            return BadRequest($"Invalid building type. Valid: {string.Join(", ", validTypes)}");

        var building = new Garden.World.Entities.Building
        {
            BuildingType = request.BuildingType,
            TileX = request.TileX,
            TileY = request.TileY,
            BuildTimeRequired = Garden.World.Entities.BuildingTypes.GetBuildTime(request.BuildingType)
        };

        _constructionSystem.PlanBuilding(settlement, request.BuildingType, request.TileX, request.TileY);
        return Ok(building);
    }
}

public record PlanBuildingRequest(string BuildingType, int TileX, int TileY);
