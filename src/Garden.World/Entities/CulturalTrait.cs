using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class CulturalTrait
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double Strength { get; set; } = 1.0;
    public long EstablishedTick { get; set; }
}

public static class CulturalTraitTemplates
{
    public static readonly CulturalTrait[] All =
    [
        new() { Name = "Hospitality", Description = "Welcoming strangers and travelers", Category = "Social" },
        new() { Name = "Industriousness", Description = "Value placed on hard work and productivity", Category = "Work" },
        new() { Name = "Martial Tradition", Description = "Emphasis on defense and combat readiness", Category = "Military" },
        new() { Name = "Trade Spirit", Description = "Openness to commerce and exchange", Category = "Economic" },
        new() { Name = "Artistic Expression", Description = "Value placed on art and beauty", Category = "Art" },
        new() { Name = "Knowledge Seeking", Description = "Pursuit of learning and wisdom", Category = "Knowledge" },
        new() { Name = "Nature Reverence", Description = "Deep respect for the natural world", Category = "Spiritual" },
        new() { Name = "Communal Living", Description = "Emphasis on community over individual", Category = "Social" },
        new() { Name = "Independence", Description = "Value of self-reliance and autonomy", Category = "Social" },
        new() { Name = "Innovation", Description = "Drive to create and improve", Category = "Knowledge" },
        new() { Name = "Traditionalism", Description = "Preservation of established customs", Category = "Social" },
        new() { Name = "Pacifism", Description = "Preference for peaceful resolution", Category = "Social" },
    ];
}
