using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotoRent.Domain.Storage;

namespace MotoRent.Server.Controllers;

/// <summary>
/// API controller for binary file storage operations.
/// Handles file uploads to S3 and retrieval of stored files.
/// </summary>
[ApiController]
[Route("api/stores")]
[Authorize]
public class StoresController : ControllerBase
{
    private IBinaryStore BinaryStore { get; }
    private ILogger<StoresController> Logger { get; }

    public StoresController(IBinaryStore binaryStore, ILogger<StoresController> logger)
    {
        this.BinaryStore = binaryStore;
        this.Logger = logger;
    }

    /// <summary>
    /// Upload a single file to storage.
    /// </summary>
    [HttpPost("upload-single")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
    public async Task<IActionResult> UploadSingle(
        [FromForm] IFormFile file,
        [FromQuery] bool publicAccess = false,
        [FromQuery] string? publicStoreId = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        try
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var storeId = publicAccess && !string.IsNullOrWhiteSpace(publicStoreId)
                ? $"public-{publicStoreId}"
                : publicAccess
                    ? $"public-{Guid.NewGuid():N}"
                    : Guid.NewGuid().ToString("N");

            var document = new BinaryStore
            {
                StoreId = storeId,
                FileName = file.FileName,
                Extension = extension,
                ContentType = file.ContentType,
                IsPublicAccess = publicAccess
            };

            // Add EXIF metadata if provided
            foreach (var key in this.Request.Form.Keys.Where(k => k.StartsWith("x-Exif-")))
            {
                var value = this.Request.Form[key].ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    document.Headers[key.Replace("x-Exif-", "")] = value;
                }
            }

            // Add prepend if provided
            if (this.Request.Form.TryGetValue("x-prepend", out var prepend) && !string.IsNullOrWhiteSpace(prepend))
            {
                storeId = $"{prepend}{storeId}";
                document.StoreId = storeId;
            }

            await using var stream = file.OpenReadStream();
            await this.BinaryStore.AddAsync(document, stream);

            this.Logger.LogInformation("File uploaded: {StoreId} ({FileName})", storeId, file.FileName);

            return Ok(new
            {
                storeId,
                fileName = file.FileName,
                contentType = file.ContentType,
                size = file.Length,
                url = this.BinaryStore.GetImageUrl(storeId)
            });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to upload file: {FileName}", file.FileName);
            return StatusCode(500, new { error = "Failed to upload file", details = ex.Message });
        }
    }

    /// <summary>
    /// Get file content by store ID.
    /// </summary>
    [HttpGet("{storeId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFile(string storeId)
    {
        if (string.IsNullOrWhiteSpace(storeId))
            return BadRequest(new { error = "Store ID is required" });

        // Handle special IDs
        if (storeId == "no-image")
            return Redirect("/images/no-image.png");
        if (storeId == "no-user")
            return Redirect("/images/no-user.png");

        try
        {
            var document = await this.BinaryStore.GetContentAsync(storeId);
            if (document == null)
                return NotFound(new { error = "File not found" });

            return File(document.Content!, document.ContentType ?? "application/octet-stream", document.FileName);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get file: {StoreId}", storeId);
            return StatusCode(500, new { error = "Failed to retrieve file" });
        }
    }

    /// <summary>
    /// Get pre-signed URL for a file.
    /// </summary>
    [HttpGet("{storeId}/url")]
    public async Task<IActionResult> GetUrl(string storeId)
    {
        if (string.IsNullOrWhiteSpace(storeId))
            return BadRequest(new { error = "Store ID is required" });

        var url = await this.BinaryStore.GetImageUrlAsync(storeId);
        return Ok(new { url });
    }

    /// <summary>
    /// Delete a file from storage.
    /// </summary>
    [HttpDelete("{storeId}")]
    public async Task<IActionResult> DeleteFile(string storeId)
    {
        if (string.IsNullOrWhiteSpace(storeId))
            return BadRequest(new { error = "Store ID is required" });

        try
        {
            await this.BinaryStore.DeleteAsync(storeId);
            this.Logger.LogInformation("File deleted: {StoreId}", storeId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to delete file: {StoreId}", storeId);
            return StatusCode(500, new { error = "Failed to delete file" });
        }
    }

    /// <summary>
    /// List files in a directory/prefix.
    /// </summary>
    [HttpGet("list/{*directory}")]
    public async Task<IActionResult> ListDirectory(string directory)
    {
        try
        {
            var files = await this.BinaryStore.ListDirectoryAsync(directory);
            return Ok(files.Select(f => new
            {
                f.StoreId,
                f.FileName,
                url = this.BinaryStore.GetImageUrl(f.StoreId ?? "")
            }));
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to list directory: {Directory}", directory);
            return StatusCode(500, new { error = "Failed to list directory" });
        }
    }
}
