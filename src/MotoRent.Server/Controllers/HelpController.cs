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
        var sessionId = this.GetOrCreateSessionCookie();

        // 2. Rate limit check
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

        // 5. Log usage
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
        const string COOKIE_NAME = "mr_ai_session";
        if (Request.Cookies.TryGetValue(COOKIE_NAME, out var existing))
            return existing;

        var sessionId = Guid.NewGuid().ToString("N")[..16];
        Response.Cookies.Append(COOKIE_NAME, sessionId, new CookieOptions
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
