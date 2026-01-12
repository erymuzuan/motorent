using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for calculating rental pricing based on vehicle type and duration.
/// </summary>
public class RentalPricingService(OperatingHoursService? hoursService = null)
{
    private OperatingHoursService? HoursService { get; } = hoursService;
    /// <summary>
    /// Calculates rental pricing based on vehicle and rental configuration.
    /// </summary>
    public RentalPricing CalculatePricing(
        Vehicle vehicle,
        RentalDurationType durationType,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int? intervalMinutes = null,
        bool includeDriver = false,
        bool includeGuide = false,
        Insurance? insurance = null,
        List<AccessoryWithQuantity>? accessories = null)
    {
        var pricing = new RentalPricing
        {
            DurationType = durationType,
            StartDate = startDate,
            EndDate = endDate,
            VehicleType = vehicle.VehicleType
        };

        if (durationType == RentalDurationType.Daily)
        {
            CalculateDailyPricing(pricing, vehicle, startDate, endDate, includeDriver, includeGuide, insurance, accessories);
        }
        else // FixedInterval
        {
            CalculateIntervalPricing(pricing, vehicle, intervalMinutes ?? 60);
        }

        return pricing;
    }

    /// <summary>
    /// Calculates pricing for daily rentals (motorbikes, cars, boats, vans).
    /// </summary>
    private void CalculateDailyPricing(
        RentalPricing pricing,
        Vehicle vehicle,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        bool includeDriver,
        bool includeGuide,
        Insurance? insurance,
        List<AccessoryWithQuantity>? accessories)
    {
        int days = Math.Max(1, (int)(endDate.Date - startDate.Date).TotalDays + 1);
        pricing.RentalDays = days;
        pricing.RentalRate = vehicle.DailyRate;
        pricing.VehicleTotal = vehicle.DailyRate * days;

        // Driver fee (daily)
        if (includeDriver && vehicle.DriverDailyFee.HasValue && vehicle.DriverDailyFee > 0)
        {
            pricing.DriverFee = vehicle.DriverDailyFee.Value * days;
            pricing.IncludeDriver = true;
        }

        // Guide fee (daily)
        if (includeGuide && vehicle.GuideDailyFee.HasValue && vehicle.GuideDailyFee > 0)
        {
            pricing.GuideFee = vehicle.GuideDailyFee.Value * days;
            pricing.IncludeGuide = true;
        }

        // Insurance (daily)
        if (insurance != null)
        {
            pricing.InsuranceTotal = insurance.DailyRate * days;
            pricing.InsuranceName = insurance.Name;
        }

        // Accessories (daily)
        if (accessories != null && accessories.Count > 0)
        {
            pricing.AccessoriesTotal = accessories
                .Where(a => !a.Accessory.IsIncluded)
                .Sum(a => a.Accessory.DailyRate * a.Quantity * days);
        }

        // Calculate totals
        pricing.SubTotal = pricing.VehicleTotal + pricing.InsuranceTotal +
                           pricing.AccessoriesTotal + pricing.DriverFee + pricing.GuideFee;
        pricing.DepositAmount = vehicle.DepositAmount;
        pricing.Total = pricing.SubTotal;
    }

    /// <summary>
    /// Calculates pricing for fixed interval rentals (jet skis).
    /// </summary>
    private void CalculateIntervalPricing(
        RentalPricing pricing,
        Vehicle vehicle,
        int intervalMinutes)
    {
        pricing.IntervalMinutes = intervalMinutes;
        pricing.RentalDays = 0; // Not applicable

        decimal rate = intervalMinutes switch
        {
            15 => vehicle.Rate15Min ?? 0,
            30 => vehicle.Rate30Min ?? 0,
            60 => vehicle.Rate1Hour ?? 0,
            _ => vehicle.Rate1Hour ?? vehicle.DailyRate / 8 // Fallback to hourly from daily
        };

        pricing.RentalRate = rate;
        pricing.VehicleTotal = rate; // Single interval, single charge

        // Jet skis typically don't have insurance/accessories/driver
        pricing.SubTotal = pricing.VehicleTotal;
        pricing.DepositAmount = vehicle.DepositAmount;
        pricing.Total = pricing.SubTotal;
    }

    /// <summary>
    /// Validates rental configuration based on vehicle type.
    /// </summary>
    public ValidationResult ValidateRental(
        Vehicle vehicle,
        RentalDurationType durationType,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int? intervalMinutes = null)
    {
        var errors = new List<string>();

        // Rule 1: Duration type must match vehicle type
        if (vehicle.UsesIntervalPricing && durationType != RentalDurationType.FixedInterval)
        {
            errors.Add($"{vehicle.VehicleType} vehicles must use interval-based rentals");
        }

        if (!vehicle.UsesIntervalPricing && durationType == RentalDurationType.FixedInterval)
        {
            errors.Add($"{vehicle.VehicleType} vehicles cannot use interval-based rentals");
        }

        // Rule 2: Jet ski cannot span multiple days
        if (vehicle.VehicleType == VehicleType.JetSki)
        {
            if (startDate.Date != endDate.Date)
            {
                errors.Add("Jet ski rentals must be completed within the same day");
            }
        }

        // Rule 3: Valid interval for fixed-interval rentals
        if (durationType == RentalDurationType.FixedInterval)
        {
            if (!intervalMinutes.HasValue)
            {
                errors.Add("Interval minutes is required for fixed-interval rentals");
            }
            else if (intervalMinutes != 15 && intervalMinutes != 30 && intervalMinutes != 60)
            {
                errors.Add("Invalid interval. Must be 15, 30, or 60 minutes");
            }
            else
            {
                // Check if the rate is configured for this interval
                decimal rate = intervalMinutes switch
                {
                    15 => vehicle.Rate15Min ?? 0,
                    30 => vehicle.Rate30Min ?? 0,
                    60 => vehicle.Rate1Hour ?? 0,
                    _ => 0
                };

                if (rate <= 0)
                {
                    errors.Add($"This vehicle does not have a rate configured for {intervalMinutes} minute intervals");
                }
            }
        }

        // Rule 4: Daily rentals must have valid date range
        if (durationType == RentalDurationType.Daily)
        {
            if (endDate <= startDate)
            {
                errors.Add("End date must be after start date");
            }

            if (startDate < DateTimeOffset.Now.AddHours(-1))
            {
                errors.Add("Start date cannot be in the past");
            }
        }

        // Rule 5: Vehicle must be available
        if (vehicle.Status != VehicleStatus.Available)
        {
            errors.Add($"Vehicle is not available (current status: {vehicle.Status})");
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    /// <summary>
    /// Calculates extra day charges for late returns.
    /// </summary>
    public decimal CalculateExtraDayCharges(
        Rental rental,
        Vehicle vehicle,
        DateTimeOffset actualEndDate)
    {
        if (rental.DurationType == RentalDurationType.FixedInterval)
        {
            // For interval rentals, calculate overtime in minutes
            var expectedEnd = rental.StartDate.AddMinutes(rental.IntervalMinutes ?? 60);
            if (actualEndDate <= expectedEnd)
                return 0;

            // Charge for extra time at the hourly rate (prorated)
            var extraMinutes = (actualEndDate - expectedEnd).TotalMinutes;
            var hourlyRate = vehicle.Rate1Hour ?? vehicle.DailyRate / 8;
            return (decimal)(extraMinutes / 60) * hourlyRate;
        }

        // Daily rental
        if (actualEndDate.Date <= rental.ExpectedEndDate.Date)
            return 0;

        int extraDays = (int)(actualEndDate.Date - rental.ExpectedEndDate.Date).TotalDays;
        return extraDays * rental.RentalRate;
    }

    /// <summary>
    /// Gets the default duration type for a vehicle.
    /// </summary>
    public RentalDurationType GetDefaultDurationType(Vehicle vehicle)
    {
        return vehicle.VehicleType == VehicleType.JetSki
            ? RentalDurationType.FixedInterval
            : RentalDurationType.Daily;
    }

    /// <summary>
    /// Gets available interval options for a vehicle.
    /// </summary>
    public List<IntervalOption> GetAvailableIntervals(Vehicle vehicle)
    {
        var options = new List<IntervalOption>();

        if (vehicle.Rate15Min.HasValue && vehicle.Rate15Min > 0)
        {
            options.Add(new IntervalOption { Minutes = 15, Rate = vehicle.Rate15Min.Value, Label = "15 Minutes" });
        }

        if (vehicle.Rate30Min.HasValue && vehicle.Rate30Min > 0)
        {
            options.Add(new IntervalOption { Minutes = 30, Rate = vehicle.Rate30Min.Value, Label = "30 Minutes" });
        }

        if (vehicle.Rate1Hour.HasValue && vehicle.Rate1Hour > 0)
        {
            options.Add(new IntervalOption { Minutes = 60, Rate = vehicle.Rate1Hour.Value, Label = "1 Hour" });
        }

        return options;
    }

    #region Location and Out-of-Hours Pricing

    /// <summary>
    /// Calculates pickup/dropoff location fees and out-of-hours fees.
    /// </summary>
    public async Task<LocationPricing> CalculateLocationPricingAsync(
        int shopId,
        DateTimeOffset pickupDateTime,
        DateTimeOffset? dropoffDateTime,
        ServiceLocation? pickupLocation,
        ServiceLocation? dropoffLocation)
    {
        var pricing = new LocationPricing();

        // Pickup location fee
        if (pickupLocation != null)
        {
            pricing.PickupLocationFee = pickupLocation.PickupFee;
            pricing.PickupLocationName = pickupLocation.Name;
        }

        // Dropoff location fee
        if (dropoffLocation != null)
        {
            pricing.DropoffLocationFee = dropoffLocation.DropoffFee;
            pricing.DropoffLocationName = dropoffLocation.Name;
        }

        // Out-of-hours fees (requires OperatingHoursService)
        if (this.HoursService != null)
        {
            // Pickup out-of-hours check
            var pickupResult = await this.HoursService.GetOutOfHoursFeeAsync(shopId, pickupDateTime);
            if (pickupResult != null)
            {
                pricing.IsOutOfHoursPickup = true;
                pricing.OutOfHoursPickupFee = pickupResult.Fee;
                pricing.OutOfHoursPickupBand = pickupResult.BandName;
            }

            // Dropoff out-of-hours check
            if (dropoffDateTime.HasValue)
            {
                var dropoffResult = await this.HoursService.GetOutOfHoursFeeAsync(shopId, dropoffDateTime.Value);
                if (dropoffResult != null)
                {
                    pricing.IsOutOfHoursDropoff = true;
                    pricing.OutOfHoursDropoffFee = dropoffResult.Fee;
                    pricing.OutOfHoursDropoffBand = dropoffResult.BandName;
                }
            }
        }

        pricing.CalculateTotal();
        return pricing;
    }

    /// <summary>
    /// Calculates location pricing synchronously using pre-loaded data.
    /// </summary>
    public LocationPricing CalculateLocationPricing(
        ShopSchedule pickupSchedule,
        ShopSchedule? dropoffSchedule,
        List<OutOfHoursBand> bands,
        TimeSpan pickupTime,
        TimeSpan? dropoffTime,
        ServiceLocation? pickupLocation,
        ServiceLocation? dropoffLocation)
    {
        var pricing = new LocationPricing();

        // Location fees
        if (pickupLocation != null)
        {
            pricing.PickupLocationFee = pickupLocation.PickupFee;
            pricing.PickupLocationName = pickupLocation.Name;
        }

        if (dropoffLocation != null)
        {
            pricing.DropoffLocationFee = dropoffLocation.DropoffFee;
            pricing.DropoffLocationName = dropoffLocation.Name;
        }

        // Out-of-hours fees (using pre-loaded data)
        if (this.HoursService != null && bands.Count > 0)
        {
            var pickupResult = this.HoursService.CalculateOutOfHoursFee(pickupSchedule, bands, pickupTime);
            if (pickupResult != null)
            {
                pricing.IsOutOfHoursPickup = true;
                pricing.OutOfHoursPickupFee = pickupResult.Fee;
                pricing.OutOfHoursPickupBand = pickupResult.BandName;
            }

            if (dropoffTime.HasValue && dropoffSchedule != null)
            {
                var dropoffResult = this.HoursService.CalculateOutOfHoursFee(dropoffSchedule, bands, dropoffTime.Value);
                if (dropoffResult != null)
                {
                    pricing.IsOutOfHoursDropoff = true;
                    pricing.OutOfHoursDropoffFee = dropoffResult.Fee;
                    pricing.OutOfHoursDropoffBand = dropoffResult.BandName;
                }
            }
        }

        pricing.CalculateTotal();
        return pricing;
    }

    #endregion
}

/// <summary>
/// Calculated rental pricing breakdown.
/// </summary>
public class RentalPricing
{
    public RentalDurationType DurationType { get; set; }
    public VehicleType VehicleType { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int RentalDays { get; set; }
    public int? IntervalMinutes { get; set; }
    public decimal RentalRate { get; set; }
    public decimal VehicleTotal { get; set; }
    public string? InsuranceName { get; set; }
    public decimal InsuranceTotal { get; set; }
    public decimal AccessoriesTotal { get; set; }
    public bool IncludeDriver { get; set; }
    public bool IncludeGuide { get; set; }
    public decimal DriverFee { get; set; }
    public decimal GuideFee { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal Total { get; set; }

    public string DurationDisplay => DurationType == RentalDurationType.Daily
        ? $"{RentalDays} day{(RentalDays > 1 ? "s" : "")}"
        : $"{IntervalMinutes} minutes";
}

/// <summary>
/// Validation result for rental configuration.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// Accessory with quantity for pricing calculation.
/// </summary>
public class AccessoryWithQuantity
{
    public Accessory Accessory { get; set; } = null!;
    public int Quantity { get; set; }
}

/// <summary>
/// Interval option for fixed-interval rentals.
/// </summary>
public class IntervalOption
{
    public int Minutes { get; set; }
    public decimal Rate { get; set; }
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Calculated location and out-of-hours pricing breakdown.
/// </summary>
public class LocationPricing
{
    // Pickup location
    public decimal PickupLocationFee { get; set; }
    public string? PickupLocationName { get; set; }

    // Dropoff location
    public decimal DropoffLocationFee { get; set; }
    public string? DropoffLocationName { get; set; }

    // Out-of-hours pickup
    public bool IsOutOfHoursPickup { get; set; }
    public decimal OutOfHoursPickupFee { get; set; }
    public string? OutOfHoursPickupBand { get; set; }

    // Out-of-hours dropoff
    public bool IsOutOfHoursDropoff { get; set; }
    public decimal OutOfHoursDropoffFee { get; set; }
    public string? OutOfHoursDropoffBand { get; set; }

    // Total
    public decimal TotalLocationFees { get; set; }

    /// <summary>
    /// Calculates the total of all location and out-of-hours fees.
    /// </summary>
    public void CalculateTotal()
    {
        this.TotalLocationFees = this.PickupLocationFee + this.DropoffLocationFee +
                                 this.OutOfHoursPickupFee + this.OutOfHoursDropoffFee;
    }

    /// <summary>
    /// Whether any location fees apply.
    /// </summary>
    public bool HasLocationFees => this.PickupLocationFee > 0 || this.DropoffLocationFee > 0;

    /// <summary>
    /// Whether any out-of-hours fees apply.
    /// </summary>
    public bool HasOutOfHoursFees => this.OutOfHoursPickupFee > 0 || this.OutOfHoursDropoffFee > 0;

    /// <summary>
    /// Whether any fees apply at all.
    /// </summary>
    public bool HasAnyFees => this.TotalLocationFees > 0;
}
