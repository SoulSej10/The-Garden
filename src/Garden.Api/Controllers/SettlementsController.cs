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

        var allMembers = _worldState.Citizens.Where(c => settlement.MemberIds.Contains(c.Id)).ToList();
        var aliveMembers = allMembers.Where(c => c.IsAlive).ToList();

        var members = allMembers
            .Select(c => new
            {
                c.Id, c.FirstName, c.LastName, c.Age,
                c.CurrentActivity, c.CurrentGoal,
                c.TileX, c.TileY, c.IsAlive
            }).ToList();

        // Nearby resources: what raw materials are actually available within
        // the settlement's territory, so residents (and the player) can see
        // what the local land can support.
        var territoryTiles = _worldState.Map.GetAllTiles()
            .Where(t => settlement.IsWithinTerritory(t.X, t.Y))
            .ToList();
        var nearbyResources = territoryTiles
            .SelectMany(t => t.Resources)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key.ToString(), Total = Math.Round(g.Sum(r => r.Quantity), 0) })
            .OrderByDescending(r => r.Total)
            .ToList();

        // Current problems: simple, honest signals derived from real state -
        // not fabricated, per the assistant's "never fabricate" rule.
        var problems = new List<string>();
        if (settlement.Storage.GetQuantity("Food") < 5 && aliveMembers.Count > 0)
            problems.Add("Low food reserves");
        if (!settlement.Buildings.Any(b => b.BuildingType == "Well" && b.Status.ToString() == "Completed"))
            problems.Add("No well - residents depend on natural water sources");
        if (!settlement.HasAvailableHousing && aliveMembers.Count > 0)
            problems.Add("Housing at capacity");
        if (aliveMembers.Count > 0 && aliveMembers.Average(c => c.Needs.Health) < 50)
            problems.Add("Average citizen health is poor");

        var ongoingProjects = settlement.Buildings
            .Where(b => b.Status.ToString() is "Planned" or "UnderConstruction")
            .Select(b => new { b.BuildingType, Status = b.Status.ToString(), b.BuildProgress, b.BuildTimeRequired })
            .ToList();

        return Ok(new
        {
            settlement.Id,
            settlement.Name,
            settlement.Population,
            settlement.TileX,
            settlement.TileY,
            settlement.TerritoryRadius,
            settlement.FoundedTick,
            settlement.LeaderName,
            settlement.GovernmentType,
            settlement.ReligionName,
            settlement.TechnologyProgress,
            CulturalTraits = settlement.CulturalTraits.Select(t => new { t.Name, t.Description }),
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
            Members = members,
            // Wellbeing is approximated from members' actual needs until a
            // dedicated settlement-level morale system exists - it is a real
            // aggregate of live citizen state, not a fabricated number.
            Wellbeing = aliveMembers.Count > 0
                ? new
                {
                    AverageHealth = Math.Round(aliveMembers.Average(c => c.Needs.Health), 1),
                    AverageHunger = Math.Round(aliveMembers.Average(c => c.Needs.Hunger), 1),
                    AverageThirst = Math.Round(aliveMembers.Average(c => c.Needs.Thirst), 1),
                    AverageEnergy = Math.Round(aliveMembers.Average(c => c.Needs.Energy), 1)
                }
                : null,
            NearbyResources = nearbyResources,
            OngoingProjects = ongoingProjects,
            CurrentProblems = problems,
            // Not yet modeled in the simulation - surfaced as explicit
            // placeholders rather than omitted, so the UI can show them once
            // these systems exist instead of silently having no field.
            Families = (object?)null,
            Security = (object?)null,
            TradeRelationships = (object?)null
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
