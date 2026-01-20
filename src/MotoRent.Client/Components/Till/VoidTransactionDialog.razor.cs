namespace MotoRent.Client.Components.Till;

/// <summary>
/// Result from void transaction dialog.
/// Contains the transaction ID to void and the reason.
/// </summary>
public class VoidTransactionResult
{
    /// <summary>
    /// ID of the transaction to void.
    /// </summary>
    public int TransactionId { get; set; }

    /// <summary>
    /// Reason for voiding the transaction.
    /// Required field with minimum 5 characters.
    /// </summary>
    public string Reason { get; set; } = "";
}
