using Garden.Core.World;

namespace Garden.World.Entities;

public class ClimateData
{
    public ClimateZone Zone { get; init; }
    public double BaseTemperature { get; init; }
    public double AverageRainfall { get; init; }
    public double TemperatureVariation { get; init; }
    public double VegetationPotential { get; init; }
}
