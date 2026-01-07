namespace MotoRent.Domain.Entities;

public class Accessory : Entity
{
    public int AccessoryId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; } = string.Empty;  // Helmet, Phone Holder, Rain Gear
    public decimal DailyRate { get; set; }
    public int QuantityAvailable { get; set; }
    public bool IsIncluded { get; set; }              // Free with rental

    public override int GetId() => this.AccessoryId;
    public override void SetId(int value) => this.AccessoryId = value;
}
