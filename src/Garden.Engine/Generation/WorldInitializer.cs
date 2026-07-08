using Garden.Engine.Services;
using Garden.World.Collections;
using Microsoft.Extensions.Logging;

namespace Garden.Engine.Generation;

public class WorldInitializer
{
    private readonly WorldState _worldState;
    private readonly SimulationCoordinator _coordinator;
    private readonly ILogger<WorldInitializer> _logger;

    public WorldInitializer(
        WorldState worldState,
        SimulationCoordinator coordinator,
        ILogger<WorldInitializer> logger)
    {
        _worldState = worldState;
        _coordinator = coordinator;
        _logger = logger;
    }

    public void Initialize(int width, int height, int seed)
    {
        if (_worldState.IsInitialized)
        {
            _logger.LogWarning("World already initialized, skipping");
            return;
        }

        Reinitialize(width, height, seed);
    }

    /// <summary>
    /// Regenerates the world map unconditionally, even if one already
    /// exists - used by the dev-only world reset endpoint.
    /// </summary>
    public void Reinitialize(int width, int height, int seed)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Initializing world: {Width}x{Height}, seed={Seed}", width, height, seed);

        var generator = new Generation.WorldGenerator(seed);
        var map = generator.Generate(width, height);

        _worldState.Map = map;
        _worldState.IsInitialized = true;

        sw.Stop();
        _logger.LogInformation("World initialized in {ElapsedMs}ms ({TotalTiles} tiles)",
            sw.ElapsedMilliseconds, width * height);
    }
}
