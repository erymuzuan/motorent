using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class InvoiceService
{
    private readonly RentalDataContext m_context;

    public InvoiceService(RentalDataContext context)
    {
        m_context = context;
    }

    public async Task<InvoiceData?> GenerateInvoiceAsync(int rentalId)
    {
        // Load rental
        var rental = await m_context.LoadOneAsync<Rental>(r => r.RentalId == rentalId);
        if (rental == null) return null;

        // Load renter
        var renter = await m_context.LoadOneAsync<Renter>(r => r.RenterId == rental.RenterId);

        // Load motorbike
        var motorbike = await m_context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == rental.MotorbikeId);

        // Load shop
        var shop = await m_context.LoadOneAsync<Shop>(s => s.ShopId == rental.ShopId);

        // Load deposit
        var deposit = await m_context.LoadOneAsync<Deposit>(d => d.RentalId == rentalId);

        // Load insurance if applicable
        Insurance? insurance = null;
        if (rental.InsuranceId.HasValue)
        {
            insurance = await m_context.LoadOneAsync<Insurance>(i => i.InsuranceId == rental.InsuranceId);
        }

        // Load accessories
        var accessories = await m_context.LoadAsync(
            m_context.RentalAccessories.Where(ra => ra.RentalId == rentalId),
            page: 1, size: 100, includeTotalRows: false);

        var accessoryIds = accessories.ItemCollection.Select(ra => ra.AccessoryId).ToList();
        var accessoryDetails = new List<Accessory>();
        if (accessoryIds.Count != 0)
        {
            var accResult = await m_context.LoadAsync(
                m_context.Accessories.Where(a => accessoryIds.Contains(a.AccessoryId)),
                page: 1, size: 100, includeTotalRows: false);
            accessoryDetails = accResult.ItemCollection.ToList();
        }

        // Load payments
        var payments = await m_context.LoadAsync(
            m_context.Payments.Where(p => p.RentalId == rentalId),
            page: 1, size: 100, includeTotalRows: false);

        // Calculate values
        int rentalDays = Math.Max(1, (int)(rental.ExpectedEndDate.Date - rental.StartDate.Date).TotalDays + 1);
        decimal motorbikeTotal = rental.DailyRate * rentalDays;
        decimal insuranceTotal = insurance?.DailyRate * rentalDays ?? 0;
        decimal accessoriesTotal = accessories.ItemCollection.Sum(ra =>
        {
            var acc = accessoryDetails.FirstOrDefault(a => a.AccessoryId == ra.AccessoryId);
            return acc != null && !acc.IsIncluded ? acc.DailyRate * ra.Quantity * rentalDays : 0;
        });

        var invoice = new InvoiceData
        {
            InvoiceNumber = $"INV-{rental.RentalId:D6}",
            InvoiceDate = DateTimeOffset.Now,
            RentalId = rentalId,

            // Shop info
            ShopName = shop?.Name ?? "MotoRent",
            ShopAddress = shop?.Address ?? "",
            ShopPhone = shop?.Phone ?? "",
            ShopEmail = shop?.Email ?? "",

            // Customer info
            CustomerName = renter?.FullName ?? "Unknown",
            CustomerPhone = renter?.Phone ?? "",
            CustomerEmail = renter?.Email ?? "",
            CustomerPassport = renter?.PassportNo ?? renter?.NationalIdNo ?? "",

            // Rental info
            MotorbikeBrand = motorbike?.Brand ?? "",
            MotorbikeModel = motorbike?.Model ?? "",
            MotorbikeLicensePlate = motorbike?.LicensePlate ?? "",
            StartDate = rental.StartDate,
            EndDate = rental.ActualEndDate ?? rental.ExpectedEndDate,
            RentalDays = rentalDays,
            DailyRate = rental.DailyRate,

            // Line items
            LineItems =
            [
                new InvoiceLineItem
                {
                    Description = $"Motorbike rental - {motorbike?.Brand} {motorbike?.Model}",
                    Quantity = rentalDays,
                    UnitPrice = rental.DailyRate,
                    Total = motorbikeTotal
                }
            ],

            // Payment info
            DepositAmount = deposit?.Amount ?? 0,
            DepositType = deposit?.DepositType ?? "N/A",
            DepositStatus = deposit?.Status ?? "N/A",

            // Totals
            Subtotal = motorbikeTotal + insuranceTotal + accessoriesTotal,
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

        // Add insurance line item if applicable
        if (insurance != null)
        {
            invoice.LineItems.Add(new InvoiceLineItem
            {
                Description = $"Insurance - {insurance.Name}",
                Quantity = rentalDays,
                UnitPrice = insurance.DailyRate,
                Total = insuranceTotal
            });
        }

        // Add accessory line items
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
                    Total = acc.DailyRate * ra.Quantity * rentalDays
                });
            }
        }

        return invoice;
    }
}

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

    // Customer info
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPassport { get; set; } = "";

    // Rental info
    public string MotorbikeBrand { get; set; } = "";
    public string MotorbikeModel { get; set; } = "";
    public string MotorbikeLicensePlate { get; set; } = "";
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int RentalDays { get; set; }
    public decimal DailyRate { get; set; }

    // Line items
    public List<InvoiceLineItem> LineItems { get; set; } = [];

    // Deposit info
    public decimal DepositAmount { get; set; }
    public string DepositType { get; set; } = "";
    public string DepositStatus { get; set; } = "";

    // Totals
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }

    // Payments
    public List<InvoicePayment> Payments { get; set; } = [];

    // Status
    public string RentalStatus { get; set; } = "";

    // Calculated
    public decimal TotalPaid => Payments.Where(p => p.PaymentType != "Refund").Sum(p => p.Amount);
    public decimal TotalRefunded => Payments.Where(p => p.PaymentType == "Refund").Sum(p => p.Amount);
    public decimal BalanceDue => Total - TotalPaid + TotalRefunded;
}

public class InvoiceLineItem
{
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
}

public class InvoicePayment
{
    public string PaymentType { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTimeOffset? PaidOn { get; set; }
    public string? TransactionRef { get; set; }
}
