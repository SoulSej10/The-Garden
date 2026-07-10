using Garden.World.Entities;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// Regression tests for the Day-8 fix (DEVELOPMENT_PLAN.md Week 2): Settlement
/// had no tier concept despite TG-650_Cities_Urbanization.md describing a
/// hamlet-to-metropolis hierarchy. Thresholds are invented (spec gives none)
/// and documented inline on Settlement.Tier.
/// </summary>
public class SettlementTierTests
{
    [Theory]
    [InlineData(1, SettlementTier.Hamlet)]
    [InlineData(9, SettlementTier.Hamlet)]
    [InlineData(10, SettlementTier.Village)]
    [InlineData(29, SettlementTier.Village)]
    [InlineData(30, SettlementTier.Town)]
    [InlineData(74, SettlementTier.Town)]
    [InlineData(75, SettlementTier.City)]
    [InlineData(149, SettlementTier.City)]
    [InlineData(150, SettlementTier.RegionalCapital)]
    [InlineData(299, SettlementTier.RegionalCapital)]
    [InlineData(300, SettlementTier.Metropolis)]
    [InlineData(10000, SettlementTier.Metropolis)]
    public void Tier_DerivesFromPopulationThresholds(int population, SettlementTier expected)
    {
        var settlement = new Settlement { Population = population };
        Assert.Equal(expected, settlement.Tier);
    }
}
