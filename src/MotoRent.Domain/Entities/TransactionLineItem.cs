namespace MotoRent.Domain.Entities;

/// <summary>
/// Working line item model for transaction editing.
/// Converted to ReceiptItem when creating receipt.
/// </summary>
public class TransactionLineItem
{
    /// <summary>Unique identifier for this item (for list manipulation)</summary>
    public string ItemId { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>Category: Rental, Insurance, Accessory, Deposit, Discount</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Description (e.g., "Honda PCX 160", "Basic Insurance")</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Detail line (e.g., "3 days @ 500/day")</summary>
    public string? Detail { get; set; }

    /// <summary>Quantity (days or units)</summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>Price per unit</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Total amount for this line</summary>
    public decimal Amount { get; set; }

    /// <summary>Whether this is a deduction (discount, refund)</summary>
    public bool IsDeduction { get; set; }

    /// <summary>Sort order for display</summary>
    public int SortOrder { get; set; }

    /// <summary>For accessories: the accessory ID for removal</summary>
    public int? AccessoryId { get; set; }

    /// <summary>For insurance: the insurance ID for changes</summary>
    public int? InsuranceId { get; set; }

    /// <summary>Whether this item can be removed by staff</summary>
    public bool CanRemove { get; set; }

    /// <summary>Reason for discount (required for Discount category)</summary>
    public string? DiscountReason { get; set; }
}
