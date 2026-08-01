using Garden.Core.World;

namespace Garden.World.Entities;

public class WeatherStateData
{
    public WeatherState CurrentWeather { get; set; } = WeatherState.Clear;
    public int RemainingDuration { get; set; }
    public double Intensity { get; set; }
    public double TemperatureModifier { get; set; }
    public double WindStrength { get; set; }
    public double HumidityModifier { get; set; }
}

/// <summary>
/// One cell of WorldState.WeatherCells - a coarse grid (one cell per
/// ~32x32 tiles, see WeatherSystem) that replaces a single global weather
/// scalar with genuinely spatial weather: each cell rolls its own
/// condition independently, with a bias toward inheriting a stormy
/// condition from its upwind neighbor so fronts visibly travel across the
/// map instead of the whole world flickering between states in lockstep.
/// </summary>
public class WeatherCell
{
    public WeatherState CurrentWeather { get; set; } = WeatherState.Clear;
    public int RemainingDuration { get; set; }
    public double Intensity { get; set; }
    public double TemperatureModifier { get; set; }
    public double WindStrength { get; set; }
    public double WindDirX { get; set; } = 1;
    public double WindDirY { get; set; }
    public double HumidityModifier { get; set; }

    // Consecutive daily checks (NaturalEventsSystem, IntervalTicks=24) this
    // cell has been predominantly dry - drives Drought/Wildfire triggers.
    public int DryStreak { get; set; }
    public bool IsDroughtActive { get; set; }
}
