namespace MotoRent.Domain.Entities;

public class RentalAccessory : Entity
{
    public int RentalAccessoryId { get; set; }
    public int RentalId { get; set; }
    public int AccessoryId { get; set; }
    public int Quantity { get; set; }
    public decimal ChargedAmount { get; set; }

    public override int GetId() => this.RentalAccessoryId;
    public override void SetId(int value) => this.RentalAccessoryId = value;
}
