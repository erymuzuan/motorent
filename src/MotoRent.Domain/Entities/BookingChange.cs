namespace MotoRent.Domain.Entities;

/// <summary>
/// Records a change made to a booking for audit trail.
/// </summary>
public class BookingChange
{
    /// <summary>
    /// When the change was made.
    /// </summary>
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Username of who made the change.
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Type of change made.
    /// </summary>
    public string ChangeType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of the change.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Previous value (if applicable).
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value (if applicable).
    /// </summary>
    public string? NewValue { get; set; }
}

/// <summary>
/// Change type constants.
/// </summary>
public static class BookingChangeType
{
    public const string Created = "Created";
    public const string StatusChange = "StatusChange";
    public const string DateChange = "DateChange";
    public const string VehicleChange = "VehicleChange";
    public const string VehicleAdded = "VehicleAdded";
    public const string VehicleRemoved = "VehicleRemoved";
    public const string ShopChange = "ShopChange";
    public const string LocationChange = "LocationChange";
    public const string PaymentReceived = "PaymentReceived";
    public const string RefundProcessed = "RefundProcessed";
    public const string CustomerInfoChange = "CustomerInfoChange";
    public const string InsuranceChange = "InsuranceChange";
    public const string AccessoriesChange = "AccessoriesChange";
    public const string CheckedIn = "CheckedIn";
    public const string Cancelled = "Cancelled";
    public const string NotesChange = "NotesChange";
    public const string AgentAssigned = "AgentAssigned";
    public const string AgentRemoved = "AgentRemoved";
    public const string AgentFinancialsUpdated = "AgentFinancialsUpdated";
}
