namespace Garden.Core.Interfaces;

public interface IWorldGenerator
{
    int Width { get; }
    int Height { get; }
    int Seed { get; }
    bool IsGenerated { get; }
    void Generate(int width, int height, int seed);
}
