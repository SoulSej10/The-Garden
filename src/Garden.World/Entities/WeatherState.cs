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
