namespace MotoRent.Domain.Models;

public class AccessoryReturnItem
{
    public int AccessoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int QuantityRented { get; set; }
    public int QuantityReturned { get; set; }
    public bool IsMissing => this.QuantityReturned < this.QuantityRented;
    public decimal MissingCharge { get; set; }
}
