using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Helps;

public class SupportRequest : Entity, ISerialNumber
{
    public int SupportRequestId { get; set; }
    public string? No { get; set; }
    public string Prefix => "SR";
    public string? Title { get; set; }
    public string? AccountNo { get; set; }
    public string? Type { get; set; }
    public int? CommentId { get; set; }
    public int? EntityId { get; set; }
    public string? AssignedTo { get; set; }
    public string? RequestedBy { get; set; }
    public TaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public double? TotalMinutesResponded { get; set; }
    public double? TotalMinutesResolved { get; set; }

    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset? ResolvedTimestamp { get; set; }
    public DateTimeOffset? ClosedTimestamp { get; set; }

    public List<Discussion> Discussions { get; } = [];
    public List<Attachment>? Attachments { get; set; }
    public string? SystemInfo { get; set; }
    public string? ObjectUri { get; set; }
    public string? ReproSteps { get; set; }
    public string? Area { get; set; }
    public string? Reason { get; set; }
    public Difficulty? Difficulty { get; set; }
    public double? EstimatedEffortHour { get; set; }

    public override int GetId() => SupportRequestId;
    public override void SetId(int value) => SupportRequestId = value;

    public SupportRequest()
    {
    }

    public SupportRequest(Comment comment, string accountNo)
    {
        this.CommentId = comment.CommentId;
        this.AccountNo = accountNo;
        this.Type = comment.Type;
        this.EntityId = comment.EntityId;
        this.RequestedBy = comment.User;
        this.Title = comment.Title;
        this.SystemInfo = comment.Text;
        this.Timestamp = comment.Timestamp;
        this.ObjectUri = comment.ObjectUri;
    }
}

public class Discussion
{
    public DateTimeOffset Timestamp { get; set; }
    public string? User { get; set; }
    public string? Text { get; set; }
    public List<Attachment>? Attachments { get; set; }
}
