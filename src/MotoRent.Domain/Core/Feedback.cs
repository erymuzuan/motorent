using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

public enum FeedbackType { General, ErrorReport }
public enum FeedbackStatus { New, Reviewed, InProgress, Resolved, Dismissed }

public class Feedback : Entity
{
    public int FeedbackId { get; set; }

    // Auto-captured
    public string? AccountNo { get; set; }
    public string? UserName { get; set; }
    public string? Url { get; set; }
    public string? UserAgent { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;

    // User-provided
    public string Description { get; set; } = "";
    public FeedbackType Type { get; set; } = FeedbackType.General;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;
    public string? ScreenshotStoreId { get; set; }

    // Error linkage (ErrorReport type only)
    public int? LogEntryId { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }

    // Admin
    public string? AdminNotes { get; set; }

    public override int GetId() => FeedbackId;
    public override void SetId(int value) => FeedbackId = value;
}
