using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class Settlement
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string Name { get; set; } = string.Empty;
    public int Population { get; set; }
    public string Location { get; set; } = string.Empty;
    public double FoodReserves { get; set; }
}
