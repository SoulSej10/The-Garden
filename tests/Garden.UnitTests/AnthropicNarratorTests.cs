using System.Net;
using Garden.Engine.Services;
using Garden.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Garden.UnitTests;

/// <summary>
/// DEVELOPMENT_PLAN.md Week 27: Real AI Narrator per
/// specification/RFC/RFC-018-ai-narrator-api-hardening.md. AnthropicNarrator
/// must never throw and must never surface a fabricated narrative when the
/// underlying request is unavailable - NarrationService relies on null
/// meaning "use the template fallback."
/// </summary>
public class AnthropicNarratorTests
{
    private class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }

    private class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("simulated network failure");
    }

    // Minimal IConfiguration fake - AnthropicNarrator only ever reads
    // "AI:ApiKey"/"AI:Model" via the indexer, so a full ConfigurationBuilder
    // (and its extra package dependency) isn't needed for these tests.
    private class FakeConfiguration(string? apiKey) : IConfiguration
    {
        public string? this[string key]
        {
            get => key == "AI:ApiKey" ? apiKey : null;
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];
        public IChangeToken GetReloadToken() => throw new NotSupportedException();
        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
    }

    private static IConfiguration ConfigWithKey(string? key) => new FakeConfiguration(key);

    private static WorldSummary EmptySummary() => new()
    {
        Tick = 0, Year = 1, Day = 1, Season = "Spring", Narrative = "template narrative"
    };

    [Fact]
    public async Task EnhanceNarrativeAsync_ReturnsNull_WhenNoApiKeyConfigured()
    {
        var narrator = new AnthropicNarrator(new HttpClient(new StubHandler(HttpStatusCode.OK, "{}")),
            ConfigWithKey(null), NullLogger<AnthropicNarrator>.Instance);

        var result = await narrator.EnhanceNarrativeAsync(EmptySummary());

        Assert.Null(result);
    }

    [Fact]
    public async Task EnhanceNarrativeAsync_ReturnsNull_WhenRequestThrows()
    {
        var narrator = new AnthropicNarrator(new HttpClient(new ThrowingHandler()),
            ConfigWithKey("fake-key"), NullLogger<AnthropicNarrator>.Instance);

        var result = await narrator.EnhanceNarrativeAsync(EmptySummary());

        Assert.Null(result);
    }

    [Fact]
    public async Task EnhanceNarrativeAsync_ReturnsNull_WhenResponseIsNotSuccessStatus()
    {
        var narrator = new AnthropicNarrator(new HttpClient(new StubHandler(HttpStatusCode.Unauthorized, "{}")),
            ConfigWithKey("fake-key"), NullLogger<AnthropicNarrator>.Instance);

        var result = await narrator.EnhanceNarrativeAsync(EmptySummary());

        Assert.Null(result);
    }

    [Fact]
    public async Task EnhanceNarrativeAsync_ReturnsText_WhenResponseIsWellFormed()
    {
        var body = """{"content":[{"type":"text","text":"A narrated summary."}]}""";
        var narrator = new AnthropicNarrator(new HttpClient(new StubHandler(HttpStatusCode.OK, body)),
            ConfigWithKey("fake-key"), NullLogger<AnthropicNarrator>.Instance);

        var result = await narrator.EnhanceNarrativeAsync(EmptySummary());

        Assert.Equal("A narrated summary.", result);
    }
}
