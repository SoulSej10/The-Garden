using Garden.Engine.Services;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CivilizationController : ControllerBase
{
    private readonly WorldState _worldState;
    private readonly TechnologyService _technologyService;
    private readonly KingdomService _kingdomService;

    public CivilizationController(
        WorldState worldState,
        TechnologyService technologyService,
        KingdomService kingdomService)
    {
        _worldState = worldState;
        _technologyService = technologyService;
        _kingdomService = kingdomService;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        return Ok(new
        {
            KingdomCount = _worldState.Kingdoms.Count(k => k.IsActive),
            GovernmentCounts = _worldState.Settlements
                .GroupBy(s => s.GovernmentType)
                .ToDictionary(g => g.Key, g => g.Count()),
            TradeRouteCount = _worldState.TradeRoutes.Count(r => r.IsActive),
            TechnologiesDiscovered = _worldState.Technologies.Count(t => t.IsDiscovered),
            CultureCount = _worldState.Settlements.Sum(s => s.CulturalTraits.Count),
            ReligionCount = _worldState.Religions.Count
        });
    }

    [HttpGet("kingdoms")]
    public IActionResult GetKingdoms()
    {
        var result = _worldState.Kingdoms.Where(k => k.IsActive).Select(k =>
        {
            var leader = _worldState.Citizens.FirstOrDefault(c => c.Id == k.LeaderId);
            return new
            {
                k.Id, k.Name, k.CapitalName, k.LeaderName,
                LeaderAge = leader?.Age ?? 0,
                k.Population, k.GovernmentType,
                SettlementCount = k.MemberSettlementIds.Count,
                k.TerritoryRadius, k.Stability, k.FoundedTick
            };
        }).ToList();
        return Ok(result);
    }

    [HttpGet("kingdoms/{id}")]
    public IActionResult GetKingdomDetail(Guid id)
    {
        var kingdom = _worldState.Kingdoms.FirstOrDefault(k => k.Id.Value == id && k.IsActive);
        if (kingdom == null) return NotFound();

        var leader = _worldState.Citizens.FirstOrDefault(c => c.Id == kingdom.LeaderId);
        var settlements = _worldState.Settlements
            .Where(s => kingdom.MemberSettlementIds.Contains(s.Id))
            .Select(s => new
            {
                s.Id, s.Name, s.Population, s.GovernmentType,
                s.LeaderName, s.TileX, s.TileY, CompletedBuildings = s.CompletedBuildings,
                FoodReserves = s.Storage.GetQuantity("Food")
            }).ToList();

        var relations = _worldState.DiplomaticRelations
            .Where(r => kingdom.MemberSettlementIds.Contains(r.EntityAId)
                     || kingdom.MemberSettlementIds.Contains(r.EntityBId))
            .Select(r => new
            {
                r.Id,
                EntityA = r.EntityAName,
                EntityB = r.EntityBName,
                Relation = r.CurrentRelation.ToString(),
                r.RelationScore, r.HasTradeAgreement, r.IsAlliance
            }).ToList();

        return Ok(new
        {
            kingdom.Id, kingdom.Name, kingdom.CapitalName,
            Leader = leader != null ? new
            {
                leader.Id, Name = $"{leader.FirstName} {leader.LastName}",
                leader.Age, leader.Attributes.Intelligence,
                leader.Personality.Compassion, leader.ContributionScore
            } : null,
            kingdom.LeaderName, kingdom.Population, kingdom.GovernmentType,
            kingdom.TerritoryRadius, kingdom.Stability, kingdom.FoundedTick,
            Settlements = settlements, DiplomaticRelations = relations
        });
    }

    [HttpGet("governments")]
    public IActionResult GetGovernments()
    {
        var result = _worldState.Settlements
            .Where(s => s.LeaderId != null)
            .Select(s => new
            {
                s.Id, s.Name, s.GovernmentType,
                s.LeaderName, s.Population, s.FoundedTick,
                BuildingCount = s.CompletedBuildings
            }).ToList();
        return Ok(result);
    }

    [HttpGet("leaders")]
    public IActionResult GetLeaders()
    {
        var leaders = _worldState.Settlements
            .Where(s => s.LeaderId != null)
            .Select(s =>
            {
                var citizen = _worldState.Citizens.FirstOrDefault(c => c.Id == s.LeaderId);
                return citizen != null ? new
                {
                    LeaderId = citizen.Id,
                    LeaderName = $"{citizen.FirstName} {citizen.LastName}",
                    citizen.Age, citizen.BiologicalSex,
                    citizen.ContributionScore, citizen.Reputation,
                    citizen.Attributes.Intelligence,
                    citizen.Personality.Compassion,
                    citizen.Personality.Diligence,
                    citizen.Personality.Aggression,
                    SettlementId = s.Id,
                    SettlementName = s.Name,
                    s.GovernmentType
                } : null;
            })
            .Where(l => l != null)
            .ToList();
        return Ok(leaders);
    }

    [HttpGet("diplomacy")]
    public IActionResult GetDiplomacy()
    {
        var result = _worldState.DiplomaticRelations.Select(r => new
        {
            r.Id, EntityA = r.EntityAName, EntityB = r.EntityBName,
            Relation = r.CurrentRelation.ToString(), r.RelationScore,
            r.HasTradeAgreement, r.IsAlliance, r.LastInteractionTick,
            r.EstablishedTick
        }).ToList();
        return Ok(result);
    }

    [HttpGet("trade-routes")]
    public IActionResult GetTradeRoutes()
    {
        var result = _worldState.TradeRoutes
            .Where(r => r.IsActive)
            .Select(r => new
            {
                r.Id, r.FromSettlementName, r.ToSettlementName,
                r.PrimaryGood, r.TotalVolumeTransported, r.TripCount,
                r.Distance, r.EconomicValue, r.EstablishedTick, r.LastTripTick
            }).ToList();
        return Ok(result);
    }

    [HttpGet("technology")]
    public IActionResult GetTechnology()
    {
        var discovered = _technologyService.GetDiscoveredTechnologies()
            .Select(t => new
            {
                t.Id, t.Name, t.Category, t.Description,
                DiscoveredTick = t.DiscoveredTick,
                SettlementName = t.DiscoveredBySettlementName,
                CitizenName = t.DiscoveredByCitizenName
            }).ToList();

        var inProgress = _technologyService.GetUndiscoveredTechnologies()
            .Select(t => new
            {
                t.Id, t.Name, t.Category, t.Description,
                Progress = Math.Round(t.CurrentProgress / t.ProgressRequired * 100, 1),
                t.ProgressRequired, t.CurrentProgress
            }).ToList();

        return Ok(new { Discovered = discovered, InProgress = inProgress });
    }

    [HttpGet("culture")]
    public IActionResult GetCulture()
    {
        var result = _worldState.Settlements
            .Where(s => s.CulturalTraits.Count > 0)
            .Select(s => new
            {
                s.Id, s.Name, s.Population,
                Traits = s.CulturalTraits.Select(t => new
                {
                    t.Id, t.Name, t.Description, t.Category,
                    t.Strength, t.EstablishedTick
                }),
                s.ReligionName
            }).ToList();
        return Ok(result);
    }

    [HttpGet("religion")]
    public IActionResult GetReligion()
    {
        var result = _worldState.Religions.Select(r => new
        {
            r.Id, r.Name, r.Description, r.CoreValue,
            Tenets = r.Tenets, r.FollowerCount, r.OriginSettlementName,
            r.CulturalInfluence, r.EstablishedTick
        }).ToList();
        return Ok(result);
    }

    [HttpGet("migration")]
    public IActionResult GetMigration()
    {
        var migrants = _worldState.Citizens
            .Where(c => c.IsAlive && (c.CurrentGoal == "Migrating" || c.CurrentActivity == "Migrating"))
            .Select(c => new
            {
                c.Id, Name = $"{c.FirstName} {c.LastName}",
                c.TileX, c.TileY, c.CurrentActivity
            }).ToList();

        return Ok(new { CurrentMigrants = migrants });
    }
}
