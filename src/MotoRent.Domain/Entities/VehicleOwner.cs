namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a third-party vehicle owner who supplies vehicles for rental.
/// Owners receive compensation based on either daily rate or revenue share.
/// </summary>
public class VehicleOwner : Entity
{
    public int VehicleOwnerId { get; set; }

    #region Owner Information

    /// <summary>
    /// Full name of the owner (individual or company name).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Contact email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Thai national ID or passport number for payment purposes.
    /// </summary>
    public string? IdNumber { get; set; }

    /// <summary>
    /// Address for correspondence.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Additional notes about this owner.
    /// </summary>
    public string? Notes { get; set; }

    #endregion

    #region Bank Details

    /// <summary>
    /// Bank name for payment transfers.
    /// </summary>
    public string? BankName { get; set; }

    /// <summary>
    /// Bank account number.
    /// </summary>
    public string? BankAccountNumber { get; set; }

    /// <summary>
    /// Name on the bank account.
    /// </summary>
    public string? BankAccountName { get; set; }

    /// <summary>
    /// PromptPay ID (phone or national ID) for easy transfers.
    /// </summary>
    public string? PromptPayId { get; set; }

    #endregion

    #region Status

    /// <summary>
    /// Whether this owner is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    #endregion

    public override int GetId() => this.VehicleOwnerId;
    public override void SetId(int value) => this.VehicleOwnerId = value;
}
