namespace MotoRent.Domain.Entities;

public class DamagePhoto : Entity
{
    public int DamagePhotoId { get; set; }
    public int DamageReportId { get; set; }
    public string PhotoType { get; set; } = string.Empty;  // Before, After
    public string ImagePath { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CapturedOn { get; set; }

    public override int GetId() => this.DamagePhotoId;
    public override void SetId(int value) => this.DamagePhotoId = value;
}
