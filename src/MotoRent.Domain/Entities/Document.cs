namespace MotoRent.Domain.Entities;

public class Document : Entity
{
    public int DocumentId { get; set; }
    public int RenterId { get; set; }
    public string DocumentType { get; set; } = string.Empty;  // Passport, NationalId, DrivingLicense
    public string ImagePath { get; set; } = string.Empty;
    public string? OcrRawJson { get; set; }   // Gemini response
    public string? ExtractedData { get; set; } // Parsed fields
    public DateTimeOffset UploadedOn { get; set; }
    public bool IsVerified { get; set; }

    public override int GetId() => this.DocumentId;
    public override void SetId(int value) => this.DocumentId = value;
}
