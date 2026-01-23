namespace MotoRent.Domain.Entities;

/// <summary>
/// Records a shortage (variance) entry for accountability tracking.
/// Created when a manager logs a till variance as a shortage during
/// session close or daily close reconciliation.
/// </summary>
public class ShortageLog : Entity
{
    public int ShortageLogId { get; set; }
    public int ShopId { get; set; }

    /// <summary>
    /// The till session where the shortage occurred.
    /// </summary>
    public int TillSessionId { get; set; }

    /// <summary>
    /// The daily close operation this shortage was logged during.
    /// Null if logged during session close (before daily close).
    /// </summary>
    public int? DailyCloseId { get; set; }

    /// <summary>
    /// Username of the staff member responsible for the session.
    /// </summary>
    public string StaffUserName { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the staff member.
    /// </summary>
    public string StaffDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Currency of the shortage (THB, USD, EUR, CNY).
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.THB;

    /// <summary>
    /// Amount of the shortage in the original currency.
    /// Always stored as positive value.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Amount converted to THB at the time of logging.
    /// For THB shortages, equals Amount.
    /// </summary>
    public decimal AmountInThb { get; set; }

    /// <summary>
    /// Manager-provided reason/explanation for the shortage.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Username of the manager who logged the shortage.
    /// </summary>
    public string LoggedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// When the shortage was logged.
    /// </summary>
    public DateTimeOffset LoggedAt { get; set; }

    public override int GetId() => ShortageLogId;
    public override void SetId(int value) => ShortageLogId = value;
}
