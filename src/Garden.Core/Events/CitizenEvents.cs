using Garden.Core.Identifiers;

namespace Garden.Core.Events;

public abstract record CitizenEvent : DomainEvent
{
    public GameEntityId CitizenId { get; init; }
    public string CitizenName { get; init; } = string.Empty;
}

public record CitizenSpawnedEvent : CitizenEvent
{
    public int TileX { get; init; }
    public int TileY { get; init; }
}

public record CitizenMovedEvent : CitizenEvent
{
    public int FromX { get; init; }
    public int FromY { get; init; }
    public int ToX { get; init; }
    public int ToY { get; init; }
}

public record CitizenAteEvent : CitizenEvent
{
    public string FoodSource { get; init; } = string.Empty;
    public double Amount { get; init; }
}

public record CitizenDrankEvent : CitizenEvent
{
    public string WaterSource { get; init; } = string.Empty;
    public double Amount { get; init; }
}

public record CitizenSleptEvent : CitizenEvent;

public record CitizenRestedEvent : CitizenEvent;

public record CitizenAgedEvent : CitizenEvent
{
    public int NewAge { get; init; }
    public string LifeStage { get; init; } = string.Empty;
}

public record CitizenDiedEvent : CitizenEvent
{
    public string CauseOfDeath { get; init; } = string.Empty;
    public int AgeAtDeath { get; init; }
}

public record CitizenBecameHungryEvent : CitizenEvent
{
    public double HungerLevel { get; init; }
}

public record CitizenBecameThirstyEvent : CitizenEvent
{
    public double ThirstLevel { get; init; }
}
