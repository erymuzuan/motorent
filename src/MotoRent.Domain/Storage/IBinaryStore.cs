namespace MotoRent.Domain.Storage;

/// <summary>
/// Interface for binary file storage operations (images, documents, etc.).
/// Supports both public and private storage buckets.
/// </summary>
public interface IBinaryStore
{
    /// <summary>
    /// Uploads a file to storage.
    /// </summary>
    Task AddAsync(BinaryStore document, Stream? stream = null);

    /// <summary>
    /// Gets file content by store ID.
    /// </summary>
    Task<BinaryStore?> GetContentAsync(string id);

    /// <summary>
    /// Deletes a file from storage.
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Gets the URL for accessing an image (async version for pre-signed URLs).
    /// </summary>
    Task<string> GetImageUrlAsync(string id);

    /// <summary>
    /// Gets the URL for accessing an image (sync version).
    /// </summary>
    string GetImageUrl(string id);

    /// <summary>
    /// Lists files in a directory/prefix.
    /// </summary>
    Task<IEnumerable<BinaryStore>> ListDirectoryAsync(string directory) => Task.FromResult(Array.Empty<BinaryStore>().AsEnumerable());

    /// <summary>
    /// Replaces S3 URLs in content with pre-signed URLs.
    /// </summary>
    string ReplaceImageUrlContent(string content) => content;
}
