using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class Religion
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CoreValue { get; set; } = string.Empty;
    public List<string> Tenets { get; init; } = [];
    public int FollowerCount { get; set; }
    public List<GameEntityId> FollowerIds { get; init; } = [];
    public GameEntityId? OriginSettlementId { get; set; }
    public string OriginSettlementName { get; set; } = string.Empty;
    public long EstablishedTick { get; set; }
    public double CulturalInfluence { get; set; } = 0.1;

    public static IReadOnlyList<ReligionTemplate> Templates => _templates;
    private static readonly List<ReligionTemplate> _templates =
    [
        new("Ancestor Worship", "Honoring those who came before", "Reverence", ["Honor the past", "Preserve traditions", "Seek wisdom from elders"]),
        new("Nature Harmony", "Living in balance with the world", "Balance", ["Respect all life", "Take only what you need", "Protect the wild"]),
        new("Community Faith", "Strength through unity", "Solidarity", ["Support your neighbors", "Share with the needy", "Build together"]),
        new("Knowledge Path", "Understanding through learning", "Truth", ["Question everything", "Share knowledge freely", "Preserve discoveries"]),
        new("Craftsmanship Creed", "Perfection through creation", "Excellence", ["Master your craft", "Create beauty", "Teach your skills"]),
    ];
}

public record ReligionTemplate(string Name, string Description, string CoreValue, List<string> Tenets);
