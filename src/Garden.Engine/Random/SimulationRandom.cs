namespace Garden.Engine.Random;

public class SimulationRandom
{
    private readonly System.Random _random;

    public SimulationRandom(int seed)
    {
        _random = new System.Random(seed);
    }

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    public int Next(int maxExclusive) => _random.Next(maxExclusive);
    public double NextDouble() => _random.NextDouble();
    public double NextDouble(double min, double max) => min + _random.NextDouble() * (max - min);
}
