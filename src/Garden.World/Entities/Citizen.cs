using Garden.Core.Identifiers;

namespace Garden.World.Entities;

public class Citizen
{
    public GameEntityId Id { get; init; } = GameEntityId.New();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public double Health { get; set; } = 100.0;
    public string Location { get; set; } = string.Empty;
    public string CurrentActivity { get; set; } = "Idle";
}
