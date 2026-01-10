namespace MotoRent.Domain.Entities;

/// <summary>
/// Tracks the maintenance schedule for a specific service type on a specific motorbike.
/// Minimal tracking: just last service date/mileage and next due date/mileage.
/// </summary>
public class MaintenanceSchedule : Entity
{
    public int MaintenanceScheduleId { get; set; }
    public int MotorbikeId { get; set; }
    public int ServiceTypeId { get; set; }

    /// <summary>
    /// Date when this service was last performed
    /// </summary>
    public DateTimeOffset? LastServiceDate { get; set; }

    /// <summary>
    /// Mileage when this service was last performed
    /// </summary>
    public int? LastServiceMileage { get; set; }

    /// <summary>
    /// Next due date (calculated: LastServiceDate + ServiceType.DaysInterval)
    /// </summary>
    public DateTimeOffset? NextDueDate { get; set; }

    /// <summary>
    /// Next due mileage (calculated: LastServiceMileage + ServiceType.KmInterval)
    /// </summary>
    public int? NextDueMileage { get; set; }

    /// <summary>
    /// Who performed the last service
    /// </summary>
    public string? LastServiceBy { get; set; }

    /// <summary>
    /// Notes about the last service
    /// </summary>
    public string? LastServiceNotes { get; set; }

    /// <summary>
    /// Denormalized service type name for display
    /// </summary>
    public string? ServiceTypeName { get; set; }

    /// <summary>
    /// Denormalized motorbike name for display
    /// </summary>
    public string? MotorbikeName { get; set; }

    public override int GetId() => this.MaintenanceScheduleId;
    public override void SetId(int value) => this.MaintenanceScheduleId = value;
}
