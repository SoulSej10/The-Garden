using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class ReligionService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ReligionService> _logger;

    public ReligionService(WorldState worldState, IEventBus eventBus, ILogger<ReligionService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void EvaluateReligion(long tick)
    {
        foreach (var settlement in _worldState.Settlements.Where(s => s.MemberIds.Count >= 5))
        {
            if (settlement.ReligionId == null)
            {
                var chance = settlement.CulturalTraits.Count * 0.005;
                if (System.Random.Shared.NextDouble() < chance)
                {
                    EstablishReligion(settlement, tick);
                }
            }

            UpdateFollowerCounts(settlement);
        }
    }

    private void EstablishReligion(Settlement settlement, long tick)
    {
        var template = Religion.Templates[System.Random.Shared.Next(Religion.Templates.Count)];
        var coreTraits = settlement.CulturalTraits.Select(t => t.Name).ToList();

        var religion = new Religion
        {
            Name = settlement.Name.Length <= 8
                ? $"{settlement.Name} Faith"
                : $"{settlement.Name[..Math.Min(8, settlement.Name.Length)]} Faith",
            Description = template.Description,
            CoreValue = template.CoreValue,
            OriginSettlementId = settlement.Id,
            OriginSettlementName = settlement.Name,
            EstablishedTick = tick,
            FollowerCount = 1
        };

        foreach (var tenet in template.Tenets)
            religion.Tenets.Add(tenet);

        _worldState.Religions.Add(religion);
        settlement.ReligionId = religion.Id;
        settlement.ReligionName = religion.Name;

        var founder = _worldState.Citizens
            .Where(c => settlement.MemberIds.Contains(c.Id) && c.IsAlive)
            .OrderByDescending(c => c.Personality.Compassion + c.Attributes.Intelligence)
            .FirstOrDefault();

        if (founder != null)
        {
            founder.ReligionId = religion.Id;
            founder.ReligionName = religion.Name;
            religion.FollowerIds.Add(founder.Id);
            religion.FollowerCount = 1;
        }

        _eventBus.Publish(new ReligionEstablishedEvent
        {
            Tick = tick,
            ReligionId = religion.Id.Value.ToString(),
            ReligionName = religion.Name,
            CoreValue = religion.CoreValue,
            OriginSettlementName = settlement.Name,
            InitialFollowers = religion.FollowerCount,
            FounderCitizenId = founder?.Id
        });

        _logger.LogInformation("Religion '{Religion}' established in {Settlement}",
            religion.Name, settlement.Name);
    }

    private void UpdateFollowerCounts(Settlement settlement)
    {
        var religion = _worldState.Religions.FirstOrDefault(r => r.Id == settlement.ReligionId);
        if (religion == null) return;

        var members = _worldState.Citizens
            .Where(c => settlement.MemberIds.Contains(c.Id) && c.IsAlive)
            .ToList();

        foreach (var citizen in members)
        {
            if (citizen.ReligionId == null)
            {
                var conversionChance = religion.CulturalInfluence * 0.01;
                if (System.Random.Shared.NextDouble() < conversionChance)
                {
                    citizen.ReligionId = religion.Id;
                    citizen.ReligionName = religion.Name;
                    religion.FollowerIds.Add(citizen.Id);
                }
            }
        }

        religion.FollowerCount = religion.FollowerIds.Count;
    }

    public void SpreadReligion(long tick)
    {
        foreach (var religion in _worldState.Religions)
        {
            var memberSettlements = _worldState.Settlements
                .Where(s => s.ReligionId == religion.Id)
                .ToList();

            foreach (var settlement in _worldState.Settlements)
            {
                if (settlement.ReligionId == religion.Id) continue;

                var hasContact = memberSettlements.Any(ms =>
                    Math.Abs(ms.TileX - settlement.TileX) + Math.Abs(ms.TileY - settlement.TileY) <= 20);

                if (!hasContact) continue;

                if (System.Random.Shared.NextDouble() < religion.CulturalInfluence * 0.005)
                {
                    settlement.ReligionId = religion.Id;
                    settlement.ReligionName = religion.Name;

                    var converts = _worldState.Citizens
                        .Where(c => settlement.MemberIds.Contains(c.Id) && c.IsAlive)
                        .Take(System.Random.Shared.Next(1, 4))
                        .ToList();

                    foreach (var citizen in converts)
                    {
                        citizen.ReligionId = religion.Id;
                        citizen.ReligionName = religion.Name;
                        religion.FollowerIds.Add(citizen.Id);
                    }

                    religion.FollowerCount = religion.FollowerIds.Count;
                    _logger.LogDebug("Religion '{Religion}' spread to {Settlement}",
                        religion.Name, settlement.Name);
                }
            }

            religion.CulturalInfluence = Math.Min(1.0, religion.CulturalInfluence + religion.FollowerCount * 0.0001);
        }
    }
}
