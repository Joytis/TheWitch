# Telemetry & Analytics for TheWitch — Framework Report

*Researched 2026-07-12. Sources: [STS2-RitsuLib](https://github.com/BAKAOLC/STS2-RitsuLib) (v0.4.41), its [telemetry-backend guide](https://github.com/BAKAOLC/STS2-RitsuLib/blob/main/docs/pages/guide/telemetry-backend.md), [BaseLib wiki](https://alchyr.github.io/BaseLib-Wiki/), [AnalyticsTelemetry (Nexus 479)](https://www.nexusmods.com/slaythespire2/mods/479).*

## TL;DR

- **BaseLib has no telemetry support.** Nothing in its wiki or feature set.
- **RitsuLib ships a full, consent-gated, phone-home telemetry framework** (`src/Telemetry/`, namespace `STS2RitsuLib.Telemetry`). Mods register as *applicants*, users grant per-request consent via an in-game prompt + settings page, and batches POST to a mod-author-owned HTTP endpoint. This is what the Hornet character mod uses (Hornet itself is closed-source; the machinery is all in RitsuLib).
- **AnalyticsTelemetry** (separate mod, BaseLib-based) is the local-only alternative: append-only NDJSON run/combat logs + in-game charts, no network.
- To adopt RitsuLib telemetry we need: the RitsuLib mod dependency, an applicant registration (~30 lines), and a **backend endpoint we host** (Cloudflare Worker is the cheapest fit).

## Landscape

| Option | Network | Dependency | Effort |
|---|---|---|---|
| RitsuLib telemetry | Phone-home (consented) | RitsuLib mod + backend we host | Medium |
| AnalyticsTelemetry mod | None (local NDJSON) | That mod (BaseLib-based) | Zero code, but data stays on player machines |
| Roll our own NDJSON logger | None | None (we already hook everything) | Small |

The rest of this doc covers the RitsuLib framework, since it is the only phone-home path.

## RitsuLib telemetry architecture

Module layout under `src/Telemetry/`: `Core/` (API, registry, envelope/batch assembly), `Consent/` (store + models), `Integration/` (first-menu consent toast, settings pages, bootstrap), `Adapters/` (HTTP JSON, PostHog), `Queue/` + `Storage/` (local queueing, retry), `Diagnostics/` (exception capture, Sentry patch), `RunHistory/` (automatic vanilla run-history capture), `Contributions/` (cross-mod data attachment).

Flow:

1. Mod registers a **`TelemetryApplicant`** (id, display name, adapter/endpoint, list of **`TelemetryRequest`s**) via `TelemetryRegistry.RegisterApplicant(...)` at startup.
2. On first main menu, `TelemetryConsentPromptCoordinator` shows a consent toast per applicant ("Allow this applicant" / "Reject this applicant"). Grants are stored per-request in `TelemetryConsentStore`; a **Telemetry settings page** lets users review/revoke anytime (auto-registered per applicant).
3. Mod code captures events through an **applicant-scoped `ITelemetryClient`** (`TelemetryApi.GetClient(applicantId)`). **Unauthorized requests are silent no-ops** — no consent checks needed at call sites.
4. Events are queued locally, assembled into batches (`TelemetryEnvelopeFactory`), and delivered by the applicant's fixed **adapter** to the applicant's own backend.

### Consent model / data categories

`TelemetryDataCategory` (flags enum) — the coarse categories users consent to:

| Category | Covers |
|---|---|
| `BasicUsage` | Session start + environment metadata (versions, platform, anonymous install id) |
| `ModInventory` | Loaded-mod list + metadata |
| `RunHistory` | Vanilla run-history payloads, untrimmed |
| `Diagnostics` | Exceptions, stack traces, runtime snapshots |
| `Custom` | Applicant-defined events/payloads |

Each `TelemetryRequest` = one user-visible consent line: `RequestId`, `Category`, `Description` (localizable via `ModSettingsText`). Built-in factories: `TelemetryRequest.BasicUsage(desc)`, `.ModInventory(desc)`, `.RunHistory(desc, sharedSubs?, captureFilter?)`, `.Diagnostics(desc, sharedSubs?)`, `.Custom(requestId, desc)`. `RunHistory` takes an optional `Func<RunEndedEvent, bool>` filter (default: every ended run).

**Contributions** (cross-mod data): other mods can register `ITelemetryContributionProvider`s. `PrivateToApplicant` providers attach only to their owner's requests; `SharedToAuthorizedSubscribers` additionally require the user to grant the specific source ("contributorModId/contributionId" subscription strings, matched per category). We can ignore this for a first pass — it exists so e.g. a stats mod can donate data to a character mod's telemetry with double consent.

### API surface (what our code would call)

```csharp
// Registration (startup, e.g. from [ModInitializer]):
TelemetryRegistry.RegisterApplicant(new TelemetryApplicant
{
    ApplicantId = "TheWitch",              // convention: same as mod id
    OwnerModId  = "TheWitch",
    DisplayName = "The Witch",
    Adapter     = new HttpJsonTelemetryAdapter("https://<our-endpoint>/v1/ingest",
                      headers: null),       // optional static headers
    Requests    =
    [
        TelemetryRequest.BasicUsage("Anonymous version/platform info."),
        TelemetryRequest.RunHistory("Full run results for Witch balance tuning.",
            captureFilter: e => /* Witch runs only */ true),
        TelemetryRequest.Custom("card_stats", "Per-card play/win-rate events."),
        TelemetryRequest.Diagnostics("Crash reports for The Witch."),
    ],
});

// Capture (anywhere, any time — no-op unless that request is granted):
var tm = TelemetryApi.GetClient("TheWitch");
tm.Capture("card_played", "card_stats",
    new Dictionary<string, object?> { ["card"] = "THEWITCH-HEX", ["upgraded"] = true });
tm.CapturePayload("run_summary", "card_stats", someJsonNode);
tm.CaptureException(ex);                    // routes under the diagnostics request
bool on = tm.IsEnabled("card_stats");       // only if we want to skip building payloads

// Manual run-history push (usually unnecessary — automatic capture covers it):
TelemetryApi.CaptureVanillaRunHistory("TheWitch", runHistoryJson);
```

`ITelemetryClient` full surface: `ApplicantId`, `IsEnabled(requestId)`, `Capture(eventName, requestId, props?)`, `CapturePayload(eventName, requestId, JsonNode payload, props?)`, `CaptureException(ex, props?)`.

### Adapters (the phone-home part)

- **`HttpJsonTelemetryAdapter(endpoint, headers?)`** — POSTs JSON batches to an absolute URL. 60s timeout, static `HttpClient`, custom headers supported (auth token etc.). Failures return `TelemetrySendResult.Fail(reason)` → the queue retries later; nothing is lost silently.
- **`PostHogTelemetryAdapter`** — PostHog batch API directly. Caveat: the project key ships inside the mod. Their own docs recommend keeping credentials server-side, so a thin proxy in front of PostHog is the sanctioned shape.
- **`ITelemetryAdapter`** is public — we could write our own (`AdapterId`, `EndpointDescription`, `SendAsync(applicant, events, ct)`).

### Wire format / backend contract

POST body (schema `ritsulib.telemetry.batch.v1`):

```json
{
  "schema": "ritsulib.telemetry.batch.v1",
  "applicant_id": "TheWitch",
  "events": [ { "...envelope: event name, timestamps, session id, versions,
                 payload: { base_payload, private_contributions,
                            shared_contributions, applicant_payload } } ]
}
```

Published contracts in-repo: `schemas/telemetry/v1/telemetry-batch.schema.json`, `telemetry-event.schema.json`, `openapi.yaml`. Backend must accept POST `/v1/ingest`, validate schema, reply `200/202 {"ok": true, "accepted": N}` or a machine-readable error (`"invalid_schema"`, …). The guide's reference targets: Cloudflare Worker, FastAPI, ASP.NET, PostHog proxy, or a plain S3/blob writer.

## What we'd need to implement

1. **Dependency**: add RitsuLib to `TheWitch.json` dependencies (Workshop ID 3747602295) and reference the [`STS2.RitsuLib` NuGet package](https://libraries.io/nuget/STS2.RitsuLib) in the csproj. Verify BaseLib + RitsuLib coexist (both are framework mods; RitsuLib even reports non-RitsuLib characters on its stats screen, so coexistence is expected — but test in-game). This makes players need **two** framework mods to run The Witch — the real cost of this path.
2. **Applicant registration** in [MainFile.cs](../TheWitchCode/MainFile.cs) (`[ModInitializer]`), per the snippet above. Localize request descriptions (consent UI supports `ModSettingsText`).
3. **Backend endpoint** we own and pay for. Cheapest: Cloudflare Worker validating `ritsulib.telemetry.batch.v1` and appending to R2/KV; or a PostHog proxy if we want dashboards for free. Schemas + OpenAPI are provided, so this is a small, well-specified service.
4. **Event design**: decide what we actually want — likely `RunHistory` (win rates per ascension) + a `Custom` "card_stats" request (card picked/played/upgraded events) to feed balance decisions, mirroring the `TESTED` workflow. Keep `BasicUsage` + `Diagnostics` for crash triage.
5. **Privacy hygiene**: consent UX is handled by RitsuLib, but we should state data collection on the Workshop page, never send user-identifying data (RitsuLib's `BasicUsage` uses an anonymous install id), and no secrets in the mod binary (auth header at most a low-value write token).

## Decision points (open)

- **Do we want phone-home at all?** For a Workshop character mod, local NDJSON (roll-our-own or AnalyticsTelemetry) covers playtest balance data with zero infra, zero extra dependency, zero privacy surface. Phone-home only pays off once strangers are playing the mod at volume.
- If yes: **self-hosted ingest vs PostHog proxy** (dashboards for free vs full control).
- **RitsuLib as hard dependency** vs optional soft-dependency (reflection-guarded registration so the mod still runs without RitsuLib installed — worth doing; Hornet-style mods require it outright).
