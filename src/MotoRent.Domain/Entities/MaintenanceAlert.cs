namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents an automated alert for a vehicle that is due or overdue for maintenance.
/// </summary>
public class MaintenanceAlert : Entity
{
    public int MaintenanceAlertId { get; set; }
    public int VehicleId { get; set; }
    public int? MaintenanceScheduleId { get; set; }
    public int ServiceTypeId { get; set; }

    /// <summary>
    /// Current status of the alert (DueSoon or Overdue).
    /// </summary>
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.DueSoon;

    /// <summary>
    /// When the alert was generated.
    /// </summary>
    public DateTimeOffset TriggerDate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Vehicle mileage when the alert was triggered.
    /// </summary>
    public int? TriggerMileage { get; set; }

    /// <summary>
    /// Vehicle engine hours when the alert was triggered (for boats/jetskis).
    /// </summary>
    public int? TriggerEngineHours { get; set; }

    /// <summary>
    /// Whether the alert has been acknowledged by a user.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// When the alert was resolved (e.g., service performed or dismissed).
    /// </summary>
    public DateTimeOffset? ResolvedDate { get; set; }

    /// <summary>
    /// Who resolved the alert.
    /// </summary>
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// Additional notes about the alert or its resolution.
    /// </summary>
    public string? Notes { get; set; }

    // Denormalized fields for display
    public string? VehicleName { get; set; }
    public string? LicensePlate { get; set; }
    public string? ServiceTypeName { get; set; }

    public override int GetId() => this.MaintenanceAlertId;
    public override void SetId(int value) => this.MaintenanceAlertId = value;
}
