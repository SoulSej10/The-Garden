using Garden.Core.Time;

namespace Garden.Core.Interfaces;

public interface ISimulationClock
{
    SimulationTime CurrentTime { get; }
    long TotalTicks { get; }
    bool IsRunning { get; }
    double SpeedMultiplier { get; }
}
