namespace MotoRent.Domain.Entities;

/// <summary>
/// Supported document types for templates.
/// </summary>
public enum DocumentType
{
    BookingConfirmation,
    RentalAgreement,
    Receipt
}

/// <summary>
/// Status of a document template.
/// </summary>
public enum DocumentTemplateStatus
{
    Draft,
    Approved,
    Archived
}

/// <summary>
/// Represents a visual template for document generation (Booking, Rental, Receipt).
/// </summary>
public class DocumentTemplate : Entity
{
    public int DocumentTemplateId { get; set; }

    /// <summary>
    /// User-friendly name of the template.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional shop filter. If null or 0, template is applicable to all shops.
    /// </summary>
    public int? ShopId { get; set; }

    /// <summary>
    /// Type of document this template is for.
    /// </summary>
    public DocumentType Type { get; set; }

    /// <summary>
    /// Current workflow status of the template.
    /// </summary>
    public DocumentTemplateStatus Status { get; set; } = DocumentTemplateStatus.Draft;

    /// <summary>
    /// Pointer to the layout JSON in IBinaryStore.
    /// </summary>
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default template for its type in the organization.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Version number of the template.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Optional description or notes about the template.
    /// </summary>
    public string? Description { get; set; }

    public override int GetId() => this.DocumentTemplateId;
    public override void SetId(int value) => this.DocumentTemplateId = value;

    /// <summary>
    /// Validates the entity state.
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(this.Name);
    }

    /// <summary>
    /// Approves the template for use.
    /// </summary>
    public void Approve()
    {
        this.Status = DocumentTemplateStatus.Approved;
    }
}
