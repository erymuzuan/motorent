using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

public class AiUsageLog : Entity
{
    public int AiUsageLogId { get; set; }

    // Caller identity
    public string? UserName { get; set; }
    public string? AccountNo { get; set; }
    public string? IpAddress { get; set; }
    public string? SessionId { get; set; }

    // Request details
    public string ServiceName { get; set; } = "";
    public string Model { get; set; } = "";
    public string? Question { get; set; }
    public string? ResponsePreview { get; set; }

    // Token/cost tracking
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public decimal EstimatedCostMyr { get; set; }

    // Status
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset DateTime { get; set; } = DateTimeOffset.Now;

    public override int GetId() => AiUsageLogId;
    public override void SetId(int value) => AiUsageLogId = value;
}
