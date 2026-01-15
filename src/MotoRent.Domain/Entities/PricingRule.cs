namespace MotoRent.Domain.Entities;

/// <summary>
/// Defines a pricing adjustment rule with date range and multiplier for dynamic pricing.
/// </summary>
public class PricingRule : Entity
{
    public int PricingRuleId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; } = string.Empty;           // "High Season 2025"
    public string? Description { get; set; }
    public PricingRuleType RuleType { get; set; } = PricingRuleType.Season;

    // Date range (required)
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    // Yearly recurrence (optional)
    public bool IsRecurring { get; set; }                      // Repeats annually
    public int? RecurringMonth { get; set; }                   // For yearly events (1-12)
    public int? RecurringDay { get; set; }                     // Day of month

    // Day of week (for DayOfWeek rule type)
    public DayOfWeek? ApplicableDayOfWeek { get; set; }        // For weekend premium rules

    // Pricing
    public decimal Multiplier { get; set; } = 1.0m;            // 1.5 = +50%, 0.8 = -20%
    public decimal? MinRate { get; set; }                      // Floor price
    public decimal? MaxRate { get; set; }                      // Ceiling price

    // Vehicle filtering (optional)
    public string? VehicleType { get; set; }                   // "Motorbike", "Car", or null for all
    public int? VehicleId { get; set; }                        // Specific vehicle only

    // Priority (higher wins on overlap)
    public int Priority { get; set; } = 10;

    // Status
    public bool IsActive { get; set; } = true;

    public override int GetId() => this.PricingRuleId;
    public override void SetId(int value) => this.PricingRuleId = value;
}
