namespace MotoRent.Domain.Entities;

/// <summary>
/// Depreciation calculation methods.
/// </summary>
public enum DepreciationMethod
{
    /// <summary>
    /// Immediate depreciation percentage on first rental.
    /// </summary>
    DayOutOfDoor,

    /// <summary>
    /// Equal monthly amounts over useful life.
    /// </summary>
    StraightLine,

    /// <summary>
    /// Percentage of book value each period.
    /// </summary>
    DecliningBalance,

    /// <summary>
    /// User-defined monthly schedule.
    /// </summary>
    Custom,

    /// <summary>
    /// Combination: Day Out of Door + Straight Line for remainder.
    /// </summary>
    HybridDayOutThenStraightLine,

    /// <summary>
    /// Combination: Day Out of Door + Declining Balance.
    /// </summary>
    HybridDayOutThenDeclining
}

/// <summary>
/// Asset status values.
/// </summary>
public enum AssetStatus
{
    Active,
    Disposed,
    WriteOff
}

/// <summary>
/// Depreciation entry types.
/// </summary>
public enum DepreciationEntryType
{
    System,
    Manual,
    Adjustment
}

/// <summary>
/// Asset expense categories.
/// </summary>
public enum AssetExpenseCategory
{
    Maintenance,
    Insurance,
    Financing,
    Accident,
    Registration,
    Consumables,
    Fuel,
    Cleaning,
    Inspection,
    Tax,
    Other
}

/// <summary>
/// Loan status values.
/// </summary>
public enum LoanStatus
{
    Active,
    PaidOff,
    Defaulted
}

/// <summary>
/// Loan payment status values.
/// </summary>
public enum LoanPaymentStatus
{
    Pending,
    Paid,
    Late,
    Missed
}
