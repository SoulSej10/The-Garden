namespace Garden.Engine.Services;

/// <summary>
/// RFC-018 (specification/RFC/RFC-018-ai-narrator-api-hardening.md): the
/// seam TG-DEV-009's "AI is template-based... Designed for pluggable AI
/// provider in future" note asks for. Implementations must never invent
/// facts - they only rephrase the WorldSummary NarrationService already
/// computed deterministically. Returning null (rather than throwing) on
/// any failure/unavailability is deliberate: NarrationService always has a
/// correct template-based fallback, so this is additive polish, never a
/// dependency the rest of the system relies on.
/// </summary>
public interface IAiNarrator
{
    Task<string?> EnhanceNarrativeAsync(WorldSummary summary, CancellationToken ct = default);
}

/// <summary>
/// The default implementation whenever no AI provider is configured (every
/// environment in this project today, including CI and local dev) - always
/// returns null immediately so callers fall back to the template narrative.
/// </summary>
public class NullAiNarrator : IAiNarrator
{
    public Task<string?> EnhanceNarrativeAsync(WorldSummary summary, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);
}
