namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a detailed maintenance/service record for a vehicle.
/// Unlike MaintenanceSchedule which tracks next-due status, this stores the complete service history.
/// </summary>
public class MaintenanceRecord : Entity
{
    public int MaintenanceRecordId { get; set; }

    /// <summary>
    /// The vehicle this service was performed on
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// The type of service performed
    /// </summary>
    public int ServiceTypeId { get; set; }

    /// <summary>
    /// Denormalized service type name for display
    /// </summary>
    public string? ServiceTypeName { get; set; }

    /// <summary>
    /// Date when the service was performed
    /// </summary>
    public DateTimeOffset ServiceDate { get; set; }

    /// <summary>
    /// Mileage/odometer reading at the time of service
    /// </summary>
    public int ServiceMileage { get; set; }

    /// <summary>
    /// Total cost of the service
    /// </summary>
    public decimal? Cost { get; set; }

    /// <summary>
    /// Workshop/garage information
    /// </summary>
    public WorkshopInfo Workshop { get; set; } = new();

    /// <summary>
    /// Notes about the service
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Photos of the work performed (parts replaced, repairs done, etc.)
    /// </summary>
    public List<MaintenancePhoto> Photos { get; set; } = [];

    /// <summary>
    /// Attached documents (receipts, invoices, warranty cards)
    /// </summary>
    public List<MaintenanceDocument> Documents { get; set; } = [];

    public override int GetId() => this.MaintenanceRecordId;
    public override void SetId(int value) => this.MaintenanceRecordId = value;
}

/// <summary>
/// Workshop/service provider information
/// </summary>
public class WorkshopInfo
{
    /// <summary>
    /// Name of the workshop or mechanic
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Address or location
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Any notes about this workshop
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Represents a photo attached to a maintenance record
/// </summary>
public class MaintenancePhoto
{
    /// <summary>
    /// Unique identifier for this photo within the record
    /// </summary>
    public string PhotoId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// BinaryStore ID for the uploaded image
    /// </summary>
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Optional caption describing what the photo shows
    /// </summary>
    public string? Caption { get; set; }

    /// <summary>
    /// When the photo was uploaded
    /// </summary>
    public DateTimeOffset UploadedOn { get; set; } = DateTimeOffset.Now;
}

/// <summary>
/// Represents a document (receipt, invoice) attached to a maintenance record
/// </summary>
public class MaintenanceDocument
{
    /// <summary>
    /// Unique identifier for this document within the record
    /// </summary>
    public string DocumentId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// BinaryStore ID for the uploaded document
    /// </summary>
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Type of document (Receipt, Invoice, Warranty, Other)
    /// </summary>
    public string DocumentType { get; set; } = "Receipt";

    /// <summary>
    /// Optional notes about this document
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// When the document was uploaded
    /// </summary>
    public DateTimeOffset UploadedOn { get; set; } = DateTimeOffset.Now;
}
