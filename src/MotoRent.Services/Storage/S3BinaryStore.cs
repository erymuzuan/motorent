using System.Net;
using System.Text;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.Storage;
using Polly;

namespace MotoRent.Services.Storage;

/// <summary>
/// AWS S3 implementation of IBinaryStore.
/// Supports separate public and private buckets.
/// </summary>
public class S3BinaryStore : IBinaryStore, IDisposable
{
    private ILogger<S3BinaryStore> Logger { get; }
    private IAmazonS3 Client { get; }

    public S3BinaryStore(ILogger<S3BinaryStore> logger)
    {
        this.Logger = logger;

        var accessKeyId = MotoConfig.AwsAccessKeyId;
        var secretAccessKey = MotoConfig.AwsSecretAccessKey;
        var region = RegionEndpoint.GetBySystemName(MotoConfig.AwsRegion);

        if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(secretAccessKey))
        {
            this.Logger.LogWarning("AWS credentials not configured, S3 operations will fail");
            this.Client = new AmazonS3Client(region);
        }
        else
        {
            this.Client = new AmazonS3Client(accessKeyId, secretAccessKey, region);
        }
    }

    private static string GetBucketName(bool publicAccess = false)
    {
        return publicAccess ? MotoConfig.AwsPublicBucket : MotoConfig.AwsBucket;
    }

    public async Task AddAsync(BinaryStore document, Stream? stream = null)
    {
        if (document.Expired.HasValue && document.Expired.Value < DateTimeOffset.Now)
        {
            this.Logger.LogDebug("Skipping expired document {StoreId}", document.StoreId);
            return;
        }

        var ftu = new TransferUtility(this.Client);

        await using Stream rs = stream != null ? new MemoryStream() : new MemoryStream(document.Content ?? []);
        if (stream != null)
        {
            await stream.CopyToAsync(rs);
            rs.Position = 0;
        }

        var bucket = GetBucketName(document.IsPublicAccess);
        var request = new TransferUtilityUploadRequest
        {
            BucketName = bucket,
            StorageClass = S3StorageClass.StandardInfrequentAccess,
            PartSize = 6291456, // 6 MB
            Key = document.StoreId,
            InputStream = rs,
            ContentType = document.ContentType ?? GetMimeType(document.Extension),
            Headers = { CacheControl = "max-age=31536000" }
        };

        if (document.IsPublicAccess)
            request.CannedACL = S3CannedACL.PublicRead;

        // Add metadata
        if (!string.IsNullOrWhiteSpace(document.Extension))
            request.Metadata.Add("Extension", ToAscii(document.Extension));
        if (!string.IsNullOrWhiteSpace(document.FileName))
            request.Metadata.Add("FileName", ToAscii(document.FileName));
        if (document.Expired is { Year: > 2000 })
            request.Metadata.Add("Expired", document.Expired.ToString()!);

        foreach (var header in document.Headers)
        {
            request.Metadata.Add($"x-amz-meta-{header.Key}", header.Value);
        }

        var result = await Policy
            .Handle<Amazon.Runtime.Internal.HttpErrorResponseException>()
            .Or<Exception>(e => e.Message.Contains("disconnected", StringComparison.OrdinalIgnoreCase))
            .WaitAndRetryAsync(5, c => TimeSpan.FromMilliseconds(600 * Math.Pow(2, c)))
            .ExecuteAndCaptureAsync(async () => await ftu.UploadAsync(request));

        if (result.FinalException is not null)
        {
            this.Logger.LogError(result.FinalException,
                "Error uploading '{FileName}' storeId '{StoreId}' with size {Size} bytes",
                document.FileName, document.StoreId, document.Content?.Length ?? 0);
            throw result.FinalException;
        }

        this.Logger.LogInformation("Uploaded file {StoreId} to bucket {Bucket}", document.StoreId, bucket);
    }

    public async Task<BinaryStore?> GetContentAsync(string id)
    {
        var request = new GetObjectRequest
        {
            BucketName = GetBucketName(id.StartsWith("public-", StringComparison.OrdinalIgnoreCase)),
            Key = id
        };

        try
        {
            using var response = await this.Client.GetObjectAsync(request);
            await using var responseStream = response.ResponseStream;

            if (response.HttpStatusCode == HttpStatusCode.NotFound)
                return null;

            var filename = response.Metadata["x-amz-meta-filename"];
            var extension = response.Metadata["x-amz-meta-extension"];
            var expiredString = response.Metadata["x-amz-meta-expired"];

            DateTimeOffset? expired = null;
            if (DateTimeOffset.TryParse(expiredString, out var exp))
                expired = exp;

            var contentType = response.Headers["Content-Type"];

            using var ms = new MemoryStream();
            await responseStream.CopyToAsync(ms);
            var content = ms.ToArray();

            var document = new BinaryStore
            {
                Extension = extension,
                StoreId = id,
                Content = content,
                Expired = expired,
                FileName = filename,
                ContentType = contentType
            };

            foreach (var mk in response.Metadata.Keys)
            {
                document.Headers.TryAdd(mk.Replace("x-amz-meta-", ""), response.Metadata[mk]);
            }

            return document;
        }
        catch (AmazonS3Exception e)
        {
            if (e.Message.Contains("exist", StringComparison.OrdinalIgnoreCase))
                return null;
            throw;
        }
    }

    public async Task<IEnumerable<BinaryStore>> ListDirectoryAsync(string directory)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = GetBucketName(),
            Delimiter = "/",
            Prefix = directory
        };

        try
        {
            var response = await this.Client.ListObjectsV2Async(request);
            if (response.S3Objects is not { Count: > 0 })
                return [];

            return response.S3Objects.Select(obj => new BinaryStore
            {
                StoreId = obj.Key,
                FileName = obj.Key
            }).ToList();
        }
        catch (AmazonS3Exception e)
        {
            if (e.Message.Contains("exist", StringComparison.OrdinalIgnoreCase))
                return [];
            throw;
        }
    }

    public async Task DeleteAsync(string id)
    {
        var deleteObjectRequest = new DeleteObjectRequest
        {
            BucketName = GetBucketName(id.StartsWith("public-", StringComparison.OrdinalIgnoreCase)),
            Key = id
        };

        await this.Client.DeleteObjectAsync(deleteObjectRequest);
        this.Logger.LogInformation("Deleted file {StoreId}", id);
    }

    public Task<string> GetImageUrlAsync(string storeId)
    {
        return Task.FromResult(this.GetImageUrl(storeId));
    }

    public string GetImageUrl(string storeId)
    {
        return storeId switch
        {
            "no-image" => "/images/no-image.png",
            "no-user" => "/images/no-user.png",
            null or "" => string.Empty,
            _ when storeId.StartsWith("public-") => $"https://s3.{MotoConfig.AwsRegion}.amazonaws.com/{MotoConfig.AwsPublicBucket}/{storeId}",
            _ => this.GetPreSignedUrl(storeId)
        };
    }

    private string GetPreSignedUrl(string storeId)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = MotoConfig.AwsBucket,
            Key = storeId,
            Expires = DateTime.Now.Add(MotoConfig.AwsS3UrlTtl)
        };

        return this.Client.GetPreSignedURL(request);
    }

    public string ReplaceImageUrlContent(string content)
    {
        // Replace S3 URLs in HTML content with pre-signed URLs
        var publicUrl = $"https://s3.{MotoConfig.AwsRegion}.amazonaws.com/{MotoConfig.AwsBucket}/";
        if (!content.Contains(publicUrl))
            return content;

        var html = new StringBuilder(content);
        // Simple replacement - for more complex cases, use regex
        var pattern = publicUrl;
        var startIdx = 0;

        while ((startIdx = content.IndexOf(pattern, startIdx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var endIdx = content.IndexOf('"', startIdx);
            if (endIdx > startIdx)
            {
                var storeId = content[(startIdx + pattern.Length)..endIdx];
                var signedUrl = this.GetPreSignedUrl(storeId);
                html.Replace(pattern + storeId, signedUrl);
            }
            startIdx += pattern.Length;
        }

        return html.ToString();
    }

    private static string GetMimeType(string? extension) => extension switch
    {
        null => "application/octet-stream",
        ".json" => "application/json",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        ".xls" or ".xlsx" => "application/vnd.ms-excel",
        ".doc" => "application/msword",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".png" => "image/png",
        ".pdf" => "application/pdf",
        ".tiff" or ".tif" => "image/tiff",
        ".webp" => "image/webp",
        ".svg" => "image/svg+xml",
        ".bmp" => "image/bmp",
        ".ico" => "image/x-icon",
        ".ppt" => "application/vnd.ms-powerpoint",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".mp3" => "audio/mpeg",
        ".mp4" => "video/mp4",
        ".mov" => "video/quicktime",
        ".avi" => "video/x-msvideo",
        ".wav" => "audio/wav",
        ".md" => "text/markdown",
        ".xml" => "application/xml",
        ".html" or ".htm" => "text/html",
        ".css" => "text/css",
        ".js" => "application/javascript",
        ".zip" => "application/zip",
        ".tar" => "application/x-tar",
        ".gz" or ".gzip" => "application/gzip",
        ".7z" => "application/x-7z-compressed",
        ".woff" => "font/woff",
        ".woff2" => "font/woff2",
        ".ttf" => "font/ttf",
        _ => "application/octet-stream"
    };

    private static string ToAscii(string text)
    {
        // Convert to ASCII-safe string for S3 metadata
        return Encoding.ASCII.GetString(
            Encoding.Convert(
                Encoding.UTF8,
                Encoding.GetEncoding(
                    Encoding.ASCII.EncodingName,
                    new EncoderReplacementFallback(string.Empty),
                    new DecoderExceptionFallback()),
                Encoding.UTF8.GetBytes(text)));
    }

    public void Dispose()
    {
        this.Client?.Dispose();
    }
}
