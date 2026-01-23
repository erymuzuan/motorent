using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;
using MotoRent.Services;
using MotoRent.Services.Core;

namespace MotoRent.Server.Controllers;

/// <summary>
/// API controller for document template management and preview generation.
/// </summary>
[ApiController]
[Route("api/document-templates")]
[Authorize]
public class DocumentTemplatesController(
    DocumentTemplateService templateService,
    DocumentTemplateAiService aiService,
    ITemplateDataResolver dataResolver,
    IHtmlTemplateRenderer htmlRenderer,
    IQuestPdfGenerator pdfGenerator,
    OrganizationService organizationService,
    IRequestContext requestContext,
    ILogger<DocumentTemplatesController> logger) : ControllerBase
{
    private DocumentTemplateService TemplateService { get; } = templateService;
    private DocumentTemplateAiService AiService { get; } = aiService;
    private ITemplateDataResolver DataResolver { get; } = dataResolver;
    private IHtmlTemplateRenderer HtmlRenderer { get; } = htmlRenderer;
    private IQuestPdfGenerator PdfGenerator { get; } = pdfGenerator;
    private OrganizationService OrganizationService { get; } = organizationService;
    private IRequestContext RequestContext { get; } = requestContext;
    private ILogger<DocumentTemplatesController> Logger { get; } = logger;

    /// <summary>
    /// Gets AI suggested clauses for a document type.
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions([FromQuery] DocumentType type, [FromQuery] string context)
    {
        try
        {
            var result = await this.AiService.GetSuggestedClausesAsync(type, context);
            return this.Ok(result);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get AI suggestions");
            return this.StatusCode(500, new { error = "Failed to get suggestions" });
        }
    }

    /// <summary>
    /// Gets all templates for a specific document type.
    /// </summary>
    [HttpGet("type/{type}")]
    public async Task<IActionResult> GetTemplatesByType(DocumentType type, [FromQuery] int? shopId = null, [FromQuery] int page = 1, [FromQuery] int size = 100)
    {
        try
        {
            var result = await this.TemplateService.GetTemplatesByTypeAsync(type, shopId, page, size);
            return this.Ok(result);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get templates for type {Type}", type);
            return this.StatusCode(500, new { error = "Failed to retrieve templates" });
        }
    }

    /// <summary>
    /// Gets a template's layout.
    /// </summary>
    [HttpGet("{storeId}/layout")]
    public async Task<IActionResult> GetLayout(string storeId)
    {
        try
        {
            var layout = await this.TemplateService.GetTemplateLayoutAsync(System.Net.WebUtility.UrlDecode(storeId));
            if (layout == null) return this.NotFound();
            return this.Ok(layout);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get layout for {StoreId}", storeId);
            return this.StatusCode(500, new { error = "Failed to retrieve layout" });
        }
    }

    /// <summary>
    /// Saves a template and its layout.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserAccount.ORG_ADMIN)]
    public async Task<IActionResult> SaveTemplate([FromBody] SaveTemplateRequest request)
    {
        try
        {
            var username = this.RequestContext.GetUserName() ?? "system";
            var result = await this.TemplateService.SaveTemplateAsync(request.Template, request.Layout, username);

            if (!result.Success)
            {
                return this.BadRequest(new { error = result.Message });
            }

            return this.Ok(result);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to save template {TemplateName}", request.Template.Name);
            return this.StatusCode(500, new { error = "Failed to save template" });
        }
    }

    /// <summary>
    /// Generates a live HTML preview of a layout.
    /// </summary>
    [HttpPost("preview/html")]
    public async Task<IActionResult> PreviewHtml([FromBody] PreviewRequest request)
    {
        try
        {
            var data = await this.GetSampleDataAsync(request.Type);
            var html = this.HtmlRenderer.RenderHtml(request.Layout, data);
            return this.Content(html, "text/html");
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to generate HTML preview");
            return this.StatusCode(500, new { error = "Failed to generate preview" });
        }
    }

    /// <summary>
    /// Generates a live PDF preview of a layout.
    /// </summary>
    [HttpPost("preview/pdf")]
    public async Task<IActionResult> PreviewPdf([FromBody] PreviewRequest request)
    {
        try
        {
            var data = await this.GetSampleDataAsync(request.Type);
            var pdf = this.PdfGenerator.GeneratePdf(request.Layout, data);
            return this.File(pdf, "application/pdf", "preview.pdf");
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to generate PDF preview");
            return this.StatusCode(500, new { error = "Failed to generate preview" });
        }
    }

    private async Task<Dictionary<string, object?>> GetSampleDataAsync(DocumentType type)
    {
        var accountNo = this.RequestContext.GetAccountNo();
        var org = await this.OrganizationService.GetOrganizationByAccountNoAsync(accountNo);
        var staff = new User { FullName = "Sample Staff", UserName = "staff@example.com" };

        Entity sampleEntity = type switch
        {
            DocumentType.BookingConfirmation => new Booking
            {
                BookingRef = "SMPL01",
                CustomerName = "Alice Sample",
                CustomerPhone = "+66 81 234 5678",
                StartDate = DateTimeOffset.UtcNow.AddDays(1),
                EndDate = DateTimeOffset.UtcNow.AddDays(4),
                TotalAmount = 1500,
                DepositRequired = 500,
                Status = "Confirmed"
            },
            DocumentType.RentalAgreement => new Rental
            {
                RentalId = 12345,
                RenterName = "Bob Sample",
                StartDate = DateTimeOffset.UtcNow,
                ExpectedEndDate = DateTimeOffset.UtcNow.AddDays(3),
                VehicleName = "Honda Click 160",
                TotalAmount = 1200,
                Status = "Active"
            },
            DocumentType.Receipt => new Receipt
            {
                ReceiptNo = "RCP-260123-00001",
                CustomerName = "Charlie Sample",
                IssuedOn = DateTimeOffset.UtcNow,
                GrandTotal = 800,
                Status = "Issued"
            },
            _ => new Booking()
        };

        return this.DataResolver.Resolve(sampleEntity, org ?? new Organization { Name = "Sample Org" }, staff);
    }
}

public class SaveTemplateRequest
{
    public DocumentTemplate Template { get; set; } = null!;
    public DocumentLayout Layout { get; set; } = null!;
}

public class PreviewRequest
{
    public DocumentType Type { get; set; }
    public DocumentLayout Layout { get; set; } = null!;
}
