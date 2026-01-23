namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents the daily close state for a shop.
/// Each shop has one DailyClose record per calendar day, tracking
/// whether the day's transactions have been closed and reconciled.
/// </summary>
public class DailyClose : Entity
{
    public int DailyCloseId { get; set; }
    public int ShopId { get; set; }

    /// <summary>
    /// The date this close record is for (date only, no time component).
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Current status of the daily close.
    /// </summary>
    public DailyCloseStatus Status { get; set; } = DailyCloseStatus.Open;

    /// <summary>
    /// When the day was closed (null if still open).
    /// </summary>
    public DateTimeOffset? ClosedAt { get; set; }

    /// <summary>
    /// Username of the manager who performed the daily close.
    /// </summary>
    public string? ClosedByUserName { get; set; }

    #region Denormalized Summary Totals

    /// <summary>
    /// Total cash received during the day across all sessions.
    /// </summary>
    public decimal TotalCashIn { get; set; }

    /// <summary>
    /// Total cash paid out during the day across all sessions.
    /// </summary>
    public decimal TotalCashOut { get; set; }

    /// <summary>
    /// Total cash dropped to safe during the day across all sessions.
    /// </summary>
    public decimal TotalDropped { get; set; }

    /// <summary>
    /// Total variance (actual - expected) across all sessions.
    /// </summary>
    public decimal TotalVariance { get; set; }

    /// <summary>
    /// Total electronic payments (card + bank transfer + PromptPay) for the day.
    /// </summary>
    public decimal TotalElectronicPayments { get; set; }

    /// <summary>
    /// Number of till sessions for this day.
    /// </summary>
    public int SessionCount { get; set; }

    /// <summary>
    /// Number of sessions that had non-zero variance.
    /// </summary>
    public int SessionsWithVariance { get; set; }

    #endregion

    #region Reopen Tracking

    /// <summary>
    /// Whether this day has been reopened after being closed.
    /// </summary>
    public bool WasReopened { get; set; }

    /// <summary>
    /// Reason provided by manager when reopening the day.
    /// </summary>
    public string? ReopenReason { get; set; }

    /// <summary>
    /// When the day was reopened.
    /// </summary>
    public DateTimeOffset? ReopenedAt { get; set; }

    /// <summary>
    /// Username of the manager who reopened the day.
    /// </summary>
    public string? ReopenedByUserName { get; set; }

    #endregion

    public override int GetId() => DailyCloseId;
    public override void SetId(int value) => DailyCloseId = value;
}

/// <summary>
/// Status of a daily close record.
/// </summary>
public enum DailyCloseStatus
{
    /// <summary>
    /// Day is open, transactions allowed.
    /// </summary>
    Open,

    /// <summary>
    /// Day is closed, soft block on new transactions.
    /// </summary>
    Closed,

    /// <summary>
    /// Day has been fully verified and reconciled.
    /// </summary>
    Reconciled
}
