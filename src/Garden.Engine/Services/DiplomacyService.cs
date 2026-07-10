using Garden.Core.Events;
using Garden.Core.Interfaces;
using Garden.World.Collections;
using Garden.World.Entities;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Services;

public class DiplomacyService
{
    private readonly WorldState _worldState;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DiplomacyService> _logger;

    public DiplomacyService(WorldState worldState, IEventBus eventBus, ILogger<DiplomacyService> logger)
    {
        _worldState = worldState;
        _eventBus = eventBus;
        _logger = logger;
    }

    public void EvaluateDiplomacy(long tick)
    {
        var settlements = _worldState.Settlements.ToList();

        for (var i = 0; i < settlements.Count; i++)
        {
            for (var j = i + 1; j < settlements.Count; j++)
            {
                var a = settlements[i];
                var b = settlements[j];
                var dist = Math.Abs(a.TileX - b.TileX) + Math.Abs(a.TileY - b.TileY);
                if (dist > 30) continue;

                var relation = _worldState.DiplomaticRelations
                    .FirstOrDefault(r =>
                        (r.EntityAId == a.Id && r.EntityBId == b.Id) ||
                        (r.EntityAId == b.Id && r.EntityBId == a.Id));

                if (relation == null && dist <= 15)
                {
                    relation = new DiplomaticRelation
                    {
                        EntityAId = a.Id,
                        EntityAName = a.Name,
                        EntityAIsSettlement = true,
                        EntityBId = b.Id,
                        EntityBName = b.Name,
                        EntityBIsSettlement = true,
                        EstablishedTick = tick,
                        LastInteractionTick = tick
                    };
                    _worldState.DiplomaticRelations.Add(relation);
                }

                if (relation == null) continue;

                var scoreChange = CalculateDiplomaticScoreChange(a, b, dist, tick, relation);
                relation.RelationScore = Math.Clamp(relation.RelationScore + scoreChange, 0, 100);
                relation.LastInteractionTick = tick;

                var newRelation = ScoreToRelation(relation.RelationScore);
                if (newRelation != relation.CurrentRelation)
                {
                    var previous = relation.CurrentRelation;
                    relation.CurrentRelation = newRelation;

                    _eventBus.Publish(new DiplomaticRelationChangedEvent
                    {
                        Tick = tick,
                        EntityAId = a.Id,
                        EntityAName = a.Name,
                        EntityBId = b.Id,
                        EntityBName = b.Name,
                        PreviousRelation = previous.ToString(),
                        NewRelation = newRelation.ToString(),
                        IsSettlementLevel = true
                    });

                    _logger.LogDebug("Diplomacy between {A} and {B}: {Prev} -> {New}",
                        a.Name, b.Name, previous, newRelation);
                }
            }
        }
    }

    private static double CalculateDiplomaticScoreChange(
        Settlement a, Settlement b, int distance, long tick, DiplomaticRelation relation)
    {
        var change = 0.0;

        if (a.LeaderId != null && b.LeaderId != null)
            change += 0.05;

        if (GetKingdomRelation(a, b))
            change += 0.1;

        if (distance <= 8)
            change += 0.03;
        else if (distance > 20)
            change -= 0.02;

        if (relation.HasTradeAgreement)
            change += 0.08;

        if (relation.CurrentRelation == RelationType.Hostile)
            change -= 0.05;

        if (IsInSameKingdom(a, b))
            change += 0.2;

        // TG-630_Diplomacy.md frames diplomacy as accumulated trust; a
        // settlement whose own government is not yet legitimate (recent
        // upheaval, an unproven or untrusted leader) is a shakier diplomatic
        // partner. Only a penalty is applied (never a bonus) so this can't
        // be the dominant driver of warming relations - it just makes
        // instability a real, visible drag, consistent with GovernanceService.
        const double legitimacyThreshold = 40.0;
        if (a.Legitimacy < legitimacyThreshold || b.Legitimacy < legitimacyThreshold)
            change -= 0.03;

        return change;
    }

    private static bool GetKingdomRelation(Settlement a, Settlement b) => IsInSameKingdom(a, b);

    private static bool IsInSameKingdom(Settlement a, Settlement b) =>
        a.KingdomId != null && b.KingdomId != null && a.KingdomId.Equals(b.KingdomId);

    private static RelationType ScoreToRelation(double score) => score switch
    {
        >= 80 => RelationType.Allied,
        >= 60 => RelationType.Friendly,
        >= 40 => RelationType.Neutral,
        >= 20 => RelationType.Suspicious,
        _ => RelationType.Hostile
    };

    public void EstablishTradeAgreement(Settlement a, Settlement b, long tick)
    {
        var relation = _worldState.DiplomaticRelations
            .FirstOrDefault(r =>
                (r.EntityAId == a.Id && r.EntityBId == b.Id) ||
                (r.EntityAId == b.Id && r.EntityBId == a.Id));

        if (relation == null)
        {
            relation = new DiplomaticRelation
            {
                EntityAId = a.Id,
                EntityAName = a.Name,
                EntityAIsSettlement = true,
                EntityBId = b.Id,
                EntityBName = b.Name,
                EntityBIsSettlement = true,
                EstablishedTick = tick,
                LastInteractionTick = tick
            };
            _worldState.DiplomaticRelations.Add(relation);
        }

        relation.HasTradeAgreement = true;
        relation.CurrentRelation = RelationType.Friendly;
        relation.RelationScore = Math.Max(relation.RelationScore, 60);
    }
}
