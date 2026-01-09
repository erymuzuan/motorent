using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Services;

namespace MotoRent.Server.Controllers;

/// <summary>
/// API controller for document upload, OCR processing, and management.
/// </summary>
[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly DocumentOcrService m_ocrService;
    private readonly IRequestContext m_requestContext;
    private readonly IConfiguration m_configuration;
    private readonly ILogger<DocumentsController> m_logger;

    public DocumentsController(
        DocumentOcrService ocrService,
        IRequestContext requestContext,
        IConfiguration configuration,
        ILogger<DocumentsController> logger)
    {
        m_ocrService = ocrService;
        m_requestContext = requestContext;
        m_configuration = configuration;
        m_logger = logger;
    }

    /// <summary>
    /// Uploads a document image and performs OCR extraction.
    /// </summary>
    /// <param name="file">The document image file</param>
    /// <param name="documentType">Type of document (Passport, NationalId, DrivingLicense)</param>
    /// <param name="renterId">Optional renter ID to associate the document with</param>
    /// <returns>Extracted document data</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<IActionResult> UploadDocument(
        IFormFile file,
        [FromForm] string documentType,
        [FromForm] int? renterId = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { error = "Invalid file type. Allowed: JPG, PNG, GIF, WebP, PDF" });
        }

        // Validate document type
        var validTypes = new[] { "Passport", "NationalId", "DrivingLicense" };
        if (!validTypes.Contains(documentType))
        {
            return BadRequest(new { error = "Invalid document type. Use: Passport, NationalId, or DrivingLicense" });
        }

        try
        {
            // Get upload path from configuration
            var basePath = m_configuration["FileStorage:BasePath"] ?? "uploads";
            var accountNo = m_requestContext.GetAccountNo();
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), basePath, accountNo ?? "default", "documents");

            // Ensure directory exists
            Directory.CreateDirectory(uploadDir);

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadDir, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            m_logger.LogInformation("Document uploaded: {FilePath}", filePath);

            // Perform OCR extraction
            var extractedData = await m_ocrService.ExtractDocumentDataAsync(filePath, documentType);

            // If renterId is provided, save the document
            if (renterId.HasValue)
            {
                var username = m_requestContext.GetUserName() ?? "system";
                var document = await m_ocrService.SaveDocumentAsync(
                    renterId.Value,
                    documentType,
                    filePath,
                    extractedData,
                    username);

                return Ok(new DocumentUploadResponse
                {
                    DocumentId = document.DocumentId,
                    FilePath = GetRelativePath(filePath),
                    ExtractedData = extractedData
                });
            }

            // Return extracted data without saving (for preview during registration)
            return Ok(new DocumentUploadResponse
            {
                FilePath = GetRelativePath(filePath),
                ExtractedData = extractedData
            });
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Failed to process document upload");
            return StatusCode(500, new { error = "Failed to process document" });
        }
    }

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    [HttpGet("{documentId:int}")]
    public async Task<IActionResult> GetDocument(int documentId)
    {
        var document = await m_ocrService.GetDocumentByIdAsync(documentId);
        if (document == null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    /// <summary>
    /// Gets all documents for a renter.
    /// </summary>
    [HttpGet("renter/{renterId:int}")]
    public async Task<IActionResult> GetRenterDocuments(int renterId)
    {
        var documents = await m_ocrService.GetDocumentsByRenterAsync(renterId);
        return Ok(documents);
    }

    /// <summary>
    /// Verifies a document (marks it as reviewed/approved).
    /// </summary>
    [HttpPost("{documentId:int}/verify")]
    public async Task<IActionResult> VerifyDocument(int documentId, [FromBody] VerifyRequest request)
    {
        var username = m_requestContext.GetUserName() ?? "system";
        var result = await m_ocrService.VerifyDocumentAsync(documentId, request.IsVerified, username);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Message ?? "Failed to verify document" });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    [HttpDelete("{documentId:int}")]
    public async Task<IActionResult> DeleteDocument(int documentId)
    {
        var document = await m_ocrService.GetDocumentByIdAsync(documentId);
        if (document == null)
        {
            return NotFound();
        }

        var username = m_requestContext.GetUserName() ?? "system";
        var result = await m_ocrService.DeleteDocumentAsync(document, username);

        if (!result.Success)
        {
            return BadRequest(new { error = result.Message ?? "Failed to delete document" });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Serves a document image file.
    /// </summary>
    [HttpGet("file/{*filePath}")]
    public IActionResult GetDocumentFile(string filePath)
    {
        var basePath = m_configuration["FileStorage:BasePath"] ?? "uploads";
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), basePath, filePath);

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        // Security: Ensure the file is within the uploads directory
        var uploadsDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), basePath));
        var requestedPath = Path.GetFullPath(fullPath);
        if (!requestedPath.StartsWith(uploadsDir))
        {
            return BadRequest("Invalid file path");
        }

        var mimeType = GetMimeType(fullPath);
        return PhysicalFile(fullPath, mimeType);
    }

    /// <summary>
    /// Re-runs OCR on an existing document.
    /// </summary>
    [HttpPost("{documentId:int}/reprocess")]
    public async Task<IActionResult> ReprocessDocument(int documentId)
    {
        var document = await m_ocrService.GetDocumentByIdAsync(documentId);
        if (document == null)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(document.ImagePath) || !System.IO.File.Exists(document.ImagePath))
        {
            return BadRequest(new { error = "Document image file not found" });
        }

        try
        {
            var extractedData = await m_ocrService.ExtractDocumentDataAsync(document.ImagePath, document.DocumentType);

            // Update the document with new OCR data
            var username = m_requestContext.GetUserName() ?? "system";
            await m_ocrService.SaveDocumentAsync(
                document.RenterId,
                document.DocumentType,
                document.ImagePath,
                extractedData,
                username);

            return Ok(new DocumentUploadResponse
            {
                DocumentId = document.DocumentId,
                FilePath = GetRelativePath(document.ImagePath),
                ExtractedData = extractedData
            });
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Failed to reprocess document {DocumentId}", documentId);
            return StatusCode(500, new { error = "Failed to reprocess document" });
        }
    }

    private string GetRelativePath(string fullPath)
    {
        var basePath = m_configuration["FileStorage:BasePath"] ?? "uploads";
        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), basePath);
        if (fullPath.StartsWith(uploadsDir))
        {
            return fullPath[uploadsDir.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        return fullPath;
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}

/// <summary>
/// Response model for document upload.
/// </summary>
public class DocumentUploadResponse
{
    public int? DocumentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public ExtractedDocumentData ExtractedData { get; set; } = new();
}

/// <summary>
/// Request model for document verification.
/// </summary>
public class VerifyRequest
{
    public bool IsVerified { get; set; }
}
