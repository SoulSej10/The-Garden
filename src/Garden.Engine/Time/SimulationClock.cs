using Garden.Core.Interfaces;
using Garden.Core.Time;

namespace Garden.Engine.Time;

public class SimulationClock : ISimulationClock
{
    private long _totalTicks;
    private bool _isRunning;
    private double _speedMultiplier = 1.0;
    private DateTime _lastTickTime = DateTime.UtcNow;

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
        _lastTickTime = DateTime.UtcNow;
        return _totalTicks;
    }

    public void SetSpeed(double multiplier)
    {
        _speedMultiplier = Math.Max(0, multiplier);
    }

    public int GetTickDelayMs()
    {
        if (_speedMultiplier <= 0) return int.MaxValue;

        // 1 tick = 1 simulation hour (see SimulationTime). At 1x, 1 real
        // second should advance 1 game hour, so a full year (8,640 hours)
        // takes ~2.4 real hours - slow enough to actually watch a
        // civilization develop, as intended.
        //
        // This previously used 1000/60 (~16.7ms, a 60fps frame interval)
        // as the base delay, which made "1x" run at 60 ticks/sec - about
        // 2.5 in-game days per real second. That's why the reported "1x"
        // reached Year 3 within a few real minutes: the base unit was a
        // rendering frame rate, not a game-time-to-real-time ratio.
        const double baseDelayMs = 1000.0;
        return (int)Math.Max(1, baseDelayMs / _speedMultiplier);
    }

    public void Reset()
    {
        _totalTicks = 0;
        _isRunning = false;
        _speedMultiplier = 1.0;
        _lastTickTime = DateTime.UtcNow;
    }
}
