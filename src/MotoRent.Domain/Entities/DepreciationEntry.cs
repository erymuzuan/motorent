using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Records depreciation entries for an asset.
/// Used for manual overrides and period-end records.
/// </summary>
public class DepreciationEntry : Entity
{
    public int DepreciationEntryId { get; set; }

    /// <summary>
    /// The asset this entry belongs to.
    /// </summary>
    public int AssetId { get; set; }

    #region Entry Details

    /// <summary>
    /// Period start date for this entry.
    /// </summary>
    public DateTimeOffset PeriodStart { get; set; }

    /// <summary>
    /// Period end date for this entry.
    /// </summary>
    public DateTimeOffset PeriodEnd { get; set; }

    /// <summary>
    /// Depreciation amount for this period.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Book value at start of period.
    /// </summary>
    public decimal BookValueStart { get; set; }

    /// <summary>
    /// Book value at end of period.
    /// </summary>
    public decimal BookValueEnd { get; set; }

    /// <summary>
    /// Method used for this calculation.
    /// </summary>
    public DepreciationMethod Method { get; set; }

    #endregion

    #region Override Information

    /// <summary>
    /// Whether this is a manual override entry.
    /// </summary>
    public bool IsManualOverride { get; set; }

    /// <summary>
    /// If manual override: the original calculated amount.
    /// </summary>
    public decimal? OriginalCalculatedAmount { get; set; }

    /// <summary>
    /// Reason for the override.
    /// </summary>
    public string? OverrideReason { get; set; }

    /// <summary>
    /// Who approved the override.
    /// </summary>
    public string? ApprovedBy { get; set; }

    #endregion

    #region Status

    /// <summary>
    /// Entry type: System, Manual, Adjustment.
    /// </summary>
    public DepreciationEntryType EntryType { get; set; } = DepreciationEntryType.System;

    /// <summary>
    /// Notes about this entry.
    /// </summary>
    public string? Notes { get; set; }

    #endregion

    #region Denormalized

    /// <summary>
    /// Vehicle name for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleName { get; set; }

    #endregion

    public override int GetId() => this.DepreciationEntryId;
    public override void SetId(int value) => this.DepreciationEntryId = value;
}
