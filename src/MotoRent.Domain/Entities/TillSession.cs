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

    /// <summary>
    /// Tracks actual foreign currency amounts in the drawer.
    /// Key is currency code (THB, USD, EUR, CNY), value is amount in that currency.
    /// THB balance is the total THB in drawer; foreign currency balances track
    /// un-converted foreign currency received during the session.
    /// Initialized with THB = OpeningFloat on session open; foreign currencies start at 0.
    /// </summary>
    public Dictionary<string, decimal> CurrencyBalances { get; set; } = new()
    {
        [SupportedCurrencies.THB] = 0
    };

    // Closing
    public decimal ActualCash { get; set; }
    public decimal Variance { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? ClosingNotes { get; set; }

    // Close metadata (Phase 7)
    public string? ClosedByUserName { get; set; }
    public bool IsForceClose { get; set; }
    public string? ForceCloseApprovedBy { get; set; }

    /// <summary>
    /// True if session was closed on a different day than it was opened.
    /// Flagged for manager review due to date mismatch.
    /// </summary>
    public bool IsLateClose { get; set; }

    /// <summary>
    /// Original date when session should have been closed (OpenedAt.Date).
    /// Set when IsLateClose is true.
    /// </summary>
    public DateTime? ExpectedCloseDate { get; set; }

    /// <summary>
    /// Actual counted balances per currency at close.
    /// Key: currency code (THB, USD, EUR, CNY)
    /// Value: actual counted amount in that currency
    /// </summary>
    public Dictionary<string, decimal> ActualBalances { get; set; } = new();

    /// <summary>
    /// Variance per currency at close (Actual - Expected).
    /// Positive = over, Negative = short, Zero = balanced.
    /// Key: currency code (THB, USD, EUR, CNY)
    /// Value: variance in that currency
    /// </summary>
    public Dictionary<string, decimal> ClosingVariances { get; set; } = new();

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

    /// <summary>
    /// Gets the balance for a specific currency in the drawer.
    /// Returns 0 if the currency is not tracked in this session.
    /// </summary>
    /// <param name="currency">Currency code (THB, USD, EUR, CNY)</param>
    /// <returns>Amount in the specified currency, or 0 if not tracked</returns>
    public decimal GetCurrencyBalance(string currency)
    {
        return CurrencyBalances.TryGetValue(currency, out var balance) ? balance : 0;
    }

    public override int GetId() => TillSessionId;
    public override void SetId(int value) => TillSessionId = value;
}
