namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a line item on a receipt.
/// Embedded in the Receipt entity's Items collection.
/// </summary>
public class ReceiptItem
{
    /// <summary>
    /// Unique identifier for this item within the receipt
    /// </summary>
    public string ItemId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>
    /// Category of the line item (Rental, Insurance, Accessory, Deposit, Damage, etc.)
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Description of the item (e.g., "Honda PCX 160", "Basic Insurance", "Helmet")
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional detail (e.g., "3 days @ 500/day", "2 units @ 50/day")
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Quantity of items (usually 1, but can be days for rental or units for accessories)
    /// </summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Price per unit
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total amount for this line item (Quantity * UnitPrice)
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Whether this is a deduction (e.g., damage charges deducted from deposit)
    /// </summary>
    public bool IsDeduction { get; set; }

    /// <summary>
    /// Sort order for display
    /// </summary>
    public int SortOrder { get; set; }
}
