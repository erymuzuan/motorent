namespace MotoRent.Domain.Entities;

public class Payment : Entity
{
    public int PaymentId { get; set; }
    public int RentalId { get; set; }
    public string PaymentType { get; set; } = string.Empty;   // Rental, Insurance, Accessory, Deposit, Damage
    public string PaymentMethod { get; set; } = string.Empty; // Cash, Card, PromptPay, BankTransfer
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";           // Pending, Completed, Refunded
    public string? TransactionRef { get; set; }
    public DateTimeOffset PaidOn { get; set; }
    public string? Notes { get; set; }

    public override int GetId() => this.PaymentId;
    public override void SetId(int value) => this.PaymentId = value;
}
