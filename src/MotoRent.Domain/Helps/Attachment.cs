using System.Text.Json.Serialization;

namespace MotoRent.Domain.Helps;

public class Attachment
{
    public string? StoreId { get; set; }
    public string? Note { get; set; }
    public string? Extension { get; set; }
    public long Size { get; set; }
    public string? Type { get; set; }
    public string? FileName { get; set; }
    public DateTime? UploadedDate { get; set; }
    public string? ExternalLink { get; set; }

    [JsonIgnore]
    public byte[]? Content { get; set; }
}
