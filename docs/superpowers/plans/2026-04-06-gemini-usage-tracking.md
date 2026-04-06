# Gemini API Usage Tracking & Rate Limiting Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add rate limiting to the docs chat endpoint and track all Gemini API usage with cost estimation, visible to SuperAdmin.

**Architecture:** New `AiUsageLog` Core entity + `AiUsageService` for rate limiting/cost tracking. Wire into `HelpController` only (phase 1). SuperAdmin gets a stat card on StartPage and a dedicated `/super-admin/ai-usage` page. Non-blocking log write via RabbitMQ `SubmitChanges`.

**Tech Stack:** C# / .NET 10, PostgreSQL JSONB, Blazor Server, Tabler CSS, RabbitMQ

**Spec:** `docs/superpowers/specs/2026-04-06-gemini-usage-tracking-design.md`

---

### Task 1: AiUsageLog Entity & SQL Table

**Files:**
- Create: `src/MotoRent.Domain/Core/AiUsageLog.cs`
- Create: `database/tables/Core.AiUsageLog.sql`

- [ ] **Step 1: Create the AiUsageLog entity**

Create `src/MotoRent.Domain/Core/AiUsageLog.cs`:

```csharp
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

public class AiUsageLog : Entity
{
    public int AiUsageLogId { get; set; }

    // Caller identity
    public string? UserName { get; set; }
    public string? AccountNo { get; set; }
    public string? IpAddress { get; set; }
    public string? SessionId { get; set; }

    // Request details
    public string ServiceName { get; set; } = "";
    public string Model { get; set; } = "";
    public string? Question { get; set; }
    public string? ResponsePreview { get; set; }

    // Token/cost tracking
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public decimal EstimatedCostMyr { get; set; }

    // Status
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset DateTime { get; set; } = DateTimeOffset.Now;

    public override int GetId() => AiUsageLogId;
    public override void SetId(int value) => AiUsageLogId = value;
}
```

- [ ] **Step 2: Create the SQL table**

Create `database/tables/Core.AiUsageLog.sql`:

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

- [ ] **Step 3: Register repository and add to CoreDataContext**

In `src/MotoRent.PostgreSqlRepository/ServiceCollectionExtensions.cs`, add after the `Feedback` repository line (~line 121):

```csharp
services.AddScoped<IRepository<AiUsageLog>, CorePgJsonRepository<AiUsageLog>>();
```

Add the `using` at the top if not already present:
```csharp
using MotoRent.Domain.Core;
```

In `src/MotoRent.Domain/DataContext/CoreDataContext.cs`, add a new queryable property after `Feedbacks`:

```csharp
public IQueryable<AiUsageLog> AiUsageLogs => CreateQuery<AiUsageLog>();
```

- [ ] **Step 4: Build to verify compilation**

Run: `dotnet build`
Expected: Build succeeds with no errors related to `AiUsageLog`.

- [ ] **Step 5: Commit**

```bash
git add src/MotoRent.Domain/Core/AiUsageLog.cs database/tables/Core.AiUsageLog.sql src/MotoRent.PostgreSqlRepository/ServiceCollectionExtensions.cs src/MotoRent.Domain/DataContext/CoreDataContext.cs
git commit -m "feat: add AiUsageLog entity, SQL table, and repository registration"
```

---

### Task 2: Update GeminiModels with UsageMetadata

**Files:**
- Modify: `src/MotoRent.Domain/Core/GeminiModels.cs`

- [ ] **Step 1: Add UsageMetadata to GeminiResponse**

In `src/MotoRent.Domain/Core/GeminiModels.cs`, add `UsageMetadata` property to `GeminiResponse` and the new class:

```csharp
using System.Text.Json.Serialization;

namespace MotoRent.Domain.Core;

public class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }

    [JsonPropertyName("usageMetadata")]
    public GeminiUsageMetadata? UsageMetadata { get; set; }
}

public class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

public class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart>? Parts { get; set; }
}

public class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

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

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/MotoRent.Domain/Core/GeminiModels.cs
git commit -m "feat: add GeminiUsageMetadata to shared response model"
```

---

### Task 3: AiUsageService — Rate Limiting & Cost Estimation

**Files:**
- Create: `src/MotoRent.Services/Core/AiUsageService.cs`
- Modify: `src/MotoRent.Server/Program.cs`

- [ ] **Step 1: Create AiUsageService**

Create `src/MotoRent.Services/Core/AiUsageService.cs`:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.Core;

public class AiUsageService
{
    private readonly CoreDataContext m_context;
    private readonly ILogger<AiUsageService> m_logger;

    private static readonly Dictionary<string, (decimal InputPerMillion, decimal OutputPerMillion)> s_defaultPricing = new()
    {
        ["gemini-3.1-flash-lite-preview"] = (0.25m, 1.50m),
        ["gemini-3-flash-preview"] = (0.50m, 3.00m),
    };

    private static readonly decimal s_defaultMyrRate = 4.5m;

    public AiUsageService(CoreDataContext context, ILogger<AiUsageService> logger)
    {
        m_context = context;
        m_logger = logger;
    }

    #region Rate Limiting

    public async Task<RateLimitResult> CheckRateLimitAsync(string? userName, string? ipAddress, string? sessionId)
    {
        var now = DateTimeOffset.Now;
        var startOfDay = new DateTimeOffset(now.Date, now.Offset);
        var startOfWeek = startOfDay.AddDays(-(int)now.DayOfWeek);

        if (!string.IsNullOrEmpty(userName))
        {
            return await CheckRegisteredLimitAsync(userName, startOfDay, startOfWeek);
        }

        return await CheckAnonymousLimitAsync(ipAddress, sessionId, startOfDay);
    }

    private async Task<RateLimitResult> CheckRegisteredLimitAsync(
        string userName, DateTimeOffset startOfDay, DateTimeOffset startOfWeek)
    {
        const int dailyLimit = 50;
        const int weeklyLimit = 200;

        var dailyQuery = m_context.AiUsageLogs
            .Where(x => x.UserName == userName && x.DateTime >= startOfDay);
        var dailyUsed = await m_context.GetCountAsync(dailyQuery);

        var weeklyQuery = m_context.AiUsageLogs
            .Where(x => x.UserName == userName && x.DateTime >= startOfWeek);
        var weeklyUsed = await m_context.GetCountAsync(weeklyQuery);

        return new RateLimitResult(
            Allowed: dailyUsed < dailyLimit && weeklyUsed < weeklyLimit,
            DailyUsed: dailyUsed,
            DailyLimit: dailyLimit,
            WeeklyUsed: weeklyUsed,
            WeeklyLimit: weeklyLimit);
    }

    private async Task<RateLimitResult> CheckAnonymousLimitAsync(
        string? ipAddress, string? sessionId, DateTimeOffset startOfDay)
    {
        const int dailyLimit = 3;

        // Count by IP address for anonymous users
        var query = m_context.AiUsageLogs
            .Where(x => x.UserName == null && x.DateTime >= startOfDay);

        if (!string.IsNullOrEmpty(ipAddress))
        {
            query = query.Where(x => x.IpAddress == ipAddress);
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(x => x.SessionId == sessionId);
        }
        else
        {
            // No identity available — deny
            return new RateLimitResult(false, 0, dailyLimit, 0, 0);
        }

        var dailyUsed = await m_context.GetCountAsync(query);

        return new RateLimitResult(
            Allowed: dailyUsed < dailyLimit,
            DailyUsed: dailyUsed,
            DailyLimit: dailyLimit,
            WeeklyUsed: 0,
            WeeklyLimit: 0);
    }

    #endregion

    #region Cost Estimation

    public (decimal Usd, decimal Myr) EstimateCost(string model, int inputTokens, int outputTokens)
    {
        var pricing = GetPricing();
        if (!pricing.TryGetValue(model, out var rates))
        {
            // Unknown model — use flash-lite pricing as fallback
            rates = s_defaultPricing["gemini-3.1-flash-lite-preview"];
        }

        var usd = (inputTokens * rates.InputPerMillion / 1_000_000m)
                + (outputTokens * rates.OutputPerMillion / 1_000_000m);
        var myr = usd * GetMyrRate();

        return (Math.Round(usd, 6), Math.Round(myr, 6));
    }

    private static Dictionary<string, (decimal InputPerMillion, decimal OutputPerMillion)> GetPricing()
    {
        var envPricing = MotoConfig.GetEnvironmentVariable("AiModelPricing");
        if (string.IsNullOrEmpty(envPricing))
        {
            return s_defaultPricing;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, ModelPricing>>(envPricing);
            if (parsed is not null)
            {
                var result = new Dictionary<string, (decimal, decimal)>();
                foreach (var (key, value) in parsed)
                {
                    result[key] = (value.Input, value.Output);
                }
                return result;
            }
        }
        catch
        {
            // Fall through to defaults
        }

        return s_defaultPricing;
    }

    private static decimal GetMyrRate()
    {
        // TODO phase 2: look up ExchangeRate entity for USD->MYR
        return s_defaultMyrRate;
    }

    #endregion

    #region Queries for SuperAdmin

    public async Task<AiUsageStats> GetStatsAsync()
    {
        var now = DateTimeOffset.Now;
        var startOfDay = new DateTimeOffset(now.Date, now.Offset);
        var startOfWeek = startOfDay.AddDays(-(int)now.DayOfWeek);

        var todayQuery = m_context.AiUsageLogs.Where(x => x.DateTime >= startOfDay);
        var todayCount = await m_context.GetCountAsync(todayQuery);

        var weekQuery = m_context.AiUsageLogs.Where(x => x.DateTime >= startOfWeek);
        var weekCount = await m_context.GetCountAsync(weekQuery);

        // Load today's logs to sum costs (token counts are in JSON, not computed columns)
        var todayLogs = await m_context.LoadAsync(todayQuery, 1, 1000, includeTotalRows: false);
        var todayCostUsd = todayLogs.ItemCollection.Sum(x => x.EstimatedCostUsd);
        var todayCostMyr = todayLogs.ItemCollection.Sum(x => x.EstimatedCostMyr);

        var weekLogs = await m_context.LoadAsync(weekQuery, 1, 1000, includeTotalRows: false);
        var weekCostUsd = weekLogs.ItemCollection.Sum(x => x.EstimatedCostUsd);
        var weekCostMyr = weekLogs.ItemCollection.Sum(x => x.EstimatedCostMyr);

        return new AiUsageStats(
            todayCount, weekCount,
            todayCostUsd, todayCostMyr,
            weekCostUsd, weekCostMyr);
    }

    public async Task<LoadOperation<AiUsageLog>> GetLogsAsync(AiUsageFilter filter, int page = 1, int size = 20)
    {
        var query = BuildQuery(filter);
        query = query.OrderByDescending(x => x.AiUsageLogId);
        return await m_context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    public async Task<List<AiUsageModelBreakdown>> GetModelBreakdownAsync(AiUsageFilter filter)
    {
        var query = BuildQuery(filter);
        var lo = await m_context.LoadAsync(query, 1, 5000, includeTotalRows: false);
        var logs = lo.ItemCollection;

        return logs
            .GroupBy(x => x.Model)
            .Select(g => new AiUsageModelBreakdown(
                g.Key,
                g.Count(),
                g.Sum(x => x.InputTokens),
                g.Sum(x => x.OutputTokens),
                g.Sum(x => x.EstimatedCostUsd),
                g.Sum(x => x.EstimatedCostMyr)))
            .OrderByDescending(x => x.Queries)
            .ToList();
    }

    private IQueryable<AiUsageLog> BuildQuery(AiUsageFilter filter)
    {
        var query = m_context.AiUsageLogs.AsQueryable();

        if (!string.IsNullOrEmpty(filter.UserName))
            query = query.Where(x => x.UserName != null && x.UserName.Contains(filter.UserName));

        if (!string.IsNullOrEmpty(filter.ServiceName))
            query = query.Where(x => x.ServiceName == filter.ServiceName);

        if (!string.IsNullOrEmpty(filter.Model))
            query = query.Where(x => x.Model == filter.Model);

        if (filter.UserType == AiUserType.Anonymous)
            query = query.Where(x => x.UserName == null);
        else if (filter.UserType == AiUserType.Registered)
            query = query.Where(x => x.UserName != null);

        if (filter.From.HasValue)
            query = query.Where(x => x.DateTime >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(x => x.DateTime <= filter.To.Value);

        return query;
    }

    #endregion
}

#region DTOs

public record RateLimitResult(
    bool Allowed,
    int DailyUsed,
    int DailyLimit,
    int WeeklyUsed,
    int WeeklyLimit);

public record AiUsageStats(
    int TodayCount,
    int WeekCount,
    decimal TodayCostUsd,
    decimal TodayCostMyr,
    decimal WeekCostUsd,
    decimal WeekCostMyr);

public record AiUsageModelBreakdown(
    string Model,
    int Queries,
    long InputTokens,
    long OutputTokens,
    decimal CostUsd,
    decimal CostMyr);

public record AiUsageFilter(
    string? UserName = null,
    string? ServiceName = null,
    string? Model = null,
    AiUserType UserType = AiUserType.All,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null);

public enum AiUserType { All, Anonymous, Registered }

internal class ModelPricing
{
    public decimal Input { get; set; }
    public decimal Output { get; set; }
}

#endregion
```

- [ ] **Step 2: Register AiUsageService in DI**

In `src/MotoRent.Server/Program.cs`, add after the existing service registrations (around line 93, after `TransliterationService`):

```csharp
builder.Services.AddScoped<AiUsageService>();
```

Add the using at the top:
```csharp
using MotoRent.Services.Core;
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/MotoRent.Services/Core/AiUsageService.cs src/MotoRent.Server/Program.cs
git commit -m "feat: add AiUsageService with rate limiting, cost estimation, and query methods"
```

---

### Task 4: Update DocumentationSearchService to Return Token Metadata

**Files:**
- Modify: `src/MotoRent.Services/DocumentationSearchService.cs`

- [ ] **Step 1: Add GeminiSearchResult record**

Add at the bottom of `src/MotoRent.Services/DocumentationSearchService.cs` (outside the class, inside the namespace):

```csharp
public record GeminiSearchResult(
    string Answer,
    string Model,
    int InputTokens,
    int OutputTokens,
    bool Success,
    string? Error);
```

- [ ] **Step 2: Change AskGeminiAsync return type and parse token metadata**

Replace the `AskGeminiAsync` method signature and body. Change `public async Task<string>` to `public async Task<GeminiSearchResult>`.

Update the method to:
1. Return `GeminiSearchResult` instead of `string`
2. Parse `UsageMetadata` from the response
3. Track which model succeeded

Replace the full method:

```csharp
public async Task<GeminiSearchResult> AskGeminiAsync(string question, CancellationToken cancellationToken = default)
{
    var apiKey = MotoConfig.GeminiApiKey;

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        return new GeminiSearchResult("Gemini API key is not configured.", "", 0, 0, false, "No API key");
    }

    var context = await this.GetDocumentationContextAsync();

    if (string.IsNullOrWhiteSpace(context))
    {
        return new GeminiSearchResult(
            "I'm sorry, I couldn't find information about that in our guides.",
            "", 0, 0, false, "No documentation context");
    }

    var request = CreateGeminiRequest(context, question);
    var client = this.HttpClientFactory.CreateClient("Gemini");
    Exception? lastException = null;

    foreach (var model in MotoConfig.GeminiModels)
    {
        try
        {
            using var httpRequest = CreateGeminiHttpRequest(apiKey, model, request);
            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.ReadContentAsStringAsync(false);

            if (!response.IsSuccessStatusCode)
            {
                lastException = new HttpRequestException(
                    $"Gemini model {model} returned HTTP {(int)response.StatusCode}: {responseBody}");
                this.Logger.LogWarning(
                    "Gemini request failed for model {Model} with HTTP {StatusCode}",
                    model,
                    (int)response.StatusCode);
                continue;
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
            var answer = geminiResponse?.Candidates?
                .FirstOrDefault()?
                .Content?
                .Parts?
                .Select(x => x.Text)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

            if (!string.IsNullOrWhiteSpace(answer))
            {
                var usage = geminiResponse?.UsageMetadata;
                return new GeminiSearchResult(
                    answer,
                    model,
                    usage?.PromptTokenCount ?? 0,
                    usage?.CandidatesTokenCount ?? 0,
                    true,
                    null);
            }

            this.Logger.LogWarning("Gemini returned an empty answer for model {Model}", model);
        }
        catch (HttpRequestException ex)
        {
            lastException = ex;
            this.Logger.LogWarning(ex, "Gemini request failed for model {Model}", model);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            lastException = ex;
            this.Logger.LogWarning(ex, "Gemini request timed out for model {Model}", model);
        }
        catch (JsonException ex)
        {
            lastException = ex;
            this.Logger.LogWarning(ex, "Failed to parse Gemini response for model {Model}", model);
        }
    }

    this.Logger.LogError(lastException, "Failed to get response from Gemini API for documentation search");
    return new GeminiSearchResult(
        "An error occurred while communicating with the AI assistant.",
        MotoConfig.GeminiModels.FirstOrDefault() ?? "",
        0, 0, false, lastException?.Message);
}
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build fails in `HelpController.cs` because it still expects `string` return. This is expected — we fix it in Task 5.

- [ ] **Step 4: Commit**

```bash
git add src/MotoRent.Services/DocumentationSearchService.cs
git commit -m "feat: return GeminiSearchResult with token metadata from DocumentationSearchService"
```

---

### Task 5: Update HelpController with Rate Limiting & Usage Logging

**Files:**
- Modify: `src/MotoRent.Server/Controllers/HelpController.cs`

- [ ] **Step 1: Update HelpController**

Replace the full file `src/MotoRent.Server/Controllers/HelpController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Services;
using MotoRent.Services.Core;

namespace MotoRent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelpController(
    DocumentationSearchService searchService,
    DocumentationTranslationService translationService,
    AiUsageService aiUsageService,
    CoreDataContext coreDataContext) : ControllerBase
{
    private DocumentationSearchService SearchService { get; } = searchService;
    private DocumentationTranslationService TranslationService { get; } = translationService;
    private AiUsageService AiUsageService { get; } = aiUsageService;
    private CoreDataContext CoreDataContext { get; } = coreDataContext;

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return this.BadRequest("Question cannot be empty.");
        }

        // 1. Extract caller identity
        var userName = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var sessionId = GetOrCreateSessionCookie();

        // 2. Rate limit check (blocking)
        var limit = await this.AiUsageService.CheckRateLimitAsync(userName, ipAddress, sessionId);
        if (!limit.Allowed)
        {
            return this.StatusCode(429, new
            {
                error = "Rate limit exceeded",
                dailyUsed = limit.DailyUsed,
                dailyLimit = limit.DailyLimit,
                weeklyUsed = limit.WeeklyUsed,
                weeklyLimit = limit.WeeklyLimit
            });
        }

        // 3. Call Gemini
        var result = await this.SearchService.AskGeminiAsync(request.Question, cancellationToken);

        // 4. Estimate cost
        var cost = this.AiUsageService.EstimateCost(result.Model, result.InputTokens, result.OutputTokens);

        // 5. Log usage (non-blocking via SubmitChanges -> RabbitMQ)
        var log = new AiUsageLog
        {
            UserName = userName,
            IpAddress = ipAddress,
            SessionId = sessionId,
            ServiceName = "DocumentationSearch",
            Model = result.Model,
            Question = request.Question,
            ResponsePreview = result.Answer.Length > 200 ? result.Answer[..200] : result.Answer,
            InputTokens = result.InputTokens,
            OutputTokens = result.OutputTokens,
            EstimatedCostUsd = cost.Usd,
            EstimatedCostMyr = cost.Myr,
            Success = result.Success,
            Error = result.Error,
            DateTime = DateTimeOffset.Now
        };

        using var session = this.CoreDataContext.OpenSession("system");
        session.Attach(log);
        await session.SubmitChanges("DocumentationSearch");

        return this.Ok(new AskResponse(result.Answer));
    }

    /// <summary>
    /// Gets translation status for all documentation files.
    /// </summary>
    [HttpGet("translations/status")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> GetTranslationStatus(CancellationToken cancellationToken)
    {
        var statuses = await this.TranslationService.GetTranslationStatusAsync(cancellationToken);
        return this.Ok(statuses);
    }

    /// <summary>
    /// Translates a specific document to Thai.
    /// </summary>
    [HttpPost("translations/translate")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> TranslateDocument([FromQuery] string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return this.BadRequest("fileName is required.");
        }

        var result = await this.TranslationService.TranslateDocumentAsync(fileName, cancellationToken);

        if (!result.Success)
        {
            return this.BadRequest(new { error = result.Error });
        }

        return this.Ok(new { success = true, message = $"Successfully translated {fileName} to Thai." });
    }

    /// <summary>
    /// Translates all pending documents to Thai.
    /// </summary>
    [HttpPost("translations/translate-all")]
    [Authorize(Roles = "administrator")]
    public async Task<IActionResult> TranslateAllPending(CancellationToken cancellationToken)
    {
        var result = await this.TranslationService.TranslateAllPendingAsync(cancellationToken);
        return this.Ok(new
        {
            successCount = result.SuccessCount,
            failureCount = result.FailureCount,
            results = result.Results.Select(r => new { r.FileName, r.Success, r.Error })
        });
    }

    private string GetOrCreateSessionCookie()
    {
        const string cookieName = "mr_ai_session";
        if (Request.Cookies.TryGetValue(cookieName, out var existing))
            return existing;

        var sessionId = Guid.NewGuid().ToString("N")[..16];
        Response.Cookies.Append(cookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.Now.AddHours(24)
        });
        return sessionId;
    }

    public sealed record AskRequest(string Question);

    public sealed record AskResponse(string Answer);
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/MotoRent.Server/Controllers/HelpController.cs
git commit -m "feat: add rate limiting and AI usage logging to HelpController"
```

---

### Task 6: SuperAdmin StartPage Stat Card

**Files:**
- Modify: `src/MotoRent.Client/Pages/SuperAdmin/StartPage.razor`

- [ ] **Step 1: Add AiUsageService injection and field**

In `StartPage.razor`, add at the top (after the existing `@inject` lines):

```csharp
@inject AiUsageService AiUsageService
```

Add the using:
```csharp
@using MotoRent.Services.Core
```

- [ ] **Step 2: Add the stat card**

After the existing "StatNewFeedback" summary card (before the closing `</div>` of `mr-summary-cards`), add:

```html
<div class="mr-summary-card">
    <a href="/super-admin/ai-usage" class="text-decoration-none text-reset">
        <div class="mr-summary-icon info">
            <i class="ti ti-sparkles"></i>
        </div>
        <div class="mr-summary-content">
            <h4>@Localizer["StatAiQueries"]</h4>
            <div class="mr-value">@m_aiQueryCount</div>
        </div>
    </a>
</div>
```

- [ ] **Step 3: Add the field and loading logic**

In the `@code` block, add the field:

```csharp
private int m_aiQueryCount;
```

At the end of `OnInitializedAsync`, add:

```csharp
// Count today's AI queries
var aiStats = await this.AiUsageService.GetStatsAsync();
this.m_aiQueryCount = aiStats.TodayCount;
```

- [ ] **Step 4: Add localization key**

Add `StatAiQueries` to the StartPage resource files. The value in each locale:
- Default/en: `AI Queries (Today)`
- th: `คำถาม AI (วันนี้)`
- ms: `Pertanyaan AI (Hari Ini)`

Find existing StartPage `.resx` files and add the entry. They are at:
- `src/MotoRent.Client/Resources/Pages/SuperAdmin/StartPage.resx`
- `src/MotoRent.Client/Resources/Pages/SuperAdmin/StartPage.en.resx`
- `src/MotoRent.Client/Resources/Pages/SuperAdmin/StartPage.th.resx`
- `src/MotoRent.Client/Resources/Pages/SuperAdmin/StartPage.ms.resx`

Add to each:
```xml
<data name="StatAiQueries" xml:space="preserve">
    <value>AI Queries (Today)</value>
</data>
```

(Use the Thai/Malay translations above for the respective locale files.)

- [ ] **Step 5: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add src/MotoRent.Client/Pages/SuperAdmin/StartPage.razor src/MotoRent.Client/Resources/Pages/SuperAdmin/StartPage*.resx
git commit -m "feat: add AI Queries stat card to SuperAdmin StartPage"
```

---

### Task 7: SuperAdmin AI Usage Page

**Files:**
- Create: `src/MotoRent.Client/Pages/SuperAdmin/AiUsage.razor`
- Create: `src/MotoRent.Client/Resources/Pages/SuperAdmin/AiUsage.resx`
- Create: `src/MotoRent.Client/Resources/Pages/SuperAdmin/AiUsage.en.resx`
- Create: `src/MotoRent.Client/Resources/Pages/SuperAdmin/AiUsage.th.resx`
- Create: `src/MotoRent.Client/Resources/Pages/SuperAdmin/AiUsage.ms.resx`

- [ ] **Step 1: Create the AiUsage.razor page**

Create `src/MotoRent.Client/Pages/SuperAdmin/AiUsage.razor`:

```razor
@page "/super-admin/ai-usage"
@rendermode InteractiveServer
@inherits LocalizedComponentBase<AiUsage>
@using Microsoft.AspNetCore.Authorization
@using MotoRent.Domain.Core
@using MotoRent.Services.Core
@attribute [Authorize(Roles = UserAccount.SUPER_ADMIN)]
@inject AiUsageService AiUsageService

<PageTitle>@Localizer["PageTitle"]</PageTitle>

<div class="container-xl mt-4">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <div>
            <h4 class="mb-1">@Localizer["HeaderTitle"]</h4>
            <p class="text-secondary mb-0">@Localizer["HeaderSubtitle"]</p>
        </div>
    </div>

    @* Stats Cards *@
    <div class="row row-deck row-cards mb-3">
        <div class="col-sm-6 col-lg-3">
            <div class="card card-sm">
                <div class="card-body">
                    <div class="row align-items-center">
                        <div class="col-auto">
                            <span class="bg-primary text-white avatar"><i class="ti ti-sparkles"></i></span>
                        </div>
                        <div class="col">
                            <div class="font-weight-medium">@m_stats.TodayCount</div>
                            <div class="text-secondary">@Localizer["StatToday"]</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-sm-6 col-lg-3">
            <div class="card card-sm">
                <div class="card-body">
                    <div class="row align-items-center">
                        <div class="col-auto">
                            <span class="bg-info text-white avatar"><i class="ti ti-calendar-week"></i></span>
                        </div>
                        <div class="col">
                            <div class="font-weight-medium">@m_stats.WeekCount</div>
                            <div class="text-secondary">@Localizer["StatThisWeek"]</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-sm-6 col-lg-3">
            <div class="card card-sm">
                <div class="card-body">
                    <div class="row align-items-center">
                        <div class="col-auto">
                            <span class="bg-warning text-white avatar"><i class="ti ti-currency-dollar"></i></span>
                        </div>
                        <div class="col">
                            <div class="font-weight-medium">$@m_stats.TodayCostUsd.ToString("F4")</div>
                            <div class="text-secondary">@Localizer["StatCostToday"] (RM @m_stats.TodayCostMyr.ToString("F2"))</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class="col-sm-6 col-lg-3">
            <div class="card card-sm">
                <div class="card-body">
                    <div class="row align-items-center">
                        <div class="col-auto">
                            <span class="bg-success text-white avatar"><i class="ti ti-report-money"></i></span>
                        </div>
                        <div class="col">
                            <div class="font-weight-medium">$@m_stats.WeekCostUsd.ToString("F4")</div>
                            <div class="text-secondary">@Localizer["StatCostWeek"] (RM @m_stats.WeekCostMyr.ToString("F2"))</div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="row">
        @* Filters *@
        <div class="col-md-3">
            <div class="card mb-3">
                <div class="card-header"><h3 class="card-title">@Localizer["Filters"]</h3></div>
                <div class="card-body">
                    <div class="mb-3">
                        <label class="form-label">@Localizer["DateRange"]</label>
                        <select class="form-select" @bind="m_dateRange" @bind:after="LoadAsync">
                            <option value="today">@Localizer["Today"]</option>
                            <option value="week">@Localizer["Last7Days"]</option>
                            <option value="month">@Localizer["Last30Days"]</option>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">@Localizer["Service"]</label>
                        <select class="form-select" @bind="m_serviceFilter" @bind:after="LoadAsync">
                            <option value="">@Localizer["All"]</option>
                            <option value="DocumentationSearch">Documentation Search</option>
                            <option value="DocumentOcr">Document OCR</option>
                            <option value="VehicleRecognition">Vehicle Recognition</option>
                            <option value="Transliteration">Transliteration</option>
                            <option value="DocumentTemplateAi">Template AI</option>
                            <option value="DocumentationTranslation">Translation</option>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">@Localizer["Model"]</label>
                        <select class="form-select" @bind="m_modelFilter" @bind:after="LoadAsync">
                            <option value="">@Localizer["All"]</option>
                            <option value="gemini-3.1-flash-lite-preview">Flash Lite</option>
                            <option value="gemini-3-flash-preview">Flash</option>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">@Localizer["UserType"]</label>
                        <select class="form-select" @bind="m_userTypeFilter" @bind:after="LoadAsync">
                            <option value="All">@Localizer["All"]</option>
                            <option value="Anonymous">@Localizer["Anonymous"]</option>
                            <option value="Registered">@Localizer["Registered"]</option>
                        </select>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">@Localizer["Username"]</label>
                        <input type="text" class="form-control" placeholder="@Localizer["FilterByUser"]"
                               @bind="m_userNameFilter" @bind:after="LoadAsync" />
                    </div>
                </div>
            </div>
        </div>

        @* Content *@
        <div class="col-md-9">
            @* Model Breakdown *@
            <div class="card mb-3">
                <div class="card-header"><h3 class="card-title">@Localizer["CostBreakdown"]</h3></div>
                <div class="table-responsive">
                    <table class="table table-vcenter card-table">
                        <thead>
                            <tr>
                                <th>@Localizer["Model"]</th>
                                <th>@Localizer["Queries"]</th>
                                <th>@Localizer["InputTokens"]</th>
                                <th>@Localizer["OutputTokens"]</th>
                                <th>@Localizer["CostUsd"]</th>
                                <th>@Localizer["CostMyr"]</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var row in m_breakdown)
                            {
                                <tr>
                                    <td><span class="badge bg-blue-lt">@row.Model</span></td>
                                    <td>@row.Queries</td>
                                    <td>@row.InputTokens.ToString("N0")</td>
                                    <td>@row.OutputTokens.ToString("N0")</td>
                                    <td>$@row.CostUsd.ToString("F4")</td>
                                    <td>RM @row.CostMyr.ToString("F2")</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            </div>

            @* Query Log *@
            <div class="card">
                <div class="card-header"><h3 class="card-title">@Localizer["QueryLog"]</h3></div>

                @if (m_loading)
                {
                    <div class="progress progress-sm"><div class="progress-bar progress-bar-indeterminate"></div></div>
                }

                <div class="table-responsive">
                    <table class="table table-vcenter card-table table-striped">
                        <thead>
                            <tr>
                                <th>@Localizer["DateTime"]</th>
                                <th>@Localizer["UserIp"]</th>
                                <th>@Localizer["Service"]</th>
                                <th>@Localizer["Model"]</th>
                                <th>@Localizer["Question"]</th>
                                <th>@Localizer["Tokens"]</th>
                                <th>@Localizer["Cost"]</th>
                                <th>@Localizer["Status"]</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var log in m_logs)
                            {
                                <tr class="cursor-pointer" @onclick="() => ToggleExpand(log.AiUsageLogId)">
                                    <td><span title="@log.DateTime.ToString("g")">@GetTimeAgo(log.DateTime)</span></td>
                                    <td>@(log.UserName ?? log.IpAddress ?? "unknown")</td>
                                    <td><span class="badge bg-cyan-lt">@log.ServiceName</span></td>
                                    <td><span class="badge bg-blue-lt">@log.Model</span></td>
                                    <td>
                                        <div class="text-truncate" style="max-width: 200px;" title="@log.Question">
                                            @log.Question
                                        </div>
                                    </td>
                                    <td>@log.InputTokens / @log.OutputTokens</td>
                                    <td>$@log.EstimatedCostUsd.ToString("F4")</td>
                                    <td>
                                        <span class="badge @(log.Success ? "bg-success" : "bg-danger")">
                                            @(log.Success ? "OK" : "Error")
                                        </span>
                                    </td>
                                </tr>
                                @if (m_expandedId == log.AiUsageLogId)
                                {
                                    <tr>
                                        <td colspan="8">
                                            <div class="p-3 bg-light rounded">
                                                <strong>@Localizer["Question"]:</strong>
                                                <p class="mb-2">@log.Question</p>
                                                <strong>@Localizer["ResponsePreview"]:</strong>
                                                <p class="mb-2 text-secondary">@log.ResponsePreview</p>
                                                @if (!string.IsNullOrEmpty(log.Error))
                                                {
                                                    <strong>@Localizer["Error"]:</strong>
                                                    <p class="text-danger mb-0">@log.Error</p>
                                                }
                                                <div class="mt-2 text-secondary small">
                                                    IP: @log.IpAddress | Session: @log.SessionId | Cost: $@log.EstimatedCostUsd.ToString("F6") / RM @log.EstimatedCostMyr.ToString("F4")
                                                </div>
                                            </div>
                                        </td>
                                    </tr>
                                }
                            }
                        </tbody>
                    </table>
                </div>

                @if (m_logs.Count == 0 && !m_loading)
                {
                    <div class="card-body">
                        <div class="empty">
                            <div class="empty-icon"><i class="ti ti-sparkles" style="font-size: 3rem;"></i></div>
                            <p class="empty-title">@Localizer["NoLogs"]</p>
                            <p class="empty-subtitle text-secondary">@Localizer["NoLogsSubtitle"]</p>
                        </div>
                    </div>
                }

                @if (m_totalRows > m_pageSize)
                {
                    <div class="card-footer d-flex align-items-center">
                        <p class="m-0 text-secondary">
                            @Localizer["Showing", $"{((m_page - 1) * m_pageSize) + 1}", $"{Math.Min(m_page * m_pageSize, m_totalRows)}", $"{m_totalRows}"]
                        </p>
                        <ul class="pagination m-0 ms-auto">
                            <li class="page-item @(m_page == 1 ? "disabled" : "")">
                                <a class="page-link" href="javascript:" @onclick="() => GoToPage(m_page - 1)">
                                    <i class="ti ti-chevron-left"></i>
                                </a>
                            </li>
                            @for (var i = 1; i <= Math.Min(m_totalPages, 5); i++)
                            {
                                var pageNum = i;
                                <li class="page-item @(m_page == pageNum ? "active" : "")">
                                    <a class="page-link" href="javascript:" @onclick="() => GoToPage(pageNum)">@pageNum</a>
                                </li>
                            }
                            <li class="page-item @(m_page == m_totalPages ? "disabled" : "")">
                                <a class="page-link" href="javascript:" @onclick="() => GoToPage(m_page + 1)">
                                    <i class="ti ti-chevron-right"></i>
                                </a>
                            </li>
                        </ul>
                    </div>
                }
            </div>
        </div>
    </div>
</div>

@code {
    private List<AiUsageLog> m_logs = [];
    private List<AiUsageModelBreakdown> m_breakdown = [];
    private AiUsageStats m_stats = new(0, 0, 0, 0, 0, 0);
    private bool m_loading = true;
    private int? m_expandedId;

    // Filters
    private string m_dateRange = "today";
    private string? m_serviceFilter;
    private string? m_modelFilter;
    private string m_userTypeFilter = "All";
    private string? m_userNameFilter;

    // Pagination
    private int m_page = 1;
    private int m_pageSize = 20;
    private int m_totalRows;
    private int m_totalPages => (int)Math.Ceiling((double)m_totalRows / m_pageSize);

    protected override async Task OnInitializedAsync()
    {
        m_stats = await this.AiUsageService.GetStatsAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        m_loading = true;
        try
        {
            var filter = BuildFilter();
            var lo = await this.AiUsageService.GetLogsAsync(filter, m_page, m_pageSize);
            m_logs = lo.ItemCollection.ToList();
            m_totalRows = lo.TotalRows;
            m_breakdown = await this.AiUsageService.GetModelBreakdownAsync(filter);
        }
        finally
        {
            m_loading = false;
        }
    }

    private AiUsageFilter BuildFilter()
    {
        var now = DateTimeOffset.Now;
        DateTimeOffset? from = m_dateRange switch
        {
            "today" => new DateTimeOffset(now.Date, now.Offset),
            "week" => now.AddDays(-7),
            "month" => now.AddDays(-30),
            _ => null
        };

        Enum.TryParse<AiUserType>(m_userTypeFilter, out var userType);

        return new AiUsageFilter(
            m_userNameFilter,
            m_serviceFilter,
            m_modelFilter,
            userType,
            from,
            null);
    }

    private void ToggleExpand(int logId)
    {
        m_expandedId = m_expandedId == logId ? null : logId;
    }

    private async Task GoToPage(int page)
    {
        if (page < 1 || page > m_totalPages) return;
        m_page = page;
        await LoadAsync();
    }

    private string GetTimeAgo(DateTimeOffset timestamp)
    {
        var diff = DateTimeOffset.Now - timestamp;
        if (diff.TotalMinutes < 1) return "just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return timestamp.ToString("MMM d");
    }
}
```

- [ ] **Step 2: Create resource files**

Create `src/MotoRent.Client/Resources/Pages/SuperAdmin/AiUsage.resx` with entries:

| Key | Value |
|-----|-------|
| PageTitle | AI Usage - MotoRent |
| HeaderTitle | AI Usage Dashboard |
| HeaderSubtitle | Monitor Gemini API usage, costs, and rate limits |
| StatToday | Queries Today |
| StatThisWeek | This Week |
| StatCostToday | Cost Today |
| StatCostWeek | Cost This Week |
| Filters | Filters |
| DateRange | Date Range |
| Today | Today |
| Last7Days | Last 7 Days |
| Last30Days | Last 30 Days |
| Service | Service |
| Model | Model |
| UserType | User Type |
| All | All |
| Anonymous | Anonymous |
| Registered | Registered |
| Username | Username |
| FilterByUser | Filter by username... |
| CostBreakdown | Cost Breakdown by Model |
| Queries | Queries |
| InputTokens | Input Tokens |
| OutputTokens | Output Tokens |
| CostUsd | Cost (USD) |
| CostMyr | Cost (MYR) |
| QueryLog | Query Log |
| DateTime | Date/Time |
| UserIp | User / IP |
| Question | Question |
| Tokens | Tokens (In/Out) |
| Cost | Cost |
| Status | Status |
| ResponsePreview | Response Preview |
| Error | Error |
| NoLogs | No AI queries found |
| NoLogsSubtitle | No queries match your current filters. |
| Showing | Showing {0} to {1} of {2} entries |

Create the `.en.resx` with the same values.

Create the `.th.resx` with Thai translations:

| Key | Value |
|-----|-------|
| PageTitle | การใช้งาน AI - MotoRent |
| HeaderTitle | แดชบอร์ดการใช้งาน AI |
| HeaderSubtitle | ตรวจสอบการใช้งาน Gemini API ต้นทุน และขีดจำกัด |
| StatToday | คำถามวันนี้ |
| StatThisWeek | สัปดาห์นี้ |
| StatCostToday | ต้นทุนวันนี้ |
| StatCostWeek | ต้นทุนสัปดาห์นี้ |
| Filters | ตัวกรอง |
| DateRange | ช่วงวันที่ |
| Today | วันนี้ |
| Last7Days | 7 วันที่ผ่านมา |
| Last30Days | 30 วันที่ผ่านมา |
| Service | บริการ |
| Model | โมเดล |
| UserType | ประเภทผู้ใช้ |
| All | ทั้งหมด |
| Anonymous | ไม่ระบุตัวตน |
| Registered | ลงทะเบียนแล้ว |
| Username | ชื่อผู้ใช้ |
| FilterByUser | กรองตามชื่อผู้ใช้... |
| CostBreakdown | รายละเอียดต้นทุนตามโมเดล |
| Queries | คำถาม |
| InputTokens | โทเค็นขาเข้า |
| OutputTokens | โทเค็นขาออก |
| CostUsd | ต้นทุน (USD) |
| CostMyr | ต้นทุน (MYR) |
| QueryLog | บันทึกคำถาม |
| DateTime | วันที่/เวลา |
| UserIp | ผู้ใช้ / IP |
| Question | คำถาม |
| Tokens | โทเค็น (เข้า/ออก) |
| Cost | ต้นทุน |
| Status | สถานะ |
| ResponsePreview | ตัวอย่างคำตอบ |
| Error | ข้อผิดพลาด |
| NoLogs | ไม่พบคำถาม AI |
| NoLogsSubtitle | ไม่มีคำถามที่ตรงกับตัวกรองปัจจุบัน |
| Showing | แสดง {0} ถึง {1} จาก {2} รายการ |

Create the `.ms.resx` with Bahasa Melayu translations:

| Key | Value |
|-----|-------|
| PageTitle | Penggunaan AI - MotoRent |
| HeaderTitle | Papan Pemuka Penggunaan AI |
| HeaderSubtitle | Pantau penggunaan Gemini API, kos dan had kadar |
| StatToday | Pertanyaan Hari Ini |
| StatThisWeek | Minggu Ini |
| StatCostToday | Kos Hari Ini |
| StatCostWeek | Kos Minggu Ini |
| Filters | Penapis |
| DateRange | Julat Tarikh |
| Today | Hari Ini |
| Last7Days | 7 Hari Lepas |
| Last30Days | 30 Hari Lepas |
| Service | Perkhidmatan |
| Model | Model |
| UserType | Jenis Pengguna |
| All | Semua |
| Anonymous | Tanpa Nama |
| Registered | Berdaftar |
| Username | Nama Pengguna |
| FilterByUser | Tapis mengikut pengguna... |
| CostBreakdown | Pecahan Kos Mengikut Model |
| Queries | Pertanyaan |
| InputTokens | Token Masukan |
| OutputTokens | Token Keluaran |
| CostUsd | Kos (USD) |
| CostMyr | Kos (MYR) |
| QueryLog | Log Pertanyaan |
| DateTime | Tarikh/Masa |
| UserIp | Pengguna / IP |
| Question | Soalan |
| Tokens | Token (Masuk/Keluar) |
| Cost | Kos |
| Status | Status |
| ResponsePreview | Pratonton Jawapan |
| Error | Ralat |
| NoLogs | Tiada pertanyaan AI ditemui |
| NoLogsSubtitle | Tiada pertanyaan sepadan dengan penapis semasa. |
| Showing | Menunjukkan {0} hingga {1} daripada {2} entri |

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/MotoRent.Client/Pages/SuperAdmin/AiUsage.razor src/MotoRent.Client/Resources/Pages/SuperAdmin/AiUsage*.resx
git commit -m "feat: add SuperAdmin AI Usage page with filters, cost breakdown, and query log"
```

---

### Task 8: Add AI Usage Link to SuperAdmin Navigation

**Files:**
- Modify: `src/MotoRent.Client/Pages/SuperAdmin/StartPage.razor`

- [ ] **Step 1: Add AI Usage management card**

In `StartPage.razor`, after the "User Feedback Card" section (after the closing `</div>` of the feedback card around line 268), add:

```html
<!-- AI Usage Card -->
<div class="col-12 col-md-6 col-lg-4 mr-animate-in mr-animate-delay-3">
    <a href="/super-admin/ai-usage" class="mr-management-card">
        <div class="mr-management-card-icon bg-indigo">
            <i class="ti ti-sparkles"></i>
        </div>
        <div class="mr-management-card-body">
            <h3 class="mr-management-card-title">@Localizer["CardAiUsageTitle"]</h3>
            <p class="mr-management-card-text">
                @Localizer["CardAiUsageText"]
            </p>
        </div>
        <div class="mr-management-card-footer">
            <span>@Localizer["CardAiUsageFooter"]</span>
            <i class="ti ti-arrow-right"></i>
        </div>
    </a>
</div>
```

- [ ] **Step 2: Add localization keys**

Add to StartPage resource files:

| Key | en | th | ms |
|-----|----|----|-----|
| CardAiUsageTitle | AI Usage | การใช้งาน AI | Penggunaan AI |
| CardAiUsageText | Monitor Gemini API usage, costs, and rate limits across all services. | ตรวจสอบการใช้งาน Gemini API ต้นทุน และขีดจำกัดทั่วทุกบริการ | Pantau penggunaan Gemini API, kos dan had kadar merentas semua perkhidmatan. |
| CardAiUsageFooter | View AI usage dashboard | ดูแดชบอร์ดการใช้งาน AI | Lihat papan pemuka penggunaan AI |

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/MotoRent.Client/Pages/SuperAdmin/StartPage.razor src/MotoRent.Client/Resources/Pages/SuperAdmin/StartPage*.resx
git commit -m "feat: add AI Usage management card to SuperAdmin StartPage"
```

---

### Task 9: Standardize Gemini API Key Header Across Services

**Files:**
- Modify: `src/MotoRent.Services/DocumentOcrService.cs`
- Modify: `src/MotoRent.Services/VehicleRecognitionService.cs`
- Modify: `src/MotoRent.Services/TransliterationService.cs`
- Modify: `src/MotoRent.Services/DocumentTemplateAiService.cs`
- Modify: `src/MotoRent.Services/DocumentationTranslationService.cs`

- [ ] **Step 1: Fix DocumentOcrService**

In `src/MotoRent.Services/DocumentOcrService.cs`, replace line 51:

```csharp
var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
```

with:

```csharp
var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
```

And change the HTTP call (around line 55) from:

```csharp
var response = await client.PostAsJsonAsync(url, request, s_jsonOptions, cancellationToken);
```

to:

```csharp
using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
httpRequest.Headers.Add("x-goog-api-key", apiKey);
httpRequest.Content = JsonContent.Create(request, options: s_jsonOptions);
var response = await client.SendAsync(httpRequest, cancellationToken);
```

Add `using System.Net.Http.Json;` if not already present.

- [ ] **Step 2: Fix VehicleRecognitionService**

In `src/MotoRent.Services/VehicleRecognitionService.cs`, replace line 76:

```csharp
var url = $"{BASE_URL}/{model}:generateContent?key={apiKey}";
```

with:

```csharp
var url = $"{BASE_URL}/{model}:generateContent";
```

And change the HTTP call (around line 80) from:

```csharp
var response = await client.PostAsJsonAsync(url, request, s_jsonOptions, cancellationToken);
```

to:

```csharp
using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
httpRequest.Headers.Add("x-goog-api-key", apiKey);
httpRequest.Content = JsonContent.Create(request, options: s_jsonOptions);
var response = await client.SendAsync(httpRequest, cancellationToken);
```

- [ ] **Step 3: Fix TransliterationService**

In `src/MotoRent.Services/TransliterationService.cs`, replace line 91:

```csharp
var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
```

with:

```csharp
var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
```

And change the HTTP call (around line 95) from:

```csharp
var response = await client.PostAsJsonAsync(url, request, s_jsonOptions, cancellationToken);
```

to:

```csharp
using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
httpRequest.Headers.Add("x-goog-api-key", apiKey);
httpRequest.Content = JsonContent.Create(request, options: s_jsonOptions);
var response = await client.SendAsync(httpRequest, cancellationToken);
```

- [ ] **Step 4: Fix DocumentTemplateAiService**

In `src/MotoRent.Services/DocumentTemplateAiService.cs`, apply the same pattern at **two** locations:

Line 76 — replace:
```csharp
var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
```
with:
```csharp
var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
```

Line 80 — replace:
```csharp
var response = await client.PostAsJsonAsync(url, request, s_jsonOptions, cancellationToken);
```
with:
```csharp
using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
httpRequest.Headers.Add("x-goog-api-key", apiKey);
httpRequest.Content = JsonContent.Create(request, options: s_jsonOptions);
var response = await client.SendAsync(httpRequest, cancellationToken);
```

Line 206 — same pattern (second `generateContent` call in `ExtractTemplateLayoutAsync`):
Replace the URL and HTTP call identically.

- [ ] **Step 5: Fix DocumentationTranslationService**

In `src/MotoRent.Services/DocumentationTranslationService.cs`, replace line 75:

```csharp
var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
```

with:

```csharp
var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
```

And change line 79 from:
```csharp
var response = await client.PostAsJsonAsync(url, request, cancellationToken);
```

to:
```csharp
using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
httpRequest.Headers.Add("x-goog-api-key", apiKey);
httpRequest.Content = JsonContent.Create(request);
var response = await client.SendAsync(httpRequest, cancellationToken);
```

- [ ] **Step 6: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 7: Commit**

```bash
git add src/MotoRent.Services/DocumentOcrService.cs src/MotoRent.Services/VehicleRecognitionService.cs src/MotoRent.Services/TransliterationService.cs src/MotoRent.Services/DocumentTemplateAiService.cs src/MotoRent.Services/DocumentationTranslationService.cs
git commit -m "fix: standardize Gemini API key to x-goog-api-key header across all services"
```

---

### Task 10: Consolidate Duplicate Gemini Response Models

**Files:**
- Modify: `src/MotoRent.Services/VehicleRecognitionService.cs`
- Modify: `src/MotoRent.Services/TransliterationService.cs`

- [ ] **Step 1: Update VehicleRecognitionService**

In `src/MotoRent.Services/VehicleRecognitionService.cs`:

1. Add `using MotoRent.Domain.Core;` at the top
2. Replace all references to `GeminiApiResponse` with `GeminiResponse`, `GeminiApiCandidate` with `GeminiCandidate`, etc.

Specifically, change line 83 from:
```csharp
var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiApiResponse>(s_jsonOptions, cancellationToken);
```
to:
```csharp
var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
```

3. Delete the internal classes at the bottom of the file (lines 270-288):
```csharp
// DELETE these classes:
internal class GeminiApiResponse { ... }
internal class GeminiApiCandidate { ... }
internal class GeminiApiContent { ... }
internal class GeminiApiPart { ... }
```

- [ ] **Step 2: Update TransliterationService**

In `src/MotoRent.Services/TransliterationService.cs`:

1. Add `using MotoRent.Domain.Core;` at the top
2. Change line 98 from:
```csharp
var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(s_jsonOptions, cancellationToken);
```
to:
```csharp
var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
```

Note: This service has a private `GeminiResponse` class that shadows the domain one. After adding the `using`, the private classes are no longer needed.

3. Delete the private nested classes (lines 175-194):
```csharp
// DELETE these classes:
private class GeminiResponse { ... }
private class Candidate { ... }
private class Content { ... }
private class Part { ... }
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build`
Expected: Build succeeds.

- [ ] **Step 4: Commit**

```bash
git add src/MotoRent.Services/VehicleRecognitionService.cs src/MotoRent.Services/TransliterationService.cs
git commit -m "refactor: consolidate duplicate Gemini response models to shared GeminiModels.cs"
```

---

### Task 11: Run SQL Migration

**Files:**
- None (database operation)

- [ ] **Step 1: Run the SQL table creation**

Execute the `database/tables/Core.AiUsageLog.sql` script against the local PostgreSQL database:

```bash
psql -h localhost -p 5432 -U postgres -d motorent -f database/tables/Core.AiUsageLog.sql
```

Expected: Table and indexes created successfully.

- [ ] **Step 2: Verify table exists**

```bash
psql -h localhost -p 5432 -U postgres -d motorent -c "\d \"AiUsageLog\""
```

Expected: Table definition displayed with all computed columns and indexes.

---

### Task 12: Final Build & Smoke Test

- [ ] **Step 1: Full build**

Run: `dotnet build`
Expected: Build succeeds with zero errors.

- [ ] **Step 2: Run existing tests**

Run: `dotnet test`
Expected: All existing tests pass (the `DocumentationSearchServiceTests` may need updating since the return type changed from `string` to `GeminiSearchResult`).

- [ ] **Step 3: Fix test if needed**

If `DocumentationSearchServiceTests` fails, update the test to expect `GeminiSearchResult` instead of `string`. The test at `tests/MotoRent.Server.Tests/DocumentationSearchServiceTests.cs` line 81 calls `AskGeminiAsync` and expects a `string` — change to access `.Answer` on the result.

- [ ] **Step 4: Commit any test fixes**

```bash
git add tests/
git commit -m "test: update DocumentationSearchService tests for GeminiSearchResult return type"
```
