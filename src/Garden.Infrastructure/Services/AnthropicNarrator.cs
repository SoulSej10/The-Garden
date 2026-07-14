using System.Net.Http.Json;
using System.Text.Json;
using Garden.Engine.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Garden.Infrastructure.Services;

/// <summary>
/// RFC-018 (specification/RFC/RFC-018-ai-narrator-api-hardening.md):
/// TG-DEV-009's "pluggable AI provider" seam, first concrete implementation.
/// Never given write access to the simulation, and never asked to invent
/// anything - the system prompt is the exact WorldStats/WorldInsight values
/// NarrationService already computed deterministically, with an explicit
/// instruction to rephrase those facts only. Returns null (never throws) on
/// any failure, so NarrationService's template fallback always applies.
/// </summary>
public class AnthropicNarrator : IAiNarrator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<AnthropicNarrator> _logger;

    public AnthropicNarrator(HttpClient httpClient, IConfiguration configuration, ILogger<AnthropicNarrator> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AI:ApiKey"] ?? string.Empty;
        _model = configuration["AI:Model"] ?? "claude-3-5-haiku-20241022";
        _logger = logger;
    }

    public async Task<string?> EnhanceNarrativeAsync(WorldSummary summary, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey)) return null;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            var facts = BuildFactSheet(summary);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = JsonContent.Create(new
            {
                model = _model,
                max_tokens = 300,
                system = "You narrate a world simulation for an observer. Use ONLY the facts given to you " +
                         "below. Never invent citizens, settlements, events, or numbers not listed. Write a " +
                         "short, readable paragraph (2-4 sentences).",
                messages = new[]
                {
                    new { role = "user", content = facts }
                }
            });

            var response = await _httpClient.SendAsync(request, timeoutCts.Token);
            if (!response.IsSuccessStatusCode) return null;

            using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token);
            var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI narration request failed; falling back to template narrative");
            return null;
        }
    }

    private static string BuildFactSheet(WorldSummary summary)
    {
        var s = summary.Statistics;
        var insights = string.Join("\n", summary.Insights.Select(i => $"- {i.Topic}: {i.Summary}"));
        return $"Year {summary.Year}, {summary.Season}, Day {summary.Day}.\n" +
               $"Citizens: {s.AliveCitizens} alive, {s.DeadCitizens} dead, {s.TotalCitizens} total.\n" +
               $"Settlements: {s.TotalSettlements}. Kingdoms: {s.TotalKingdoms}. Buildings: {s.TotalBuildings}.\n" +
               $"Active trade routes: {s.TotalTradeRoutes}. Technologies discovered: {s.TechnologiesDiscovered}.\n" +
               $"History records: {s.HistoryRecordCount}.\n" +
               $"Insights:\n{insights}";
    }
}
