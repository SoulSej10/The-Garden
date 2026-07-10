using Garden.Engine.Services;
using Garden.Engine.Systems;
using Garden.World.Collections;
using Microsoft.AspNetCore.Mvc;

namespace Garden.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CitizensController : ControllerBase
{
    private readonly WorldState _worldState;
    private readonly PopulationManager _populationManager;
    private readonly CommunicationSystem _communicationSystem;

    public CitizensController(WorldState worldState, PopulationManager populationManager,
        CommunicationSystem communicationSystem)
    {
        _worldState = worldState;
        _populationManager = populationManager;
        _communicationSystem = communicationSystem;
    }

    [HttpGet]
    public IActionResult GetCitizens([FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null, [FromQuery] string? sortBy = "name")
    {
        var citizens = _worldState.Citizens
            .Where(c => c.IsAlive)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            citizens = citizens.Where(c =>
                c.FirstName.ToLower().Contains(term) ||
                c.LastName.ToLower().Contains(term));
        }

        citizens = sortBy?.ToLower() switch
        {
            "age" => citizens.OrderByDescending(c => c.Age),
            "health" => citizens.OrderBy(c => c.Needs.Health),
            "activity" => citizens.OrderBy(c => c.CurrentActivity),
            _ => citizens.OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
        };

        var list = citizens.ToList();
        var total = list.Count;
        var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Citizens = paged.Select(MapCitizen)
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetCitizen(string id)
    {
        var citizen = _worldState.Citizens.FirstOrDefault(c => c.Id.ToString() == id);
        if (citizen == null)
            return NotFound(new { Error = "Citizen not found" });

        return Ok(new
        {
            Citizen = MapCitizenDetail(citizen, _communicationSystem, _worldState),
            RecentEvents = citizen.Memories
                .OrderByDescending(m => m.Tick)
                .Take(20)
                .Select(m => new { m.Tick, m.EventType, m.Description })
        });
    }

    [HttpGet("population")]
    public IActionResult GetPopulation()
    {
        var alive = _worldState.Citizens.Count(c => c.IsAlive);
        var dead = _worldState.Citizens.Count(c => !c.IsAlive);

        return Ok(new
        {
            Total = alive + dead,
            Alive = alive,
            Dead = dead,
            TotalBirths = _populationManager.TotalBirths,
            TotalDeaths = _populationManager.TotalDeaths,
            AverageAge = Math.Round(_populationManager.GetAverageAge(_worldState.Citizens), 1)
        });
    }

    [HttpGet("{id}/relationships")]
    public IActionResult GetRelationships(string id)
    {
        var citizen = _worldState.Citizens.FirstOrDefault(c => c.Id.ToString() == id);
        if (citizen == null) return NotFound(new { Error = "Citizen not found" });

        var relationships = _worldState.Relationships
            .Where(r => r.EntityAId == citizen.Id || r.EntityBId == citizen.Id)
            .Select(r =>
            {
                var otherId = r.EntityAId == citizen.Id ? r.EntityBId : r.EntityAId;
                var other = _worldState.Citizens.FirstOrDefault(c => c.Id == otherId);
                return new
                {
                    OtherCitizenId = otherId.ToString(),
                    OtherCitizenName = other != null ? $"{other.FirstName} {other.LastName}" : "Unknown",
                    Trust = Math.Round(r.Trust, 1),
                    Affection = Math.Round(r.Affection, 1),
                    SocialDistance = Math.Round(r.SocialDistance, 1),
                    r.InteractionCount,
                    r.EstablishedTick,
                    r.LastInteractionTick
                };
            })
            .OrderBy(r => r.SocialDistance)
            .ToList();

        return Ok(relationships);
    }

    [HttpGet("statistics")]
    public IActionResult GetStatistics()
    {
        var alive = _worldState.Citizens.Where(c => c.IsAlive).ToList();
        var dist = _populationManager.GetAgeDistribution(_worldState.Citizens);

        return Ok(new
        {
            AgeDistribution = new
            {
                Infants = dist.Infants,
                Children = dist.Children,
                Teens = dist.Teens,
                Adults = dist.Adults,
                Elders = dist.Elders
            },
            AverageNeeds = alive.Count > 0 ? new
            {
                Hunger = Math.Round(alive.Average(c => c.Needs.Hunger), 1),
                Thirst = Math.Round(alive.Average(c => c.Needs.Thirst), 1),
                Energy = Math.Round(alive.Average(c => c.Needs.Energy), 1),
                Health = Math.Round(alive.Average(c => c.Needs.Health), 1),
                Warmth = Math.Round(alive.Average(c => c.Needs.Warmth), 1)
            } : null,
            DeathCauses = _populationManager.DeathCauses
                .Select(kv => new { Cause = kv.Key, Count = kv.Value })
        });
    }

    private static object MapCitizen(World.Entities.Citizen c)
    {
        return new
        {
            Id = c.Id.ToString(),
            Name = $"{c.FirstName} {c.LastName}",
            c.Age,
            Stage = c.Stage.ToString(),
            c.CurrentActivity,
            c.CurrentGoal,
            c.TileX,
            c.TileY,
            Health = Math.Round(c.Needs.Health, 1),
            Hunger = Math.Round(c.Needs.Hunger, 1),
            Thirst = Math.Round(c.Needs.Thirst, 1),
            Energy = Math.Round(c.Needs.Energy, 1),
            Warmth = Math.Round(c.Needs.Warmth, 1)
        };
    }

    // RFC-002 Day 24: minimal read-only "what this citizen knows" surfacing -
    // resolves each KnownEventIds key against CommunicationSystem.EventTitles
    // (the same in-memory dictionary the system populates when it marks a
    // discoverer/founder, or diffuses knowledge to a listener).
    private static object MapCitizenDetail(World.Entities.Citizen c, CommunicationSystem communicationSystem, WorldState worldState)
    {
        return new
        {
            Id = c.Id.ToString(),
            FirstName = c.FirstName,
            LastName = c.LastName,
            c.Age,
            Stage = c.Stage.ToString(),
            c.BiologicalSex,
            c.IsAlive,
            c.CurrentActivity,
            c.CurrentGoal,
            c.TileX,
            c.TileY,
            Attributes = new
            {
                Strength = Math.Round(c.Attributes.Strength, 1),
                Endurance = Math.Round(c.Attributes.Endurance, 1),
                Intelligence = Math.Round(c.Attributes.Intelligence, 1),
                Dexterity = Math.Round(c.Attributes.Dexterity, 1),
                Perception = Math.Round(c.Attributes.Perception, 1)
            },
            Personality = new
            {
                Curiosity = Math.Round(c.Personality.Curiosity, 1),
                Patience = Math.Round(c.Personality.Patience, 1),
                Aggression = Math.Round(c.Personality.Aggression, 1),
                Compassion = Math.Round(c.Personality.Compassion, 1),
                Diligence = Math.Round(c.Personality.Diligence, 1),
                Introversion = Math.Round(c.Personality.Introversion, 1)
            },
            Needs = new
            {
                Hunger = Math.Round(c.Needs.Hunger, 1),
                Thirst = Math.Round(c.Needs.Thirst, 1),
                Energy = Math.Round(c.Needs.Energy, 1),
                Warmth = Math.Round(c.Needs.Warmth, 1),
                Health = Math.Round(c.Needs.Health, 1)
            },
            Emotions = new
            {
                Fear = Math.Round(c.Emotions.Fear, 1),
                Joy = Math.Round(c.Emotions.Joy, 1),
                Sadness = Math.Round(c.Emotions.Sadness, 1),
                Trust = Math.Round(c.Emotions.Trust, 1),
                Curiosity = Math.Round(c.Emotions.Curiosity, 1),
                Loneliness = Math.Round(c.Emotions.Loneliness, 1)
            },
            KnownEvents = c.KnownEventIds
                .Select(key => new
                {
                    Key = key,
                    Title = communicationSystem.EventTitles.TryGetValue(key, out var title) ? title : key
                })
                .ToList(),
            Apprenticeship = BuildApprenticeship(c, worldState)
        };
    }

    // RFC-004 Day 39: minimal read-only surfacing of a citizen's active
    // apprenticeship, if any - as either the mentor or the student.
    private static object? BuildApprenticeship(World.Entities.Citizen c, WorldState worldState)
    {
        var apprenticeship = worldState.Apprenticeships
            .FirstOrDefault(a => a.IsActive && (a.MentorId == c.Id || a.StudentId == c.Id));
        if (apprenticeship == null) return null;

        var isMentor = apprenticeship.MentorId == c.Id;
        var otherId = isMentor ? apprenticeship.StudentId : apprenticeship.MentorId;
        var other = worldState.Citizens.FirstOrDefault(x => x.Id == otherId);

        return new
        {
            Role = isMentor ? "Mentor" : "Student",
            OtherCitizenId = otherId.ToString(),
            OtherCitizenName = other != null ? $"{other.FirstName} {other.LastName}" : "Unknown"
        };
    }
}
