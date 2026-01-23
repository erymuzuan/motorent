using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing till sessions, transactions, and reconciliation.
/// </summary>
public partial class TillService(RentalDataContext context, ExchangeRateService exchangeRateService)
{
    private RentalDataContext Context { get; } = context;
    private ExchangeRateService ExchangeRateService { get; } = exchangeRateService;
}

/// <summary>
/// Summary of a till session for display.
/// </summary>
public class TillSessionSummary
{
    public int TillSessionId { get; set; }
    public string StaffDisplayName { get; set; } = string.Empty;
    public decimal OpeningFloat { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalDropped { get; set; }
    public decimal TotalToppedUp { get; set; }
    public decimal ExpectedCash { get; set; }
    public decimal ActualCash { get; set; }
    public decimal Variance { get; set; }
    public TillSessionStatus Status { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

/// <summary>
/// Daily summary for EOD reconciliation.
/// </summary>
public class DailyTillSummary
{
    public DateTime Date { get; set; }
    public int ShopId { get; set; }
    public int TotalSessions { get; set; }
    public int VerifiedSessions { get; set; }
    public int SessionsWithVariance { get; set; }
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalCardPayments { get; set; }
    public decimal TotalBankTransfers { get; set; }
    public decimal TotalPromptPay { get; set; }
    public decimal TotalDropped { get; set; }
    public decimal TotalVariance { get; set; }
    public List<TillSessionSummary> Sessions { get; set; } = [];

    /// <summary>
    /// Total non-cash (electronic) payments.
    /// </summary>
    public decimal TotalElectronicPayments => this.TotalCardPayments + this.TotalBankTransfers + this.TotalPromptPay;

    /// <summary>
    /// Net cash movement (in - out - dropped).
    /// </summary>
    public decimal NetCashMovement => this.TotalCashIn - this.TotalCashOut - this.TotalDropped;
}

/// <summary>
/// Represents a currency amount for a multi-currency drop operation.
/// </summary>
public class CurrencyDropAmount
{
    /// <summary>
    /// Currency code (THB, USD, EUR, CNY)
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.THB;

    /// <summary>
    /// Amount to drop in this currency
    /// </summary>
    public decimal Amount { get; set; }
}
