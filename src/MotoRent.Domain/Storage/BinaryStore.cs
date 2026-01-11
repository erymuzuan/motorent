namespace MotoRent.Domain.Storage;

/// <summary>
/// Represents a binary file stored in cloud storage (S3, etc.).
/// </summary>
public class BinaryStore
{
    /// <summary>
    /// Unique identifier for the stored file.
    /// For public files, prefix with "public-" to route to public bucket.
    /// </summary>
    public string? StoreId { get; set; }

    /// <summary>
    /// Binary content of the file.
    /// </summary>
    public byte[]? Content { get; set; }

    /// <summary>
    /// File extension (e.g., ".jpg", ".pdf").
    /// </summary>
    public string? Extension { get; set; }

    /// <summary>
    /// MIME content type (e.g., "image/jpeg").
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Original file name.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Whether this file should be publicly accessible.
    /// Public files go to a separate bucket with public-read ACL.
    /// </summary>
    public bool IsPublicAccess { get; set; }

    /// <summary>
    /// Expiration date for temporary files.
    /// </summary>
    public DateTimeOffset? Expired { get; set; }

    /// <summary>
    /// Additional metadata headers.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
}
