namespace MotoRent.Client.Components.Till;

/// <summary>
/// Result from void receipt dialog.
/// Contains the receipt ID to void and the reason.
/// </summary>
public class VoidReceiptResult
{
    /// <summary>
    /// ID of the receipt to void.
    /// </summary>
    public int ReceiptId { get; set; }

    /// <summary>
    /// Reason for voiding the receipt.
    /// Required field with minimum 5 characters.
    /// </summary>
    public string Reason { get; set; } = "";
}
