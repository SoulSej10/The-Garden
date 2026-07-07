using Garden.Core.Interfaces;
using Garden.Core.Time;

namespace Garden.Engine.Time;

public class SimulationClock : ISimulationClock
{
    private long _totalTicks;
    private bool _isRunning;
    private double _speedMultiplier = 1.0;

    public SimulationTime CurrentTime => SimulationTime.FromTick(_totalTicks);
    public long TotalTicks => _totalTicks;
    public bool IsRunning => _isRunning;
    public double SpeedMultiplier => _speedMultiplier;

    public void Start() => _isRunning = true;
    public void Pause() => _isRunning = false;
    public long AdvanceTick()
    {
        if (!_isRunning) return _totalTicks;
        _totalTicks++;
        return _totalTicks;
    }
    public void SetSpeed(double multiplier) => _speedMultiplier = Math.Max(0, multiplier);
    public void Reset()
    {
        _totalTicks = 0;
        _isRunning = false;
        _speedMultiplier = 1.0;
    }
}
