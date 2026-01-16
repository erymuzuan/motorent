namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a cashier till session for a staff member.
/// Each staff opens their own session at the start of their shift
/// and closes it at the end with reconciliation.
/// </summary>
public class TillSession : Entity
{
    public int TillSessionId { get; set; }
    public int ShopId { get; set; }
    public string StaffUserName { get; set; } = string.Empty;
    public string StaffDisplayName { get; set; } = string.Empty;
    public TillSessionStatus Status { get; set; } = TillSessionStatus.Open;

    // Opening
    public decimal OpeningFloat { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public string? OpeningNotes { get; set; }

    // Running totals (denormalized for quick display)
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalDropped { get; set; }
    public decimal TotalToppedUp { get; set; }

    // Non-cash totals (for EOD reconciliation)
    public decimal TotalCardPayments { get; set; }
    public decimal TotalBankTransfers { get; set; }
    public decimal TotalPromptPay { get; set; }

    // Closing
    public decimal ActualCash { get; set; }
    public decimal Variance { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? ClosingNotes { get; set; }

    // Manager verification
    public string? VerifiedByUserName { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }

    /// <summary>
    /// Calculates the expected cash in the till.
    /// Expected = Opening Float + Cash In - Cash Out - Dropped + Topped Up
    /// </summary>
    public decimal ExpectedCash => OpeningFloat + TotalCashIn - TotalCashOut - TotalDropped + TotalToppedUp;

    /// <summary>
    /// Total non-cash payments received during the session.
    /// </summary>
    public decimal TotalNonCashPayments => TotalCardPayments + TotalBankTransfers + TotalPromptPay;

    public override int GetId() => TillSessionId;
    public override void SetId(int value) => TillSessionId = value;
}
