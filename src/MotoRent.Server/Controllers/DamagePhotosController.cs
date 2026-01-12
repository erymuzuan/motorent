using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;

namespace MotoRent.Server.Controllers;

/// <summary>
/// API controller for damage photo uploads during check-out.
/// </summary>
[ApiController]
[Route("api/damage-photos")]
[Authorize]
public class DamagePhotosController : ControllerBase
{
    private readonly IRequestContext m_requestContext;
    private readonly ILogger<DamagePhotosController> m_logger;

    public DamagePhotosController(
        IRequestContext requestContext,
        ILogger<DamagePhotosController> logger)
    {
        m_requestContext = requestContext;
        m_logger = logger;
    }

    /// <summary>
    /// Uploads a damage photo during check-out.
    /// </summary>
    /// <param name="file">The damage photo file</param>
    /// <param name="photoType">Type of photo (Before, After)</param>
    /// <param name="rentalId">Optional rental ID to associate the photo with</param>
    /// <returns>Uploaded file path</returns>
    [HttpPost("upload")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    public async Task<IActionResult> UploadDamagePhoto(
        IFormFile file,
        [FromForm] string photoType = "After",
        [FromForm] int? rentalId = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file uploaded" });
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest(new { error = "Invalid file type. Allowed: JPG, PNG, GIF, WebP" });
        }

        // Validate photo type
        var validTypes = new[] { "Before", "After" };
        if (!validTypes.Contains(photoType))
        {
            photoType = "After"; // Default to After
        }

        try
        {
            // Get upload path from configuration
            var basePath = MotoConfig.FileStorageBasePath;
            var accountNo = m_requestContext.GetAccountNo();

            // Store in: uploads/{accountNo}/damage/{rentalId or temp}/{guid}.jpg
            var folder = rentalId.HasValue ? rentalId.Value.ToString() : "temp";
            var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), basePath, accountNo ?? "default", "damage", folder);

            // Ensure directory exists
            Directory.CreateDirectory(uploadDir);

            // Generate unique filename with photo type prefix
            var fileName = $"{photoType.ToLower()}_{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadDir, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            m_logger.LogInformation("Damage photo uploaded: {FilePath}", filePath);

            // Return relative path
            var relativePath = GetRelativePath(filePath);

            return Ok(new DamagePhotoUploadResponse
            {
                FilePath = relativePath,
                PhotoType = photoType
            });
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Failed to upload damage photo");
            return StatusCode(500, new { error = "Failed to upload photo" });
        }
    }

    /// <summary>
    /// Serves a damage photo file.
    /// </summary>
    [HttpGet("file/{*filePath}")]
    public IActionResult GetDamagePhoto(string filePath)
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

    /// <summary>
    /// Gets all damage photos for a rental.
    /// </summary>
    [HttpGet("rental/{rentalId:int}")]
    public IActionResult GetDamagePhotosForRental(int rentalId)
    {
        var basePath = MotoConfig.FileStorageBasePath;
        var accountNo = m_requestContext.GetAccountNo();
        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), basePath, accountNo ?? "default", "damage", rentalId.ToString());

        if (!Directory.Exists(uploadDir))
        {
            return Ok(new List<DamagePhotoInfo>());
        }

        var photos = Directory.GetFiles(uploadDir)
            .Select(f => new DamagePhotoInfo
            {
                FilePath = GetRelativePath(f),
                PhotoType = Path.GetFileName(f).StartsWith("before_", StringComparison.OrdinalIgnoreCase) ? "Before" : "After",
                FileName = Path.GetFileName(f)
            })
            .ToList();

        return Ok(photos);
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
            _ => "application/octet-stream"
        };
    }
}

/// <summary>
/// Response model for damage photo upload.
/// </summary>
public class DamagePhotoUploadResponse
{
    public string FilePath { get; set; } = string.Empty;
    public string PhotoType { get; set; } = string.Empty;
}

/// <summary>
/// Info model for damage photo listing.
/// </summary>
public class DamagePhotoInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string PhotoType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
