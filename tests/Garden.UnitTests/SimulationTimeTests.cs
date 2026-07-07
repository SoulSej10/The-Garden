using Garden.Core.Time;
using Xunit;

namespace Garden.UnitTests;

public class SimulationTimeTests
{
    [Fact]
    public void FromTick_Zero_CorrectValues()
    {
        var time = SimulationTime.FromTick(0);
        Assert.Equal(1, time.Year);
        Assert.Equal(1, time.Month);
        Assert.Equal(1, time.Day);
        Assert.Equal(0, time.Hour);
    }

    [Fact]
    public void FromYmd_CorrectTick()
    {
        var time = SimulationTime.FromYmd(1, 1, 1, 0);
        Assert.Equal(0, time.Tick);
    }

    [Fact]
    public void OneFullDay_Is24Ticks()
    {
        var time = SimulationTime.FromTick(24);
        Assert.Equal(2, time.Day);
        Assert.Equal(0, time.Hour);
    }

    [Fact]
    public void OneFullYear_Is8640Ticks()
    {
        var time = SimulationTime.FromTick(8640);
        Assert.Equal(2, time.Year);
        Assert.Equal(1, time.Month);
        Assert.Equal(1, time.Day);
    }

    [Fact]
    public void SpringIsCorrect()
    {
        var time = SimulationTime.FromYmd(1, 3, 15);
        Assert.Equal(Season.Spring, time.Season);
    }

    [Fact]
    public void WinterIsCorrect()
    {
        var time = SimulationTime.FromYmd(1, 12, 1);
        Assert.Equal(Season.Winter, time.Season);
    }
}
