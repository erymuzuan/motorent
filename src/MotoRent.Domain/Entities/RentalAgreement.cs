namespace MotoRent.Domain.Entities;

public class RentalAgreement : Entity
{
    public int RentalAgreementId { get; set; }
    public int RentalId { get; set; }
    public string AgreementText { get; set; } = string.Empty;
    public string SignatureImagePath { get; set; } = string.Empty;  // Touch signature image
    public DateTimeOffset SignedOn { get; set; }
    public string? SignedByIp { get; set; }

    public override int GetId() => this.RentalAgreementId;
    public override void SetId(int value) => this.RentalAgreementId = value;
}
