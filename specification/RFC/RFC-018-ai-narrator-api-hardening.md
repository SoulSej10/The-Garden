# RFC-018: Real AI Narrator + API Hardening (Rate Limiting, Versioning)

**Status:** Implemented (Week 27, 2026-07-14 - see DEVELOPMENT_PLAN.md Days 132-136)
**Date:** 2026-07-14
**Author:** `DEVELOPMENT_PLAN.md` Week 27
**Governing spec:** `06_Development/TG-DEV-009_AI_Production_Readiness.md`

---

## Why this needs an RFC before a day-to-day plan

Unlike every other week in this cycle, Week 27 doesn't scope a new simulation mechanic against a
`TG-###` Living Document — it closes out `TG-DEV-009`'s own **Known Technical Debt** list, which
already names the three items this RFC covers verbatim: *"AI is template-based — Uses pattern-matched
responses from historical data, not an actual LLM. Designed for pluggable AI provider in future,"*
*"No rate limiting — API has no throttling middleware yet,"* and *"No API versioning — Controllers
don't have version prefixes."* This RFC is still needed because none of the three has an obvious
"smallest possible fix" — each touches either an external provider integration, cross-cutting
middleware, or every controller in the API, and needs an explicit decision on scope before touching
code that runs in production.

## Scope decision: pluggable LLM narration, built-in rate limiting, route-prefix versioning

| In scope | Deferred (needs its own increment/RFC later) |
|---|---|
| `IAiNarrator` interface + `AnthropicNarrator` (calls Anthropic's Messages API, strictly grounded in facts `NarrationService` already computes — never given write access, per `TG-DEV-009`'s "AI shall never... generate simulation logic") + `NullAiNarrator` (the default when no API key is configured, falls back to the existing template narrative) | Multiple AI provider support (OpenAI, etc.) — `IAiNarrator` is provider-agnostic by design, but only one concrete implementation ships this increment |
| ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` middleware (already part of the `Microsoft.NET.Sdk.Web` shared framework, no new package needed): a global fixed-window limiter | Per-endpoint or per-client-tier rate limits, authenticated-user-based limits — this increment applies one global policy, not a tiered scheme |
| Route-prefix API versioning (`v1/[controller]`, applied via a single `IApplicationModelConvention` rather than editing all 16 controllers individually) | `Asp.Versioning.Mvc`-style content negotiation, deprecation headers, multiple simultaneous API versions — this increment adds exactly one version prefix, not a full versioning framework, since there is only one API version that has ever existed |
| A real production bug found while implementing versioning: `nginx.conf`'s `/api/` proxy strips the prefix before forwarding, but the Observatory's production build falls back to `apiUrl = '/'` (no `VITE_API_URL` is set anywhere in `docker-compose.yml`) — meaning dockerized production API calls hit the SPA's `try_files` fallback (serving `index.html`) instead of the backend. Fixed alongside versioning, not deferred, since it's the same "does the client actually reach the right route" concern | Authentication/authorization (`TG-DEV-009` explicitly frames this as "prepare for future authentication without coupling it to the simulation" — preparation, not implementation) |
| A second, adjacent production gap found in the same file: `nginx.conf` has no proxy location for `/simulationHub` (SignalR, WebSocket upgrade) — the one hub the Observatory frontend actually connects to (`useSimulationHub.ts`). Fixed alongside the `/api/` fix, since both are "does dockerized production actually reach the backend" bugs found in the same file during the same investigation | The other four registered-but-unused hub types (`environmentHub`, `citizenHub`, `settlementHub`, `historyHub`) — the frontend doesn't connect to them at all today, so proxying them isn't yet load-bearing; adding proxy rules for hubs nothing calls would be speculative |

## Why Anthropic specifically, and why grounding matters

`TG-DEV-009`'s Development Rules are explicit: AI "shall never... generate simulation logic," and the
AI World Assistant section requires *"Answers must always reference historical records... Never
fabricate simulation facts."* This RFC's `AnthropicNarrator` therefore never lets the model invent
facts: it passes the exact same `WorldStats`/`WorldInsight` list `NarrationService.GenerateSummary()`
already computes deterministically as the system-prompt's *only* factual input, with an explicit
instruction to rephrase those facts into a more natural narrative and nothing else. If the API call
fails, times out, or no key is configured, the caller falls back to the existing template-based
narrative — the feature is additive polish on top of an already-correct deterministic summary, never a
replacement for it. Anthropic's Messages API is used because this project's own tooling context is
Claude-based, but `IAiNarrator` is the seam `TG-DEV-009`'s "pluggable AI provider" note asks for, so a
different provider is a follow-up implementation, not a redesign.

## Mechanism

1. `IAiNarrator` (new interface, `Garden.Core.Interfaces`): `Task<string?> EnhanceNarrativeAsync(WorldSummary summary, CancellationToken ct)`, returning `null` on any failure (no key, network error, non-2xx response, timeout) rather than throwing — callers always have a safe fallback.
2. `AnthropicNarrator` (new, `Garden.Infrastructure/Services`): reads `AI:ApiKey`/`AI:Model` from
   `IConfiguration` (never hardcoded, per `TG-DEV-009`), builds a single Messages API request via
   `IHttpClientFactory`, with a system prompt listing every `WorldStats`/`WorldInsight` value verbatim
   and instructing the model to narrate *only* those facts. A 5-second timeout; any exception or
   non-success status returns `null`.
3. `NullAiNarrator` (new): always returns `null` immediately. Registered when `AI:ApiKey` is absent or
   empty — the default in every environment (dev, CI, this project's own sandbox) until an operator
   configures a real key.
4. `NarrationService.GenerateSummaryAsync(CancellationToken ct)` (new, alongside the existing synchronous
   `GenerateSummary()` which remains unchanged and still computes the deterministic facts/insights):
   computes the same `WorldSummary`, then calls `IAiNarrator.EnhanceNarrativeAsync`; if it returns
   non-null, that replaces `Narrative` in the response, otherwise the template narrative is used
   unchanged.
5. `AssistantController.GetSummary` becomes `async`, calling `GenerateSummaryAsync`.
6. Rate limiting: `AddRateLimiter` with one global `FixedWindowLimiter` (invented threshold — generous
   enough not to interfere with the Observatory's own polling cadence, e.g. 300 requests/minute per
   client), `UseRateLimiter()` added to the middleware pipeline, applied globally via
   `RequireRateLimiting` on the controller base or a global filter.
7. Versioning: a new `RoutePrefixConvention : IApplicationModelConvention` prefixes every controller's
   route template with `v1`, registered once via `AddControllers(options => options.Conventions.Add(...))`
   — no individual controller file needs editing. `Garden.Observatory`'s `apiUrl` (`api.ts`) is updated
   to include the `/v1` segment (dev: `http://localhost:5088/v1`; prod: `/api/v1`, matching `nginx.conf`'s
   `/api/` proxy which strips that segment before forwarding).
8. `nginx.conf` gains a `/simulationHub` proxy location (WebSocket upgrade headers, matching the
   existing `/api/` block's pattern) alongside the `/api/` fix, closing both production-routing gaps
   found during this investigation.

## Explicitly out of scope for the next cycle

- Multiple simultaneous AI providers or provider-selection UI.
- Per-endpoint/per-tier rate limiting, authenticated-request quotas.
- Full API versioning framework (content negotiation, deprecation headers, `v2` migration tooling).
- Authentication/authorization of any kind.
- Proxying the four currently-unused SignalR hubs.

## Open questions for review before implementation starts

1. Should the rate limit apply per-IP or globally across all clients? (Recommendation: per-IP
   (`PartitionedRateLimiter` keyed on `RemoteIpAddress`) — a single global counter would let one heavy
   Observatory session starve every other client, including this project's own live-verification
   checks; per-IP is the standard default and matches how a single Observatory user's polling traffic
   should be isolated from any other client.)
2. Should `AnthropicNarrator` retry on transient failures, or fail straight to the template fallback?
   (Recommendation: fail straight to fallback — `NarrationService`'s template narrative is always
   correct and instant; retrying a non-essential polish feature would add latency to an endpoint the
   Observatory dashboard polls every 15 seconds for no real benefit.)

## Implementation notes (Week 27, added at close-out)

Shipped as specified, with both open questions resolved as recommended (per-IP partitioned rate
limiting; `AnthropicNarrator` fails straight to the template fallback with no retry).

- `IAiNarrator` (`Garden.Engine.Services`) + `NullAiNarrator` (default) + `AnthropicNarrator`
  (`Garden.Infrastructure.Services`, real HTTP call to Anthropic's Messages API, grounded strictly in
  the `WorldStats`/`WorldInsight` values `NarrationService.GenerateSummary()` already computes).
  `WorldSummary`/`WorldStats` converted to records so `GenerateSummaryAsync` could swap in an
  AI-enhanced `Narrative` via a `with` expression without duplicating every other field.
  `AssistantController.GetSummary` is now async, calling `GenerateSummaryAsync`.
- Rate limiting: ASP.NET Core's built-in `Microsoft.AspNetCore.RateLimiting` (no new package needed),
  one global `FixedWindowLimiter` partitioned per client IP (300 req/min), `429` on rejection.
- Versioning: `RoutePrefixConvention` (a single `IApplicationModelConvention`) prefixes every
  controller's route with `v1` — no individual controller file edited. Confirmed live: the old bare
  route (`/system/statistics`) now 404s, `/v1/system/statistics` works.
- **Two real production bugs were found and fixed while implementing versioning, not deferred**:
  (1) `nginx.conf`'s `/api/` proxy strips the prefix before forwarding, but `Garden.Observatory`'s
  production build had no `VITE_API_URL` set anywhere in `docker-compose.yml`, so `apiUrl` fell back to
  `/` — meaning dockerized production API calls would hit the SPA's `try_files` fallback instead of the
  backend. Fixed by changing the production fallback to `/api/v1`, matching what `nginx.conf` actually
  forwards. (2) `nginx.conf` had no proxy location for `/simulationHub` at all — the one SignalR hub the
  Observatory frontend actually connects to (`useSimulationHub.ts`) would never have reached the backend
  in a dockerized deployment. Added, mirroring the `/api/` block's WebSocket upgrade headers.
- `TG-DEV-009`'s own "Known Technical Debt" list already named exactly these three items verbatim
  (confirmed by reading the document in full) — all three struck through with resolution notes,
  following the same annotation convention already used there for the Week 1 Day 1 fix.
- Tests: 3 new `NarrationServiceTests` (template fallback preserved when no AI narrator succeeds, AI
  narrative used when it does, statistics/insights identical regardless) + 4 new
  `AnthropicNarratorTests` (no key configured, network failure, non-success status, well-formed
  response) — all against a stubbed `HttpMessageHandler`, no real network call made in tests. Required
  adding a `Garden.Infrastructure` project reference to `Garden.UnitTests` (previously absent). 257
  total (up from 250).
- Live verification: confirmed the versioned route works and the unversioned route 404s; confirmed the
  rate limiter rejects with `429` after 300 requests in a minute (issued 310 requests directly, got 288
  `200`s and 22 `429`s); confirmed `/v1/assistant/summary` returns the correct template-based narrative
  (no `AI:ApiKey` configured in this environment, as expected — every environment this project runs in
  today uses `NullAiNarrator`); confirmed the Observatory frontend (Dashboard, Civilization pages) still
  loads and renders real data through the new `/v1` routes with no console errors. Full verification:
  build clean, 257/257 unit tests, 3/3 fast integration tests, `tsc --noEmit` clean.
