using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;

namespace MotoRent.Server.Controllers;

/// <summary>
/// API controller for rental agreement file uploads.
/// </summary>
[ApiController]
[Route("api/agreements")]
[Authorize]
public class AgreementsController : ControllerBase
{
    private readonly IRequestContext m_requestContext;
    private readonly ILogger<AgreementsController> m_logger;

    public AgreementsController(
        IRequestContext requestContext,
        ILogger<AgreementsController> logger)
    {
        m_requestContext = requestContext;
        m_logger = logger;
    }

    /// <summary>
    /// Uploads a scanned/photographed signed rental agreement.
    /// </summary>
    /// <param name="file">The agreement image or PDF file</param>
    /// <param name="documentType">Type of document (should be "SignedAgreement")</param>
    /// <returns>File path of uploaded document</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<IActionResult> UploadAgreement(
        IFormFile file,
        [FromForm] string documentType)
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

        try
        {
            // Get upload path from configuration
            var basePath = MotoConfig.FileStorageBasePath;
            var accountNo = m_requestContext.GetAccountNo();
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), basePath, accountNo ?? "default", "agreements");

            // Ensure directory exists
            Directory.CreateDirectory(uploadDir);

            // Generate unique filename with timestamp for easier identification
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var fileName = $"{timestamp}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadDir, fileName);

            // Save the file
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            m_logger.LogInformation("Signed agreement uploaded: {FilePath}", filePath);

            // Return relative path for storage in database
            var relativePath = GetRelativePath(filePath);

            return Ok(new AgreementUploadResponse
            {
                FilePath = relativePath
            });
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Failed to upload signed agreement");
            return StatusCode(500, new { error = "Failed to upload agreement" });
        }
    }

    /// <summary>
    /// Serves a signed agreement file.
    /// </summary>
    [HttpGet("file/{*filePath}")]
    public IActionResult GetAgreementFile(string filePath)
    {
        var basePath = MotoConfig.FileStorageBasePath;
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

    private string GetRelativePath(string fullPath)
    {
        var basePath = MotoConfig.FileStorageBasePath;
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
/// Response model for agreement upload.
/// </summary>
public class AgreementUploadResponse
{
    public string FilePath { get; set; } = string.Empty;
}
