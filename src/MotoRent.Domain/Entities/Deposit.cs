namespace MotoRent.Domain.Entities;

public class Deposit : Entity
{
    public int DepositId { get; set; }
    public int RentalId { get; set; }
    public string DepositType { get; set; } = string.Empty;  // Cash, CardPreAuth, Passport
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Held";             // Held, Refunded, Forfeited
    public string? CardLast4 { get; set; }
    public string? TransactionRef { get; set; }
    public DateTimeOffset CollectedOn { get; set; }
    public DateTimeOffset? RefundedOn { get; set; }

    public override int GetId() => this.DepositId;
    public override void SetId(int value) => this.DepositId = value;
}
