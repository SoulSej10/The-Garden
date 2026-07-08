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
    public void GetTickDelayMs_AtDefaultSpeed_IsOneRealSecondPerGameHour()
    {
        // Regression guard: this previously used a 60fps frame interval
        // (~16.7ms) as the base delay, making "1x" advance ~2.5 in-game
        // days per real second - a full year in a few real minutes instead
        // of the ~2.4 real hours a believable "1x" pace should take.
        var clock = new SimulationClock();
        clock.SetSpeed(1.0);
        Assert.Equal(1000, clock.GetTickDelayMs());
    }

    [Fact]
    public void GetTickDelayMs_ScalesInverselyWithSpeed()
    {
        var clock = new SimulationClock();
        clock.SetSpeed(1000.0);
        Assert.Equal(1, clock.GetTickDelayMs());
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
