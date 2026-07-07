using Garden.Engine.Time;
using Xunit;

namespace Garden.UnitTests;

public class SimulationClockTests
{
    [Fact]
    public void Clock_StartsNotRunning()
    {
        var clock = new SimulationClock();
        Assert.False(clock.IsRunning);
    }

    [Fact]
    public void AdvanceTick_IncrementsTotalTicks()
    {
        var clock = new SimulationClock();
        clock.Start();
        clock.AdvanceTick();
        Assert.Equal(1, clock.TotalTicks);
    }

    [Fact]
    public void AdvanceTick_DoesNothingWhenPaused()
    {
        var clock = new SimulationClock();
        clock.AdvanceTick();
        Assert.Equal(0, clock.TotalTicks);
    }

    [Fact]
    public void Pause_StopsTickAdvance()
    {
        var clock = new SimulationClock();
        clock.Start();
        clock.AdvanceTick();
        clock.Pause();
        clock.AdvanceTick();
        Assert.Equal(1, clock.TotalTicks);
    }

    [Fact]
    public void SetSpeed_AcceptsValidValues()
    {
        var clock = new SimulationClock();
        clock.SetSpeed(5.0);
        Assert.Equal(5.0, clock.SpeedMultiplier);
    }

    [Fact]
    public void SetSpeed_ClampsToZero()
    {
        var clock = new SimulationClock();
        clock.SetSpeed(-1.0);
        Assert.Equal(0, clock.SpeedMultiplier);
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var clock = new SimulationClock();
        clock.Start();
        clock.AdvanceTick();
        clock.SetSpeed(10.0);
        clock.Reset();
        Assert.Equal(0, clock.TotalTicks);
        Assert.False(clock.IsRunning);
        Assert.Equal(1.0, clock.SpeedMultiplier);
    }
}
