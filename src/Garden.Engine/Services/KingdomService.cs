using Garden.Core.Events;
using Garden.Core.Identifiers;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class KingdomService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<KingdomService> _logger;
    private int _kingdomNameIndex;

    private static readonly string[] KingdomPrefixes =
        ["Northern", "Southern", "Eastern", "Western", "Central", "Greater", "Lesser", "Old", "New", "Free"];
    private static readonly string[] KingdomSuffixes =
        ["Realm", "Dominion", "Territory", "Kingdom", "Lands", "Union", "Alliance", "Confederacy", "Federation"];

    public KingdomService(WorldState worldState, IEventBus eventBus, ILogger<KingdomService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void EvaluateKingdomFormation(long tick)
    {
        var activeSettlements = _worldState.Settlements
            .Where(s => s.LeaderId != null && s.MemberIds.Count >= 3)
            .ToList();

        if (activeSettlements.Count < 2) return;

        foreach (var settlement in activeSettlements)
        {
            if (settlement.KingdomId != null) continue;
            if (_worldState.Kingdoms.Any(k => k.IsActive && k.MemberSettlementIds.Contains(settlement.Id))) continue;

            var nearby = activeSettlements
                .Where(s => s.Id != settlement.Id
                    && s.KingdomId == null
                    && GetDistance(s, settlement) <= 20
                    && !_worldState.Kingdoms.Any(k => k.IsActive && k.MemberSettlementIds.Contains(s.Id)))
                .ToList();

            if (nearby.Count < 1) continue;

            var diplomatic = _worldState.DiplomaticRelations
                .FirstOrDefault(r =>
                    (r.EntityAId == settlement.Id && r.EntityBId == nearby[0].Id ||
                     r.EntityAId == nearby[0].Id && r.EntityBId == settlement.Id) &&
                    r.CurrentRelation == RelationType.Friendly);

            if (diplomatic == null && nearby[0].LeaderId == null) continue;

            var candidates = new List<Settlement> { settlement };
            candidates.AddRange(nearby.Take(2));

            FormKingdom(candidates, tick);
        }
    }

    private void FormKingdom(List<Settlement> members, long tick)
    {
        var capital = members.OrderByDescending(s => s.MemberIds.Count).First()!;
        var leader = _worldState.Citizens.FirstOrDefault(c => c.Id == capital.LeaderId);
        var name = GenerateKingdomName();
        var kingdom = new Kingdom
        {
            Name = name,
            CapitalSettlementId = capital.Id,
            CapitalName = capital.Name,
            LeaderId = capital.LeaderId ?? GameEntityId.New(),
            LeaderName = capital.LeaderName,
            GovernmentType = capital.GovernmentType,
            FoundedTick = tick,
            Population = members.Sum(s => s.MemberIds.Count),
            TerritoryRadius = members.Max(s => s.TerritoryRadius) + 5
        };

        foreach (var member in members)
        {
            kingdom.MemberSettlementIds.Add(member.Id);
            member.KingdomId = kingdom.Id;
        }

        _worldState.Kingdoms.Add(kingdom);

        _eventBus.Publish(new KingdomFoundedEvent
        {
            Tick = tick,
            KingdomId = kingdom.Id,
            KingdomName = kingdom.Name,
            CapitalSettlementId = capital.Id,
            CapitalName = capital.Name,
            LeaderId = kingdom.LeaderId,
            LeaderName = kingdom.LeaderName,
            MemberCount = members.Count
        });

        _logger.LogInformation("Kingdom '{Name}' formed with {Count} settlements, capital {Capital}",
            name, members.Count, capital.Name);
    }

    public void EvaluateKingdomDissolution(long tick)
    {
        foreach (var kingdom in _worldState.Kingdoms.Where(k => k.IsActive).ToList())
        {
            var activeMembers = kingdom.MemberSettlementIds
                .Count(id => _worldState.Settlements.Any(s => s.Id == id));
            if (activeMembers < 2)
            {
                kingdom.IsActive = false;
                kingdom.DissolvedTick = tick;

                foreach (var sid in kingdom.MemberSettlementIds)
                {
                    var s = _worldState.Settlements.FirstOrDefault(x => x.Id == sid);
                    if (s != null) s.KingdomId = null;
                }

                _eventBus.Publish(new KingdomDissolvedEvent
                {
                    Tick = tick,
                    KingdomId = kingdom.Id,
                    KingdomName = kingdom.Name,
                    Reason = "Too few active member settlements"
                });

                _logger.LogInformation("Kingdom '{Name}' dissolved", kingdom.Name);
            }
        }
    }

    private string GenerateKingdomName()
    {
        var p = KingdomPrefixes[_kingdomNameIndex % KingdomPrefixes.Length];
        var s = KingdomSuffixes[_kingdomNameIndex / KingdomPrefixes.Length % KingdomSuffixes.Length];
        _kingdomNameIndex++;
        return $"{p} {s}";
    }

    private static int GetDistance(Settlement a, Settlement b) =>
        Math.Abs(a.TileX - b.TileX) + Math.Abs(a.TileY - b.TileY);
}
