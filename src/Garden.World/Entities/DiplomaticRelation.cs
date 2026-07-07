using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public enum RelationType
{
    Neutral,
    Friendly,
    Allied,
    Suspicious,
    Hostile
}

public class DiplomaticRelation
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public GameEntityId EntityAId { get; init; }
    public string EntityAName { get; set; } = string.Empty;
    public bool EntityAIsSettlement { get; init; }
    public GameEntityId EntityBId { get; init; }
    public string EntityBName { get; set; } = string.Empty;
    public bool EntityBIsSettlement { get; init; }
    public RelationType CurrentRelation { get; set; } = RelationType.Neutral;
    public double RelationScore { get; set; } = 50.0;
    public bool HasTradeAgreement { get; set; }
    public bool IsAlliance { get; set; }
    public long LastInteractionTick { get; set; }
    public long EstablishedTick { get; set; }
}
