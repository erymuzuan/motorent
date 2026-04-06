# Gemini API Usage Tracking & Rate Limiting

**Date:** 2026-04-06
**Status:** Approved
**Scope:** Documentation search chat endpoint (phase 1), extensible to all Gemini services

## Problem

The MotoRent docs chat (`api/help/ask`) uses the Gemini API with no rate limiting, no usage tracking, and no cost visibility. The endpoint has no `[Authorize]` attribute — open to anonymous users. Six services total use Gemini with inconsistent API patterns (mixed API key passing, duplicate response models).

## Goals

1. Rate limit the docs chat endpoint (anonymous: 3/day, registered: 50/day + 200/week)
2. Track all AI usage with full audit trail (question, response preview, tokens, cost)
3. Provide SuperAdmin visibility via stat card + dedicated AI usage page
4. Standardize Gemini API patterns across services
5. Store estimated cost in both USD and MYR

## Non-Goals (Phase 2)

- Rate limiting other Gemini services (OCR, vehicle recognition, transliteration, template AI, translation)
- User-facing AI usage dashboard with credit purchasing
- Embedding-based semantic search (no embeddings used currently)

## Architecture

### Approach: Lightweight Logging + Rate Limiting Layer

Add `AiUsageLog` entity and `AiUsageService` for tracking and rate limiting. Wire into `DocumentationSearchService` and `HelpController` only. Other services get wired in later when the credit system is built.

```
User Request
    |
    v
HelpController
    |-- 1. Extract caller identity (userName, IP, sessionId cookie)
    |-- 2. AiUsageService.CheckRateLimitAsync() [blocking DB query]
    |-- 3. DocumentationSearchService.AskGeminiAsync() [blocking]
    |-- 4. session.SubmitChanges("DocumentationSearch") [non-blocking via RabbitMQ]
    |
    v
Response (answer + rate limit headers)

    ... async ...

AiUsageLogSubscriber
    |-- Receives AiUsageLog.Added.DocumentationSearch
    |-- Persists to Core schema DB
```

## Components

### 1. AiUsageLog Entity (Core Schema)

```csharp
public class AiUsageLog : Entity
{
    public int AiUsageLogId { get; set; }

    // Caller identity
    public string? UserName { get; set; }        // null for anonymous
    public string? AccountNo { get; set; }       // null for anonymous
    public string? IpAddress { get; set; }
    public string? SessionId { get; set; }       // GUID cookie for anonymous

    // Request details
    public string ServiceName { get; set; }      // "DocumentationSearch", "DocumentOcr", etc.
    public string Model { get; set; }            // "gemini-3.1-flash-lite-preview"
    public string? Question { get; set; }        // input text
    public string? ResponsePreview { get; set; } // first ~200 chars of response

    // Token/cost tracking
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public decimal EstimatedCostMyr { get; set; } // USD * exchange rate (default 4.5)

    // Status
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset DateTime { get; set; }

    public override int GetId() => AiUsageLogId;
    public override void SetId(int value) => AiUsageLogId = value;
}
```

### SQL Table

```sql
CREATE TABLE "AiUsageLog"
(
    "AiUsageLogId"      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "UserName"          VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'UserName')::VARCHAR(200)) STORED,
    "AccountNo"         VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'AccountNo')::VARCHAR(50)) STORED,
    "IpAddress"         VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'IpAddress')::VARCHAR(50)) STORED,
    "SessionId"         VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'SessionId')::VARCHAR(50)) STORED,
    "ServiceName"       VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'ServiceName')::VARCHAR(50)) STORED,
    "Model"             VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Model')::VARCHAR(100)) STORED,
    "Success"           BOOLEAN GENERATED ALWAYS AS (("Json"->>'Success')::BOOLEAN) STORED,
    "DateTime"          TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'DateTime')) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_AiUsageLog_DateTime ON "AiUsageLog"("DateTime");
CREATE INDEX IX_AiUsageLog_UserName_DateTime ON "AiUsageLog"("UserName", "DateTime");
CREATE INDEX IX_AiUsageLog_IpAddress_DateTime ON "AiUsageLog"("IpAddress", "DateTime");
CREATE INDEX IX_AiUsageLog_ServiceName ON "AiUsageLog"("ServiceName");
```

### CoreDataContext Addition

```csharp
public IQueryable<AiUsageLog> AiUsageLogs => CreateQuery<AiUsageLog>();
```

### 2. AiUsageService

```csharp
public class AiUsageService(CoreDataContext coreDataContext, ILogger<AiUsageService> logger)
{
    // --- Rate Limiting ---
    // Queries AiUsageLogs WHERE DateTime >= today, counts by identity
    // Anonymous (userName=null): 3/day identified by IP + SessionId
    // Registered: 50/day AND 200/week by UserName
    // Returns: RateLimitResult { Allowed, DailyUsed, DailyLimit, WeeklyUsed, WeeklyLimit }
    Task<RateLimitResult> CheckRateLimitAsync(string? userName, string? ipAddress, string? sessionId)

    // --- Cost Estimation ---
    // Hardcoded defaults, overridable via MOTO_AiModelPricing env var (JSON)
    // Default pricing:
    //   gemini-3.1-flash-lite-preview: $0.25/1M input, $1.50/1M output
    //   gemini-3-flash-preview:        $0.50/1M input, $3.00/1M output
    // MYR = USD * (ExchangeRate for USD/MYR if exists, else 4.5)
    (decimal Usd, decimal Myr) EstimateCost(string model, int inputTokens, int outputTokens)

    // --- Queries for SuperAdmin ---
    // Stats: today's query count, today's cost, this week's cost, rate-limited count
    Task<AiUsageStats> GetStatsAsync()

    // Paginated log query with filters
    Task<LoadOperation<AiUsageLog>> GetLogsAsync(
        string? userName, string? serviceName, string? model,
        DateTimeOffset? from, DateTimeOffset? to, int page, int size)
}
```

### Pricing Configuration

Default pricing hardcoded as:

| Model | Input (per 1M tokens) | Output (per 1M tokens) |
|-------|----------------------|------------------------|
| gemini-3.1-flash-lite-preview | $0.25 | $1.50 |
| gemini-3-flash-preview | $0.50 | $3.00 |

Override via env var `MOTO_AiModelPricing`:
```json
{"gemini-3.1-flash-lite-preview":{"input":0.25,"output":1.50},"gemini-3-flash-preview":{"input":0.50,"output":3.00}}
```

MYR conversion: lookup `ExchangeRate` entity for USD->MYR. If not found, use hardcoded `4.5`.

### 3. HelpController Changes

```csharp
[HttpPost("ask")]
public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
{
    // 1. Extract caller identity
    var userName = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
    var sessionId = GetOrCreateSessionCookie(); // GUID cookie, 24h expiry

    // 2. Rate limit check (blocking — must await)
    var limit = await AiUsageService.CheckRateLimitAsync(userName, ipAddress, sessionId);
    if (!limit.Allowed)
        return StatusCode(429, new { error = "Rate limit exceeded", limit.DailyUsed, limit.DailyLimit });

    // 3. Call Gemini (blocking)
    var result = await SearchService.AskGeminiAsync(request.Question, ct);

    // 4. Estimate cost
    var cost = AiUsageService.EstimateCost(result.Model, result.InputTokens, result.OutputTokens);

    // 5. Publish usage via RabbitMQ (non-blocking)
    //    Uses CoreDataContext session, routing key: AiUsageLog.Added.DocumentationSearch
    var log = new AiUsageLog
    {
        UserName = userName, IpAddress = ipAddress, SessionId = sessionId,
        ServiceName = "DocumentationSearch", Model = result.Model,
        Question = request.Question,
        ResponsePreview = result.Answer?.Length > 200 ? result.Answer[..200] : result.Answer,
        InputTokens = result.InputTokens, OutputTokens = result.OutputTokens,
        EstimatedCostUsd = cost.Usd, EstimatedCostMyr = cost.Myr,
        Success = result.Success, DateTime = DateTimeOffset.Now
    };
    using var coreSession = CoreDataContext.OpenSession("system");
    coreSession.Attach(log);
    await coreSession.SubmitChanges("DocumentationSearch");

    return Ok(new AskResponse(result.Answer));
}
```

Session cookie helper:
```csharp
private string GetOrCreateSessionCookie()
{
    const string cookieName = "mr_ai_session";
    if (Request.Cookies.TryGetValue(cookieName, out var existing))
        return existing;

    var sessionId = Guid.NewGuid().ToString("N")[..16];
    Response.Cookies.Append(cookieName, sessionId, new CookieOptions
    {
        HttpOnly = true, SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.Now.AddHours(24)
    });
    return sessionId;
}
```

### Race Condition Note

Rate limit check reads DB, but log write is async via RabbitMQ. A burst of requests could slip past the limit before the subscriber writes them. For 3/day and 50/day limits, worst case is +1-2 extra queries. Accepted as negligible.

### 4. DocumentationSearchService Changes

Return a richer result instead of plain `string`:

```csharp
public record GeminiSearchResult(
    string Answer,
    string Model,
    int InputTokens,
    int OutputTokens,
    bool Success,
    string? Error);
```

Parse `usageMetadata` from Gemini response:

```csharp
// Add to GeminiResponse in GeminiModels.cs
public class GeminiUsageMetadata
{
    [JsonPropertyName("promptTokenCount")]
    public int PromptTokenCount { get; set; }

    [JsonPropertyName("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }

    [JsonPropertyName("totalTokenCount")]
    public int TotalTokenCount { get; set; }
}
```

### 5. AiUsageLogSubscriber (RabbitMQ)

```csharp
public class AiUsageLogSubscriber : Subscriber<AiUsageLog>
{
    public override string QueueName => nameof(AiUsageLogSubscriber);
    public override string[] RoutingKeys => [$"{nameof(AiUsageLog)}.{CrudOperation.Added}.*"];

    protected override async Task ProcessMessage(AiUsageLog log, BrokeredMessage message)
    {
        // Log already persisted via SubmitChanges in controller.
        // Subscriber handles any post-write side effects (e.g., alerting on cost thresholds).
        // No retry needed — log loss is acceptable.
        message.Accept();
    }
}
```

### 6. SuperAdmin StartPage Stat Card

Add to existing `mr-summary-cards` row in `StartPage.razor`:

```html
<div class="mr-summary-card">
    <div class="mr-summary-icon info">
        <i class="ti ti-sparkles"></i>
    </div>
    <div class="mr-summary-content">
        <h4>@Localizer["StatAiQueries"]</h4>
        <div class="mr-value">@m_aiQueryCount</div>
    </div>
</div>
```

Load via `AiUsageService.GetStatsAsync()` in `OnInitializedAsync`.

### 7. SuperAdmin AI Usage Page (`/super-admin/ai-usage`)

**Route:** `/super-admin/ai-usage`
**Auth:** `[Authorize(Roles = UserAccount.SUPER_ADMIN)]`
**Layout:** 2-column (`col-3` filters, `col-9` content)

**Filters (col-3):**
- Date range: today, last 7 days, last 30 days, custom
- Service name dropdown
- Model dropdown
- User type: All / Anonymous / Registered
- Username text filter

**Content (col-9):**

**Summary row** — 4 stat cards:
- Total queries (filtered period)
- Total cost USD + MYR (filtered period)
- Unique users (filtered period)
- Rate-limited requests (filtered period)

**Cost breakdown table** — grouped by model:

| Model | Queries | Input Tokens | Output Tokens | Cost (USD) | Cost (MYR) |
|-------|---------|-------------|---------------|------------|------------|

**Query log table** — paginated, sortable:

| DateTime | User/IP | Service | Model | Question | Tokens (in/out) | Cost (USD/MYR) | Status |
|----------|---------|---------|-------|----------|-----------------|----------------|--------|

- Row click expands to show full question text and response preview
- Standard `LoadingSkeleton` loading pattern
- Localized: `.resx` files for en, th, ms

### 8. API Consistency Fixes

**API key standardization:** All 6 Gemini services use `x-goog-api-key` header instead of `?key=` query parameter. Affected files:
- `DocumentOcrService.cs` — line 51
- `VehicleRecognitionService.cs` — line 76
- `TransliterationService.cs` — line 91
- `DocumentTemplateAiService.cs` — lines 76, 206
- `DocumentationTranslationService.cs` — line 75

Extract shared helper for creating Gemini HTTP requests (similar to `DocumentationSearchService.CreateGeminiHttpRequest`).

**Consolidate Gemini response models:** Remove duplicate classes from:
- `VehicleRecognitionService.cs` (internal `GeminiApiResponse`, `GeminiApiCandidate`, `GeminiApiContent`, `GeminiApiPart`)
- `TransliterationService.cs` (private `GeminiResponse`, `Candidate`, `Content`, `Part`)

All services use the shared `GeminiResponse` from `MotoRent.Domain.Core.GeminiModels`. Add `GeminiUsageMetadata` to that file.

## Files to Create

| File | Purpose |
|------|---------|
| `database/tables/Core.AiUsageLog.sql` | SQL table definition |
| `src/MotoRent.Domain/Core/AiUsageLog.cs` | Entity class |
| `src/MotoRent.Services/Core/AiUsageService.cs` | Rate limiting, cost estimation, queries |
| `src/MotoRent.Client/Pages/SuperAdmin/AiUsage.razor` | Dedicated AI usage page |
| `src/MotoRent.Worker/Subscribers/AiUsageLogSubscriber.cs` | RabbitMQ subscriber |
| Resource files (`.resx`) for `StartPage` and `AiUsage` | en, th, ms localization |

## Files to Modify

| File | Change |
|------|--------|
| `src/MotoRent.Domain/Core/GeminiModels.cs` | Add `UsageMetadata` to response model |
| `src/MotoRent.Domain/DataContext/CoreDataContext.cs` | Add `AiUsageLogs` queryable |
| `src/MotoRent.Services/DocumentationSearchService.cs` | Return `GeminiSearchResult` with token counts |
| `src/MotoRent.Server/Controllers/HelpController.cs` | Rate limiting, caller identity, usage publishing |
| `src/MotoRent.Client/Pages/SuperAdmin/StartPage.razor` | Add AI queries stat card |
| `src/MotoRent.Services/DocumentOcrService.cs` | Fix API key to header |
| `src/MotoRent.Services/VehicleRecognitionService.cs` | Fix API key to header, use shared response models |
| `src/MotoRent.Services/TransliterationService.cs` | Fix API key to header, use shared response models |
| `src/MotoRent.Services/DocumentTemplateAiService.cs` | Fix API key to header |
| `src/MotoRent.Services/DocumentationTranslationService.cs` | Fix API key to header |
| DI registration (Program.cs or ServiceCollectionExtensions) | Register `AiUsageService`, `AiUsageLog` repository |

## Rate Limits

| User Type | Daily Limit | Weekly Limit | Identity |
|-----------|-------------|--------------|----------|
| Anonymous | 3 | - | IP address + session cookie |
| Registered | 50 | 200 | UserName |
