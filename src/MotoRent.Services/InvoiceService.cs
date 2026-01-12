using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services;

/// <summary>
/// Service for generating invoices from rentals.
/// Supports all vehicle types including interval-based rentals (jet skis)
/// and optional driver/guide fees (boats, vans).
/// </summary>
public class InvoiceService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    public async Task<InvoiceData?> GenerateInvoiceAsync(int rentalId)
    {
        // Load rental
        var rental = await this.Context.LoadOneAsync<Rental>(r => r.RentalId == rentalId);
        if (rental == null) return null;

        // Load renter
        var renter = await this.Context.LoadOneAsync<Renter>(r => r.RenterId == rental.RenterId);

        // Load vehicle (try new Vehicle entity first, fall back to Motorbike for backward compat)
        Vehicle? vehicle = null;
        Motorbike? motorbike = null;

        if (rental.VehicleId > 0)
        {
            vehicle = await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == rental.VehicleId);
        }
        else if (rental.MotorbikeId > 0)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            motorbike = await this.Context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == rental.MotorbikeId);
#pragma warning restore CS0618
        }

        // Load shops
        var rentedFromShop = await this.Context.LoadOneAsync<Shop>(s => s.ShopId == rental.RentedFromShopId);
        Shop? returnedToShop = null;
        if (rental.ReturnedToShopId.HasValue && rental.ReturnedToShopId.Value != rental.RentedFromShopId)
        {
            returnedToShop = await this.Context.LoadOneAsync<Shop>(s => s.ShopId == rental.ReturnedToShopId.Value);
        }

        // Load deposit
        var deposit = await this.Context.LoadOneAsync<Deposit>(d => d.RentalId == rentalId);

        // Load insurance if applicable
        Insurance? insurance = null;
        if (rental.InsuranceId.HasValue)
        {
            insurance = await this.Context.LoadOneAsync<Insurance>(i => i.InsuranceId == rental.InsuranceId);
        }

        // Load accessories
        var accessories = await this.Context.LoadAsync(
            this.Context.CreateQuery<RentalAccessory>().Where(ra => ra.RentalId == rentalId),
            page: 1, size: 100, includeTotalRows: false);

        var accessoryIds = accessories.ItemCollection.Select(ra => ra.AccessoryId).ToList();
        var accessoryDetails = new List<Accessory>();
        if (accessoryIds.Count != 0)
        {
            var accResult = await this.Context.LoadAsync(
                this.Context.CreateQuery<Accessory>().Where(a => accessoryIds.IsInList(a.AccessoryId)),
                page: 1, size: 100, includeTotalRows: false);
            accessoryDetails = accResult.ItemCollection.ToList();
        }

        // Load payments
        var payments = await this.Context.LoadAsync(
            this.Context.CreateQuery<Payment>().Where(p => p.RentalId == rentalId),
            page: 1, size: 100, includeTotalRows: false);

        // Determine duration type and calculate accordingly
        var durationType = rental.DurationType;
        var isIntervalRental = durationType == RentalDurationType.FixedInterval;

        // Calculate rental duration and vehicle total
        int rentalDays = 0;
        int? intervalMinutes = null;
        decimal vehicleTotal;
        string durationDisplay;

        if (isIntervalRental)
        {
            intervalMinutes = rental.IntervalMinutes ?? 60;
            vehicleTotal = rental.RentalRate;
            durationDisplay = GetIntervalDisplayText(intervalMinutes.Value);
        }
        else
        {
            rentalDays = Math.Max(1, (int)(rental.ExpectedEndDate.Date - rental.StartDate.Date).TotalDays + 1);
            vehicleTotal = rental.RentalRate * rentalDays;
            durationDisplay = $"{rentalDays} day{(rentalDays > 1 ? "s" : "")}";
        }

        // Calculate other totals (only for daily rentals)
        decimal insuranceTotal = 0;
        decimal accessoriesTotal = 0;

        if (!isIntervalRental)
        {
            insuranceTotal = insurance?.DailyRate * rentalDays ?? 0;
            accessoriesTotal = accessories.ItemCollection.Sum(ra =>
            {
                var acc = accessoryDetails.FirstOrDefault(a => a.AccessoryId == ra.AccessoryId);
                return acc != null && !acc.IsIncluded ? acc.DailyRate * ra.Quantity * rentalDays : 0;
            });
        }

        // Driver and guide fees (from rental record)
        decimal driverFee = rental.DriverFee;
        decimal guideFee = rental.GuideFee;

        // Get vehicle info
        var vehicleBrand = vehicle?.Brand ?? motorbike?.Brand ?? "";
        var vehicleModel = vehicle?.Model ?? motorbike?.Model ?? "";
        var vehicleLicensePlate = vehicle?.LicensePlate ?? motorbike?.LicensePlate ?? "";
        var vehicleType = vehicle?.VehicleType ?? VehicleType.Motorbike;

        var invoice = new InvoiceData
        {
            InvoiceNumber = $"INV-{rental.RentalId:D6}",
            InvoiceDate = DateTimeOffset.Now,
            RentalId = rentalId,

            // Shop info
            ShopName = rentedFromShop?.Name ?? "MotoRent",
            ShopAddress = rentedFromShop?.Address ?? "",
            ShopPhone = rentedFromShop?.Phone ?? "",
            ShopEmail = rentedFromShop?.Email ?? "",

            // Return shop info (for cross-shop returns)
            ReturnShopName = returnedToShop?.Name,
            ReturnShopAddress = returnedToShop?.Address,
            IsCrossShopReturn = returnedToShop != null,

            // Customer info
            CustomerName = renter?.FullName ?? "Unknown",
            CustomerPhone = renter?.Phone ?? "",
            CustomerEmail = renter?.Email ?? "",
            CustomerPassport = renter?.PassportNo ?? renter?.NationalIdNo ?? "",

            // Vehicle info (updated from Motorbike)
            VehicleBrand = vehicleBrand,
            VehicleModel = vehicleModel,
            VehicleLicensePlate = vehicleLicensePlate,
            VehicleType = vehicleType,
            VehicleTypeDisplay = GetVehicleTypeDisplayText(vehicleType),
            StartDate = rental.StartDate,
            EndDate = rental.ActualEndDate ?? rental.ExpectedEndDate,

            // Duration info
            DurationType = durationType,
            RentalDays = rentalDays,
            IntervalMinutes = intervalMinutes,
            DurationDisplay = durationDisplay,
            RentalRate = rental.RentalRate,

            // Backward compatibility
            DailyRate = rental.RentalRate,

            // Line items
            LineItems = [],

            // Payment info
            DepositAmount = deposit?.Amount ?? 0,
            DepositType = deposit?.DepositType ?? "N/A",
            DepositStatus = deposit?.Status ?? "N/A",

            // Driver/Guide info
            IncludeDriver = rental.IncludeDriver,
            IncludeGuide = rental.IncludeGuide,
            DriverFee = driverFee,
            GuideFee = guideFee,

            // Totals
            Subtotal = vehicleTotal + insuranceTotal + accessoriesTotal + driverFee + guideFee,
            Tax = 0, // No VAT in Thailand for small businesses
            Total = rental.TotalAmount,

            // Payment records
            Payments = payments.ItemCollection.Select(p => new InvoicePayment
            {
                PaymentType = p.PaymentType ?? "Unknown",
                PaymentMethod = p.PaymentMethod ?? "Unknown",
                Amount = p.Amount,
                PaidOn = p.PaidOn,
                TransactionRef = p.TransactionRef
            }).ToList(),

            // Status
            RentalStatus = rental.Status ?? "Unknown"
        };

        // Add vehicle line item
        string vehicleDescription = isIntervalRental
            ? $"{GetVehicleTypeDisplayText(vehicleType)} rental - {vehicleBrand} {vehicleModel} ({durationDisplay})"
            : $"{GetVehicleTypeDisplayText(vehicleType)} rental - {vehicleBrand} {vehicleModel}";

        invoice.LineItems.Add(new InvoiceLineItem
        {
            Description = vehicleDescription,
            Quantity = isIntervalRental ? 1 : rentalDays,
            UnitPrice = rental.RentalRate,
            Total = vehicleTotal,
            ItemType = InvoiceItemType.Vehicle
        });

        // Add driver fee line item if applicable
        if (rental.IncludeDriver && driverFee > 0)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = "Driver service",
                Quantity = rentalDays > 0 ? rentalDays : 1,
                UnitPrice = rentalDays > 0 ? driverFee / rentalDays : driverFee,
                Total = driverFee,
                ItemType = InvoiceItemType.Driver
            });
        }

        // Add guide fee line item if applicable
        if (rental.IncludeGuide && guideFee > 0)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = "Tourist guide service",
                Quantity = rentalDays > 0 ? rentalDays : 1,
                UnitPrice = rentalDays > 0 ? guideFee / rentalDays : guideFee,
                Total = guideFee,
                ItemType = InvoiceItemType.Guide
            });
        }

        // Add insurance line item if applicable (daily rentals only)
        if (insurance != null && !isIntervalRental)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = $"Insurance - {insurance.Name}",
                Quantity = rentalDays,
                UnitPrice = insurance.DailyRate,
                Total = insuranceTotal,
                ItemType = InvoiceItemType.Insurance
            });
        }

        // Add accessory line items (daily rentals only)
        if (!isIntervalRental)
        {
            foreach (var ra in accessories.ItemCollection)
            {
                var acc = accessoryDetails.FirstOrDefault(a => a.AccessoryId == ra.AccessoryId);
                if (acc != null && !acc.IsIncluded)
                {
                    invoice.LineItems.Add(new InvoiceLineItem
                    {
                        Description = $"Accessory - {acc.Name}",
                        Quantity = ra.Quantity * rentalDays,
                        UnitPrice = acc.DailyRate,
                        Total = acc.DailyRate * ra.Quantity * rentalDays,
                        ItemType = InvoiceItemType.Accessory
                    });
                }
            }
        }

        return invoice;
    }

    /// <summary>
    /// Gets display text for interval durations.
    /// </summary>
    private static string GetIntervalDisplayText(int minutes)
    {
        return minutes switch
        {
            15 => "15 minutes",
            30 => "30 minutes",
            60 => "1 hour",
            _ => $"{minutes} minutes"
        };
    }

    /// <summary>
    /// Gets display text for vehicle types.
    /// </summary>
    private static string GetVehicleTypeDisplayText(VehicleType vehicleType)
    {
        return vehicleType switch
        {
            VehicleType.Motorbike => "Motorbike",
            VehicleType.Car => "Car",
            VehicleType.JetSki => "Jet Ski",
            VehicleType.Boat => "Boat",
            VehicleType.Van => "Van",
            _ => "Vehicle"
        };
    }
}

/// <summary>
/// Invoice data transfer object with support for all vehicle types and rental configurations.
/// </summary>
public class InvoiceData
{
    public string InvoiceNumber { get; set; } = "";
    public DateTimeOffset InvoiceDate { get; set; }
    public int RentalId { get; set; }

    // Shop info
    public string ShopName { get; set; } = "";
    public string ShopAddress { get; set; } = "";
    public string ShopPhone { get; set; } = "";
    public string ShopEmail { get; set; } = "";

    // Return shop info (for cross-shop returns)
    public string? ReturnShopName { get; set; }
    public string? ReturnShopAddress { get; set; }
    public bool IsCrossShopReturn { get; set; }

    // Customer info
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPassport { get; set; } = "";

    // Vehicle info (replaces Motorbike)
    public string VehicleBrand { get; set; } = "";
    public string VehicleModel { get; set; } = "";
    public string VehicleLicensePlate { get; set; } = "";
    public VehicleType VehicleType { get; set; }
    public string VehicleTypeDisplay { get; set; } = "";

    // Backward compatibility aliases
    [Obsolete("Use VehicleBrand instead")]
    public string MotorbikeBrand { get => VehicleBrand; set => VehicleBrand = value; }
    [Obsolete("Use VehicleModel instead")]
    public string MotorbikeModel { get => VehicleModel; set => VehicleModel = value; }
    [Obsolete("Use VehicleLicensePlate instead")]
    public string MotorbikeLicensePlate { get => VehicleLicensePlate; set => VehicleLicensePlate = value; }

    // Rental dates
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }

    // Duration info
    public RentalDurationType DurationType { get; set; }
    public int RentalDays { get; set; }
    public int? IntervalMinutes { get; set; }
    public string DurationDisplay { get; set; } = "";
    public decimal RentalRate { get; set; }
    [Obsolete("Use RentalRate instead")]
    public decimal DailyRate { get; set; }

    // Line items
    public List<InvoiceLineItem> LineItems { get; set; } = [];

    // Deposit info
    public decimal DepositAmount { get; set; }
    public string DepositType { get; set; } = "";
    public string DepositStatus { get; set; } = "";

    // Driver/Guide info
    public bool IncludeDriver { get; set; }
    public bool IncludeGuide { get; set; }
    public decimal DriverFee { get; set; }
    public decimal GuideFee { get; set; }

    // Totals
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    // Payments
    public List<InvoicePayment> Payments { get; set; } = [];

    // Status
    public string RentalStatus { get; set; } = "";

    // Calculated
    public decimal TotalPaid => this.Payments.Where(p => p.PaymentType != "Refund").Sum(p => p.Amount);
    public decimal TotalRefunded => this.Payments.Where(p => p.PaymentType == "Refund").Sum(p => p.Amount);
    public decimal BalanceDue => this.Total - this.TotalPaid + this.TotalRefunded;
}

/// <summary>
/// Invoice line item with type categorization.
/// </summary>
public class InvoiceLineItem
{
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public InvoiceItemType ItemType { get; set; }
}

/// <summary>
/// Type of invoice line item for grouping and display.
/// </summary>
public enum InvoiceItemType
{
    Vehicle,
    Insurance,
    Accessory,
    Driver,
    Guide,
    ExtraCharge,
    Discount
}

/// <summary>
/// Payment record for invoice.
/// </summary>
public class InvoicePayment
{
    public string PaymentType { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTimeOffset? PaidOn { get; set; }
    public string? TransactionRef { get; set; }
}
