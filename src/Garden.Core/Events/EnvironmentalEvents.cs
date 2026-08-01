using Garden.Core.Events;

namespace Garden.Core.Events;

public abstract record EnvironmentalEvent : DomainEvent
{
    public int TileX { get; init; }
    public int TileY { get; init; }
    public string Severity { get; init; } = "Normal";
}

public record RainStartedEvent : EnvironmentalEvent
{
    public int Intensity { get; init; }
    public int Duration { get; init; }
}

public record RainStoppedEvent : EnvironmentalEvent
{
    public int TotalRainfall { get; init; }
}

public record SeasonChangedEvent : EnvironmentalEvent
{
    public Time.Season PreviousSeason { get; init; }
    public Time.Season NewSeason { get; init; }
}

public record RiverExpandedEvent : EnvironmentalEvent
{
    public string RiverName { get; init; } = string.Empty;
}

public record RiverShrankEvent : EnvironmentalEvent
{
    public string RiverName { get; init; } = string.Empty;
}

public record LakeDriedEvent : EnvironmentalEvent;

public record ForestExpandedEvent : EnvironmentalEvent
{
    public int AreaExpanded { get; init; }
}

public record ForestDeclinedEvent : EnvironmentalEvent
{
    public int AreaLost { get; init; }
}

public record ResourceRegeneratedEvent : EnvironmentalEvent
{
    public string ResourceName { get; init; } = string.Empty;
    public int Amount { get; init; }
}

public record DroughtStartedEvent : EnvironmentalEvent
{
    public int SeverityLevel { get; init; }
}

public record DroughtEndedEvent : EnvironmentalEvent;

// Published by NaturalEventsSystem, driven by WeatherSystem's spatial
// weather cells (drought/flood) or WorldTile.IsVolcanic (eruption). Each
// resolves within the tick it's detected on rather than simulating
// multi-day spread/recession - a deliberate simplification, documented at
// the publish site.
public record WildfireStartedEvent : EnvironmentalEvent
{
    public int TilesBurned { get; init; }
}

public record FloodStartedEvent : EnvironmentalEvent
{
    public string? NearestSettlementName { get; init; }
}

public record VolcanicEruptionEvent : EnvironmentalEvent
{
    public bool FormedNewLand { get; init; }
}
