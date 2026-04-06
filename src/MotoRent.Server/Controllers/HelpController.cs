using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Services;
using System.Threading;
using System.Threading.Tasks;

namespace MotoRent.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelpController(
    DocumentationSearchService searchService,
    DocumentationTranslationService translationService) : ControllerBase
{
    private DocumentationSearchService SearchService { get; } = searchService;
    private DocumentationTranslationService TranslationService { get; } = translationService;

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return this.BadRequest("Question cannot be empty.");
        }

        var answer = await this.SearchService.AskGeminiAsync(request.Question, cancellationToken);
        return this.Ok(new AskResponse(answer));
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

    public sealed record AskRequest(string Question);

    public sealed record AskResponse(string Answer);
}
