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
    private readonly HistoricalArchive _archive;

    public CivilizationController(
        WorldState worldState,
        TechnologyService technologyService,
        KingdomService kingdomService,
        HistoricalArchive archive)
    {
        _worldState = worldState;
        _technologyService = technologyService;
        _kingdomService = kingdomService;
        _archive = archive;
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
            TechnologiesDiscovered = _worldState.SettlementTechnologies.Count(t => t.IsDiscovered),
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
                Id = k.Id.ToString(), k.Name, k.CapitalName, k.LeaderName,
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
                Id = s.Id.ToString(), s.Name, s.Population, s.GovernmentType,
                s.LeaderName, s.TileX, s.TileY, CompletedBuildings = s.CompletedBuildings,
                FoodReserves = s.Storage.GetQuantity("Food")
            }).ToList();

        var relations = _worldState.DiplomaticRelations
            .Where(r => kingdom.MemberSettlementIds.Contains(r.EntityAId)
                     || kingdom.MemberSettlementIds.Contains(r.EntityBId))
            .Select(r => new
            {
                Id = r.Id.ToString(),
                EntityA = r.EntityAName,
                EntityB = r.EntityBName,
                Relation = r.CurrentRelation.ToString(),
                r.RelationScore, r.HasTradeAgreement, r.IsAlliance
            }).ToList();

        return Ok(new
        {
            Id = kingdom.Id.ToString(), kingdom.Name, kingdom.CapitalName,
            Leader = leader != null ? new
            {
                Id = leader.Id.ToString(), Name = $"{leader.FirstName} {leader.LastName}",
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
                Id = s.Id.ToString(), s.Name, s.GovernmentType,
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
                    LeaderId = citizen.Id.ToString(),
                    LeaderName = $"{citizen.FirstName} {citizen.LastName}",
                    citizen.Age, citizen.BiologicalSex,
                    citizen.ContributionScore, citizen.Reputation,
                    citizen.Attributes.Intelligence,
                    citizen.Personality.Compassion,
                    citizen.Personality.Diligence,
                    citizen.Personality.Aggression,
                    SettlementId = s.Id.ToString(),
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
            Id = r.Id.ToString(), EntityA = r.EntityAName, EntityB = r.EntityBName,
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
                Id = r.Id.ToString(), r.FromSettlementName, r.ToSettlementName,
                r.PrimaryGood, r.TotalVolumeTransported, r.TripCount,
                r.Distance, r.EconomicValue, r.EstablishedTick, r.LastTripTick
            }).ToList();
        return Ok(result);
    }

    // RFC-015: discovery is now per-settlement (ADR-004), so this endpoint
    // takes an optional settlementId - omitted, it returns the aggregate
    // world-wide discovered list (every settlement's discoveries combined,
    // for the world-summary Civilization dashboard); provided, it also
    // returns that one settlement's own in-progress technologies, which
    // only make sense scoped to a single settlement.
    [HttpGet("technology")]
    public IActionResult GetTechnology(Guid? settlementId = null)
    {
        var scopedId = settlementId.HasValue
            ? new Garden.Core.Identifiers.GameEntityId(settlementId.Value)
            : (Garden.Core.Identifiers.GameEntityId?)null;

        var discovered = _technologyService.GetDiscoveredTechnologies(scopedId)
            .Select(t => new
            {
                t.Id, t.Name, t.Category, t.Description,
                t.DiscoveredTick,
                t.SettlementId, t.SettlementName,
                CitizenName = t.DiscoveredByCitizenName
            }).ToList();

        var inProgressSource = scopedId.HasValue
            ? _technologyService.GetUndiscoveredTechnologies(scopedId.Value)
            : _technologyService.GetUndiscoveredTechnologiesAggregate();

        var inProgress = inProgressSource
            .Select(t => new
            {
                t.Id, t.Name, t.Category, t.Description,
                Progress = Math.Round(t.CurrentProgress / t.ProgressRequired * 100, 1),
                t.ProgressRequired, t.CurrentProgress
            }).ToList();

        return Ok(new { Discovered = discovered, InProgress = inProgress });
    }

    // RFC-016: minimal read-only surfacing of formed Legends, each paired
    // with the original HistoricalRecord it distorted (so the Observatory
    // can show fact and myth side by side, per TG-STRY-040's "Legends
    // never overwrite objective history. They exist alongside it.").
    [HttpGet("legends")]
    public IActionResult GetLegends()
    {
        var result = _worldState.Legends
            .OrderByDescending(l => l.FormedTick)
            .Select(l =>
            {
                var source = _archive.GetById(l.SourceRecordId.Value);
                return new
                {
                    Id = l.Id.ToString(), l.Title, l.DistortedNarrative,
                    LegendaryStatus = Math.Round(l.LegendaryStatus, 1),
                    l.FormedTick,
                    OriginalTitle = source?.Title,
                    OriginalDescription = source?.Description
                };
            }).ToList();
        return Ok(result);
    }

    [HttpGet("culture")]
    public IActionResult GetCulture()
    {
        var result = _worldState.Settlements
            .Where(s => s.CulturalTraits.Count > 0)
            .Select(s => new
            {
                Id = s.Id.ToString(), s.Name, s.Population,
                Traits = s.CulturalTraits.Select(t => new
                {
                    Id = t.Id.ToString(), t.Name, t.Description, t.Category,
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
            Id = r.Id.ToString(), r.Name, r.Description, r.CoreValue,
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
                Id = c.Id.ToString(), Name = $"{c.FirstName} {c.LastName}",
                c.TileX, c.TileY, c.CurrentActivity
            }).ToList();

        return Ok(new { CurrentMigrants = migrants });
    }
}
