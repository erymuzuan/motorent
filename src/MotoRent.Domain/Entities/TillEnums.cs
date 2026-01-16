namespace MotoRent.Domain.Entities;

/// <summary>
/// Status of a till session.
/// </summary>
public enum TillSessionStatus
{
    /// <summary>
    /// Session is active and accepting transactions.
    /// </summary>
    Open,

    /// <summary>
    /// Staff is counting cash and preparing to close.
    /// </summary>
    Reconciling,

    /// <summary>
    /// Session closed with no variance.
    /// </summary>
    Closed,

    /// <summary>
    /// Session closed but had a variance (short or over).
    /// </summary>
    ClosedWithVariance,

    /// <summary>
    /// Session closed and awaiting manager verification.
    /// </summary>
    PendingVerification,

    /// <summary>
    /// Session has been verified by a manager.
    /// </summary>
    Verified
}

/// <summary>
/// Type of till transaction.
/// </summary>
public enum TillTransactionType
{
    // Inflows (Cash In)
    /// <summary>
    /// Payment received for a rental.
    /// </summary>
    RentalPayment,

    /// <summary>
    /// Booking deposit received.
    /// </summary>
    BookingDeposit,

    /// <summary>
    /// Security deposit collected.
    /// </summary>
    SecurityDeposit,

    /// <summary>
    /// Charge for damage to vehicle.
    /// </summary>
    DamageCharge,

    /// <summary>
    /// Fee for late return.
    /// </summary>
    LateFee,

    /// <summary>
    /// Additional surcharge (fuel, delivery, etc.).
    /// </summary>
    Surcharge,

    /// <summary>
    /// Miscellaneous income.
    /// </summary>
    MiscellaneousIncome,

    /// <summary>
    /// Cash added to till from safe/manager.
    /// </summary>
    TopUp,

    /// <summary>
    /// Card payment (tracked for reconciliation, not cash).
    /// </summary>
    CardPayment,

    /// <summary>
    /// Bank transfer payment (tracked for reconciliation, not cash).
    /// </summary>
    BankTransfer,

    /// <summary>
    /// PromptPay payment (tracked for reconciliation, not cash).
    /// </summary>
    PromptPay,

    // Outflows (Cash Out)
    /// <summary>
    /// Refund of deposit to customer.
    /// </summary>
    DepositRefund,

    /// <summary>
    /// Fuel reimbursement to staff or customer.
    /// </summary>
    FuelReimbursement,

    /// <summary>
    /// Commission paid to booking agent.
    /// </summary>
    AgentCommission,

    /// <summary>
    /// Petty cash expense.
    /// </summary>
    PettyCash,

    /// <summary>
    /// Cash moved to safe/bank.
    /// </summary>
    Drop,

    /// <summary>
    /// Cash shortage recorded at close.
    /// </summary>
    CashShortage
}

/// <summary>
/// Direction of cash flow in a till transaction.
/// </summary>
public enum TillTransactionDirection
{
    /// <summary>
    /// Money coming into the till.
    /// </summary>
    In,

    /// <summary>
    /// Money going out of the till.
    /// </summary>
    Out
}
