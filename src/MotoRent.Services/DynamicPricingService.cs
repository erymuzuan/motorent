using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Settings;

namespace MotoRent.Services;

/// <summary>
/// Service for dynamic pricing based on seasons, events, and day-of-week rules.
/// </summary>
public class DynamicPricingService
{
    private readonly RentalDataContext m_context;
    private readonly ISettingConfig m_settings;

    /// <summary>
    /// Setting key for dynamic pricing enabled flag.
    /// </summary>
    public const string SettingKeyEnabled = "DynamicPricing.Enabled";

    /// <summary>
    /// Setting key for showing multiplier on invoice.
    /// </summary>
    public const string SettingKeyShowOnInvoice = "DynamicPricing.ShowOnInvoice";

    /// <summary>
    /// Setting key for the applied preset name.
    /// </summary>
    public const string SettingKeyAppliedPreset = "DynamicPricing.AppliedPreset";

    public DynamicPricingService(RentalDataContext context, ISettingConfig settings)
    {
        m_context = context;
        m_settings = settings;
    }

    /// <summary>
    /// Checks if dynamic pricing is enabled for the current shop.
    /// </summary>
    public async Task<bool> IsEnabledAsync()
        => await m_settings.GetBoolAsync(SettingKeyEnabled);

    /// <summary>
    /// Enables or disables dynamic pricing for the current shop.
    /// </summary>
    public async Task SetEnabledAsync(bool enabled)
        => await m_settings.SetValueAsync(SettingKeyEnabled, enabled);

    /// <summary>
    /// Calculates the adjusted rate based on active pricing rules.
    /// Returns the base rate unchanged if dynamic pricing is disabled.
    /// </summary>
    public async Task<PricingCalculation> CalculateAdjustedRateAsync(
        int shopId,
        decimal baseRate,
        DateOnly rentalDate,
        string? vehicleType = null,
        int? vehicleId = null)
    {
        // Check if dynamic pricing is enabled
        var isEnabled = await m_settings.GetBoolAsync(SettingKeyEnabled);

        if (!isEnabled)
        {
            return new PricingCalculation
            {
                BaseRate = baseRate,
                AdjustedRate = baseRate,
                Multiplier = 1.0m,
                AppliedRuleName = null,
                AppliedRuleType = null
            };
        }

        // Load active rules for this shop
        var rules = await LoadActiveRulesAsync(shopId, rentalDate, vehicleType, vehicleId);

        if (rules.Count == 0)
        {
            return new PricingCalculation
            {
                BaseRate = baseRate,
                AdjustedRate = baseRate,
                Multiplier = 1.0m,
                AppliedRuleName = null,
                AppliedRuleType = null
            };
        }

        // Select highest priority rule
        var applicableRule = rules.OrderByDescending(r => r.Priority).First();

        // Calculate adjusted rate
        var adjustedRate = baseRate * applicableRule.Multiplier;

        // Apply min/max bounds
        if (applicableRule.MinRate.HasValue && adjustedRate < applicableRule.MinRate.Value)
        {
            adjustedRate = applicableRule.MinRate.Value;
        }

        if (applicableRule.MaxRate.HasValue && adjustedRate > applicableRule.MaxRate.Value)
        {
            adjustedRate = applicableRule.MaxRate.Value;
        }

        return new PricingCalculation
        {
            BaseRate = baseRate,
            AdjustedRate = adjustedRate,
            Multiplier = applicableRule.Multiplier,
            AppliedRuleName = applicableRule.Name,
            AppliedRuleType = applicableRule.RuleType.ToString()
        };
    }

    /// <summary>
    /// Loads all active pricing rules that apply to the given date and filters.
    /// </summary>
    private async Task<List<PricingRule>> LoadActiveRulesAsync(
        int shopId,
        DateOnly rentalDate,
        string? vehicleType,
        int? vehicleId)
    {
        var query = m_context.CreateQuery<PricingRule>()
            .Where(r => r.ShopId == shopId && r.IsActive);

        var allRules = await m_context.LoadAsync(query, size: 500);

        var applicableRules = new List<PricingRule>();

        foreach (var rule in allRules.ItemCollection)
        {
            // Check if rule applies to the rental date
            if (!RuleAppliesToDate(rule, rentalDate))
                continue;

            // Check vehicle type filter
            if (!string.IsNullOrEmpty(rule.VehicleType) && rule.VehicleType != vehicleType)
                continue;

            // Check specific vehicle filter
            if (rule.VehicleId.HasValue && rule.VehicleId != vehicleId)
                continue;

            applicableRules.Add(rule);
        }

        return applicableRules;
    }

    /// <summary>
    /// Checks if a pricing rule applies to the given date.
    /// </summary>
    private bool RuleAppliesToDate(PricingRule rule, DateOnly rentalDate)
    {
        // Handle day-of-week rules
        if (rule.RuleType == PricingRuleType.DayOfWeek)
        {
            if (rule.ApplicableDayOfWeek.HasValue)
            {
                return rentalDate.DayOfWeek == rule.ApplicableDayOfWeek.Value;
            }
        }

        // Handle recurring rules (yearly events)
        if (rule.IsRecurring && rule.RecurringMonth.HasValue)
        {
            // For recurring rules, check month/day regardless of year
            var ruleStartMonth = rule.StartDate.Month;
            var ruleStartDay = rule.StartDate.Day;
            var ruleEndMonth = rule.EndDate.Month;
            var ruleEndDay = rule.EndDate.Day;

            // Simple case: same year range (e.g., Apr 13-15)
            if (ruleStartMonth <= ruleEndMonth)
            {
                return rentalDate.Month >= ruleStartMonth && rentalDate.Month <= ruleEndMonth &&
                       (rentalDate.Month != ruleStartMonth || rentalDate.Day >= ruleStartDay) &&
                       (rentalDate.Month != ruleEndMonth || rentalDate.Day <= ruleEndDay);
            }
            else
            {
                // Cross-year range (e.g., Dec 15 - Feb 28)
                return (rentalDate.Month >= ruleStartMonth &&
                        (rentalDate.Month != ruleStartMonth || rentalDate.Day >= ruleStartDay)) ||
                       (rentalDate.Month <= ruleEndMonth &&
                        (rentalDate.Month != ruleEndMonth || rentalDate.Day <= ruleEndDay));
            }
        }

        // Non-recurring: check exact date range
        return rentalDate >= rule.StartDate && rentalDate <= rule.EndDate;
    }

    /// <summary>
    /// Gets all pricing rules for a shop.
    /// </summary>
    public async Task<List<PricingRule>> GetRulesAsync(int shopId, bool activeOnly = false)
    {
        var query = m_context.CreateQuery<PricingRule>()
            .Where(r => r.ShopId == shopId);

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        query = query.OrderByDescending(r => r.Priority);

        var result = await m_context.LoadAsync(query, size: 500);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets a pricing rule by ID.
    /// </summary>
    public async Task<PricingRule?> GetRuleByIdAsync(int pricingRuleId)
    {
        return await m_context.LoadOneAsync<PricingRule>(r => r.PricingRuleId == pricingRuleId);
    }

    /// <summary>
    /// Gets a multiplier calendar for a month showing the effective multiplier for each day.
    /// </summary>
    public async Task<Dictionary<DateOnly, decimal>> GetMultiplierCalendarAsync(
        int shopId,
        int year,
        int month,
        string? vehicleType = null)
    {
        var calendar = new Dictionary<DateOnly, decimal>();

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var baseRate = 1.0m; // We just need the multiplier

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            var calc = await CalculateAdjustedRateAsync(shopId, baseRate, date, vehicleType);
            calendar[date] = calc.Multiplier;
        }

        return calendar;
    }

    /// <summary>
    /// Gets active rules for a date range (useful for calendar display).
    /// </summary>
    public async Task<List<PricingRule>> GetActiveRulesForDateRangeAsync(
        int shopId,
        DateOnly start,
        DateOnly end)
    {
        var rules = await GetRulesAsync(shopId, activeOnly: true);

        return rules.Where(r =>
        {
            // Check if rule overlaps with date range
            if (r.IsRecurring)
            {
                // For recurring rules, we assume they might apply
                return true;
            }

            // Non-recurring: check overlap
            return r.EndDate >= start && r.StartDate <= end;
        }).ToList();
    }
}

/// <summary>
/// Result of a dynamic pricing calculation.
/// </summary>
public class PricingCalculation
{
    /// <summary>Original base rate before adjustment.</summary>
    public decimal BaseRate { get; set; }

    /// <summary>Adjusted rate after applying the multiplier.</summary>
    public decimal AdjustedRate { get; set; }

    /// <summary>Multiplier applied (1.0 = no change, 1.5 = +50%, 0.8 = -20%).</summary>
    public decimal Multiplier { get; set; } = 1.0m;

    /// <summary>Name of the applied pricing rule, if any.</summary>
    public string? AppliedRuleName { get; set; }

    /// <summary>Type of the applied pricing rule, if any.</summary>
    public string? AppliedRuleType { get; set; }

    /// <summary>Whether a pricing rule was applied.</summary>
    public bool HasAdjustment => Multiplier != 1.0m;

    /// <summary>Percentage change as a formatted string (e.g., "+50%", "-20%").</summary>
    public string PercentageChange
    {
        get
        {
            var pct = (Multiplier - 1.0m) * 100;
            return pct >= 0 ? $"+{pct:N0}%" : $"{pct:N0}%";
        }
    }
}
