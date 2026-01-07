namespace MotoRent.Domain.Entities;

public class Shop : Entity
{
    public int ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;  // Phuket, Krabi, etc.
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
    public string? TermsAndConditions { get; set; }
    public bool IsActive { get; set; } = true;

    public override int GetId() => this.ShopId;
    public override void SetId(int value) => this.ShopId = value;
}
