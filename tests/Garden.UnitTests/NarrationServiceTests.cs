using Garden.Engine.Services;
using Garden.World.Collections;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 27: Real AI Narrator per
/// specification/RFC/RFC-018-ai-narrator-api-hardening.md. NarrationService's
/// deterministic template narrative must always be the fallback - these tests
/// confirm GenerateSummaryAsync behaves correctly whether or not an
/// IAiNarrator successfully enhances the narrative.
/// </summary>
public class NarrationServiceTests
{
    private class StubAiNarrator(string? result) : IAiNarrator
    {
        public Task<string?> EnhanceNarrativeAsync(WorldSummary summary, CancellationToken ct = default) =>
            Task.FromResult(result);
    }

    private static NarrationService CreateHarness(IAiNarrator narrator)
    {
        var world = new WorldState();
        var archive = new HistoricalArchive();
        return new NarrationService(world, archive, narrator, NullLogger<NarrationService>.Instance);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesTemplateNarrative_WhenAiNarratorReturnsNull()
    {
        var service = CreateHarness(new NullAiNarrator());

        var templateSummary = service.GenerateSummary();
        var asyncSummary = await service.GenerateSummaryAsync();

        Assert.Equal(templateSummary.Narrative, asyncSummary.Narrative);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesAiNarrative_WhenAiNarratorSucceeds()
    {
        var service = CreateHarness(new StubAiNarrator("A hand-crafted AI narrative."));

        var summary = await service.GenerateSummaryAsync();

        Assert.Equal("A hand-crafted AI narrative.", summary.Narrative);
    }

    [Fact]
    public async Task GenerateSummaryAsync_PreservesStatisticsAndInsights_RegardlessOfAiNarrator()
    {
        var withAi = await CreateHarness(new StubAiNarrator("Something else entirely.")).GenerateSummaryAsync();
        var withoutAi = await CreateHarness(new NullAiNarrator()).GenerateSummaryAsync();

        Assert.Equal(withoutAi.Statistics, withAi.Statistics);
        Assert.Equal(withoutAi.Insights, withAi.Insights);
    }
}
