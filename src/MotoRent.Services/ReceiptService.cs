using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing receipts (check-in, check-out/settlement, booking deposits).
/// </summary>
public class ReceiptService(RentalDataContext context, ShopService shopService)
{
    private RentalDataContext Context { get; } = context;
    private ShopService ShopService { get; } = shopService;

    #region CRUD Operations

    public async Task<Receipt?> GetByIdAsync(int receiptId)
    {
        return await Context.LoadOneAsync<Receipt>(r => r.ReceiptId == receiptId);
    }

    public async Task<Receipt?> GetByReceiptNoAsync(string receiptNo)
    {
        return await Context.LoadOneAsync<Receipt>(r => r.ReceiptNo == receiptNo);
    }

    public async Task<List<Receipt>> GetByRentalIdAsync(int rentalId)
    {
        var result = await Context.LoadAsync(
            Context.CreateQuery<Receipt>().Where(r => r.RentalId == rentalId)
                .OrderByDescending(r => r.IssuedOn),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    public async Task<List<Receipt>> GetByBookingIdAsync(int bookingId)
    {
        var result = await Context.LoadAsync(
            Context.CreateQuery<Receipt>().Where(r => r.BookingId == bookingId)
                .OrderByDescending(r => r.IssuedOn),
            page: 1, size: 100, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    public async Task<LoadOperation<Receipt>> GetReceiptsAsync(
        int shopId,
        string? receiptType = null,
        string? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int? renterId = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        IQueryable<Receipt> query = Context.CreateQuery<Receipt>()
            .OrderByDescending(r => r.IssuedOn);

        // Filter by shop if specified
        if (shopId > 0)
            query = query.Where(r => r.ShopId == shopId);

        if (!string.IsNullOrWhiteSpace(receiptType))
            query = query.Where(r => r.ReceiptType == receiptType);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(r => r.Status == status);

        if (fromDate.HasValue)
            query = query.Where(r => r.IssuedOn >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.IssuedOn <= toDate.Value);

        if (renterId.HasValue)
            query = query.Where(r => r.RenterId == renterId.Value);

        var result = await Context.LoadAsync(query, page, pageSize, includeTotalRows: true);

        // Apply search term filter in memory
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            result.ItemCollection = result.ItemCollection
                .Where(r =>
                    (r.ReceiptNo?.ToLowerInvariant().Contains(term) ?? false) ||
                    (r.CustomerName?.ToLowerInvariant().Contains(term) ?? false) ||
                    (r.CustomerPhone?.ToLowerInvariant().Contains(term) ?? false) ||
                    (r.LicensePlate?.ToLowerInvariant().Contains(term) ?? false))
                .ToList();
        }

        return result;
    }

    public async Task<SubmitOperation> SaveReceiptAsync(Receipt receipt, string username)
    {
        using var session = Context.OpenSession(username);
        session.Attach(receipt);
        return await session.SubmitChanges(receipt.ReceiptId == 0 ? "Create" : "Update");
    }

    #endregion

    #region Receipt Generation

    /// <summary>
    /// Generates a check-in receipt with all charges.
    /// The accessory info is passed via a separate DTO since RentalAccessory doesn't have all display fields.
    /// </summary>
    public async Task<Receipt> GenerateCheckInReceiptAsync(
        int rentalId,
        int? tillSessionId,
        Rental rental,
        Renter renter,
        Vehicle vehicle,
        Deposit? deposit,
        Insurance? insurance,
        List<ReceiptAccessoryInfo> accessories,
        List<ReceiptPayment> payments,
        string username)
    {
        var shop = await ShopService.GetShopByIdAsync(rental.RentedFromShopId);
        var receiptNo = await GenerateReceiptNoAsync(rental.RentedFromShopId);

        var receipt = new Receipt
        {
            ReceiptNo = receiptNo,
            ReceiptType = ReceiptTypes.CheckIn,
            Status = ReceiptStatus.Issued,
            RentalId = rentalId,
            TillSessionId = tillSessionId,
            ShopId = rental.RentedFromShopId,
            RenterId = renter.RenterId,
            CustomerName = renter.FullName ?? string.Empty,
            CustomerPhone = renter.Phone,
            CustomerPassportNo = renter.PassportNo,
            IssuedOn = DateTimeOffset.Now,
            IssuedByUserName = username,
            ShopName = shop?.Name,
            ShopAddress = shop?.Address,
            ShopPhone = shop?.Phone,
            VehicleName = vehicle.DisplayName,
            LicensePlate = vehicle.LicensePlate,
            Payments = payments
        };

        var sortOrder = 0;

        // Add rental charge
        var rentalDays = (int)Math.Ceiling((rental.ExpectedEndDate - rental.StartDate).TotalDays);
        if (rentalDays < 1) rentalDays = 1;
        var rentalAmount = rental.TotalAmount;

        // Calculate insurance amount from rental (if present)
        decimal insuranceAmount = 0;
        if (insurance != null && rental.InsuranceId.HasValue)
        {
            insuranceAmount = insurance.DailyRate * rentalDays;
        }

        // Calculate base rental amount (total minus insurance)
        var baseRentalAmount = rentalAmount - insuranceAmount;

        receipt.Items.Add(new ReceiptItem
        {
            Category = ReceiptItemCategory.Rental,
            Description = vehicle.DisplayName,
            Detail = $"{rentalDays} day(s) @ {rental.RentalRate:N0}/day",
            Quantity = rentalDays,
            UnitPrice = rental.RentalRate,
            Amount = baseRentalAmount > 0 ? baseRentalAmount : rental.RentalRate * rentalDays,
            SortOrder = sortOrder++
        });

        // Add insurance if present
        if (insurance != null && insuranceAmount > 0)
        {
            receipt.Items.Add(new ReceiptItem
            {
                Category = ReceiptItemCategory.Insurance,
                Description = insurance.Name ?? "Insurance",
                Detail = $"{rentalDays} day(s) @ {insurance.DailyRate:N0}/day",
                Quantity = rentalDays,
                UnitPrice = insurance.DailyRate,
                Amount = insuranceAmount,
                SortOrder = sortOrder++
            });
        }

        // Add accessories
        foreach (var accessory in accessories)
        {
            var days = accessory.Days > 0 ? accessory.Days : rentalDays;
            receipt.Items.Add(new ReceiptItem
            {
                Category = ReceiptItemCategory.Accessory,
                Description = accessory.Name ?? "Accessory",
                Detail = accessory.Quantity > 1
                    ? $"{accessory.Quantity} x {days} day(s) @ {accessory.DailyRate:N0}/day"
                    : $"{days} day(s) @ {accessory.DailyRate:N0}/day",
                Quantity = accessory.Quantity * days,
                UnitPrice = accessory.DailyRate,
                Amount = accessory.TotalAmount,
                SortOrder = sortOrder++
            });
        }

        // Add deposit if present
        if (deposit != null && deposit.Amount > 0)
        {
            receipt.Items.Add(new ReceiptItem
            {
                Category = ReceiptItemCategory.Deposit,
                Description = "Security Deposit",
                Detail = deposit.DepositType == "Card" ? "Card Pre-authorization" : "Cash Deposit",
                Quantity = 1,
                UnitPrice = deposit.Amount,
                Amount = deposit.Amount,
                SortOrder = sortOrder++
            });
        }

        // Calculate totals
        receipt.Subtotal = receipt.Items.Where(i => i.Category != ReceiptItemCategory.Deposit).Sum(i => i.Amount);
        receipt.GrandTotal = receipt.Items.Sum(i => i.Amount);

        // Save receipt
        await SaveReceiptAsync(receipt, username);

        return receipt;
    }

    /// <summary>
    /// Generates a settlement receipt for check-out.
    /// </summary>
    public async Task<Receipt> GenerateSettlementReceiptAsync(
        int rentalId,
        int? tillSessionId,
        Rental rental,
        Renter renter,
        Vehicle vehicle,
        decimal depositHeld,
        decimal extraDaysCharge,
        int extraDays,
        decimal damageCharge,
        List<DamageReport>? damages,
        decimal locationFee,
        string? locationName,
        decimal refundAmount,
        decimal amountDue,
        List<ReceiptPayment> payments,
        string username)
    {
        var shop = await ShopService.GetShopByIdAsync(rental.RentedFromShopId);
        var receiptNo = await GenerateReceiptNoAsync(rental.RentedFromShopId);

        var receipt = new Receipt
        {
            ReceiptNo = receiptNo,
            ReceiptType = ReceiptTypes.Settlement,
            Status = ReceiptStatus.Issued,
            RentalId = rentalId,
            TillSessionId = tillSessionId,
            ShopId = rental.RentedFromShopId,
            RenterId = renter.RenterId,
            CustomerName = renter.FullName ?? string.Empty,
            CustomerPhone = renter.Phone,
            CustomerPassportNo = renter.PassportNo,
            IssuedOn = DateTimeOffset.Now,
            IssuedByUserName = username,
            ShopName = shop?.Name,
            ShopAddress = shop?.Address,
            ShopPhone = shop?.Phone,
            VehicleName = vehicle.DisplayName,
            LicensePlate = vehicle.LicensePlate,
            DepositHeld = depositHeld,
            RefundAmount = refundAmount,
            AmountDue = amountDue,
            Payments = payments
        };

        var sortOrder = 0;
        var deductionsTotal = 0m;

        // Add deposit held as starting point
        if (depositHeld > 0)
        {
            receipt.Items.Add(new ReceiptItem
            {
                Category = ReceiptItemCategory.Deposit,
                Description = "Deposit Held",
                Amount = depositHeld,
                SortOrder = sortOrder++
            });
        }

        // Add extra days charge if applicable
        if (extraDaysCharge > 0)
        {
            receipt.Items.Add(new ReceiptItem
            {
                Category = ReceiptItemCategory.ExtraDays,
                Description = "Late Return Charge",
                Detail = $"{extraDays} extra day(s) @ {rental.RentalRate:N0}/day",
                Quantity = extraDays,
                UnitPrice = rental.RentalRate,
                Amount = extraDaysCharge,
                IsDeduction = true,
                SortOrder = sortOrder++
            });
            deductionsTotal += extraDaysCharge;
        }

        // Add damage charges
        if (damageCharge > 0)
        {
            if (damages != null && damages.Any())
            {
                foreach (var damage in damages)
                {
                    receipt.Items.Add(new ReceiptItem
                    {
                        Category = ReceiptItemCategory.Damage,
                        Description = damage.Description ?? "Damage",
                        Amount = damage.EstimatedCost,
                        IsDeduction = true,
                        SortOrder = sortOrder++
                    });
                }
            }
            else
            {
                receipt.Items.Add(new ReceiptItem
                {
                    Category = ReceiptItemCategory.Damage,
                    Description = "Damage Charges",
                    Amount = damageCharge,
                    IsDeduction = true,
                    SortOrder = sortOrder++
                });
            }
            deductionsTotal += damageCharge;
        }

        // Add location fee if applicable
        if (locationFee > 0)
        {
            receipt.Items.Add(new ReceiptItem
            {
                Category = ReceiptItemCategory.LocationFee,
                Description = "Drop-off Location Fee",
                Detail = locationName,
                Amount = locationFee,
                IsDeduction = true,
                SortOrder = sortOrder++
            });
            deductionsTotal += locationFee;
        }

        // Add refund or amount due
        if (refundAmount > 0)
        {
            receipt.Items.Add(new ReceiptItem
            {
                Category = ReceiptItemCategory.DepositRefund,
                Description = "Deposit Refund",
                Amount = refundAmount,
                SortOrder = sortOrder++
            });
        }

        receipt.DeductionsTotal = deductionsTotal;
        receipt.Subtotal = depositHeld - deductionsTotal;
        receipt.GrandTotal = amountDue > 0 ? amountDue : 0;

        // Save receipt
        await SaveReceiptAsync(receipt, username);

        return receipt;
    }

    /// <summary>
    /// Generates a booking deposit receipt.
    /// </summary>
    public async Task<Receipt> GenerateBookingDepositReceiptAsync(
        int bookingId,
        Booking booking,
        Renter? renter,
        int? tillSessionId,
        List<ReceiptPayment> payments,
        string username)
    {
        var shopId = booking.PreferredShopId ?? 0;
        var shop = shopId > 0 ? await ShopService.GetShopByIdAsync(shopId) : null;
        var receiptNo = await GenerateReceiptNoAsync(shopId);

        // Get vehicle name from first booking item
        var firstItem = booking.Items.FirstOrDefault();
        var vehicleName = firstItem?.VehicleDisplayName;
        var licensePlate = firstItem?.AssignedVehiclePlate;

        var receipt = new Receipt
        {
            ReceiptNo = receiptNo,
            ReceiptType = ReceiptTypes.BookingDeposit,
            Status = ReceiptStatus.Issued,
            BookingId = bookingId,
            TillSessionId = tillSessionId,
            ShopId = shopId,
            RenterId = renter?.RenterId,
            CustomerName = renter?.FullName ?? booking.CustomerName ?? string.Empty,
            CustomerPhone = renter?.Phone ?? booking.CustomerPhone,
            CustomerPassportNo = renter?.PassportNo,
            IssuedOn = DateTimeOffset.Now,
            IssuedByUserName = username,
            ShopName = shop?.Name,
            ShopAddress = shop?.Address,
            ShopPhone = shop?.Phone,
            VehicleName = vehicleName,
            LicensePlate = licensePlate,
            Payments = payments
        };

        var sortOrder = 0;

        // Add booking deposit item
        receipt.Items.Add(new ReceiptItem
        {
            Category = ReceiptItemCategory.Deposit,
            Description = "Booking Deposit",
            Detail = $"Booking #{booking.BookingRef}",
            Quantity = 1,
            UnitPrice = booking.AmountPaid,
            Amount = booking.AmountPaid,
            SortOrder = sortOrder++
        });

        // Calculate totals
        receipt.Subtotal = booking.AmountPaid;
        receipt.GrandTotal = booking.AmountPaid;

        // Save receipt
        await SaveReceiptAsync(receipt, username);

        return receipt;
    }

    /// <summary>
    /// Generates a booking deposit receipt (simple overload for quick payments).
    /// </summary>
    public async Task<Receipt?> GenerateBookingDepositReceiptAsync(
        int bookingId,
        decimal amount,
        string paymentMethod,
        int? tillSessionId,
        string username)
    {
        var booking = await Context.LoadOneAsync<Booking>(b => b.BookingId == bookingId);
        if (booking == null)
            return null;

        // Get renter if linked
        Renter? renter = null;
        if (booking.RenterId.HasValue)
        {
            renter = await Context.LoadOneAsync<Renter>(r => r.RenterId == booking.RenterId.Value);
        }

        // Create payment record
        var payments = new List<ReceiptPayment>
        {
            new()
            {
                Method = paymentMethod,
                Amount = amount,
                AmountInBaseCurrency = amount,
                Currency = SupportedCurrencies.THB
            }
        };

        return await GenerateBookingDepositReceiptAsync(
            bookingId,
            booking,
            renter,
            tillSessionId,
            payments,
            username);
    }

    #endregion

    #region Actions

    /// <summary>
    /// Voids a receipt.
    /// </summary>
    public async Task<SubmitOperation> VoidReceiptAsync(int receiptId, string reason, string username)
    {
        var receipt = await GetByIdAsync(receiptId);
        if (receipt == null)
            return SubmitOperation.CreateFailure("Receipt not found");

        if (receipt.Status == ReceiptStatus.Voided)
            return SubmitOperation.CreateFailure("Receipt is already voided");

        receipt.Status = ReceiptStatus.Voided;
        receipt.VoidedOn = DateTimeOffset.Now;
        receipt.VoidReason = reason;

        return await SaveReceiptAsync(receipt, username);
    }

    /// <summary>
    /// Records a reprint of a receipt.
    /// </summary>
    public async Task<SubmitOperation> RecordReprintAsync(int receiptId, string username)
    {
        var receipt = await GetByIdAsync(receiptId);
        if (receipt == null)
            return SubmitOperation.CreateFailure("Receipt not found");

        receipt.ReprintCount++;

        return await SaveReceiptAsync(receipt, username);
    }

    #endregion

    #region Receipt Number Generation

    /// <summary>
    /// Generates a unique receipt number in the format RCP-YYMMDD-XXXXX.
    /// </summary>
    private async Task<string> GenerateReceiptNoAsync(int shopId)
    {
        var today = DateTimeOffset.Now;
        var datePrefix = $"RCP-{today:yyMMdd}-";

        // Get today's receipts for this shop to determine sequence
        var startOfDay = new DateTimeOffset(today.Date, today.Offset);
        var endOfDay = startOfDay.AddDays(1);

        IQueryable<Receipt> query = Context.CreateQuery<Receipt>()
            .Where(r => r.IssuedOn >= startOfDay)
            .Where(r => r.IssuedOn < endOfDay)
            .OrderByDescending(r => r.ReceiptId);

        if (shopId > 0)
            query = query.Where(r => r.ShopId == shopId);

        var result = await Context.LoadAsync(query, page: 1, size: 1, includeTotalRows: true);

        var sequence = result.TotalRows + 1;
        return $"{datePrefix}{sequence:D5}";
    }

    #endregion

    #region Reports

    /// <summary>
    /// Gets receipt statistics for a shop within a date range.
    /// </summary>
    public async Task<ReceiptStatistics> GetReceiptStatisticsAsync(
        int shopId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate)
    {
        var query = Context.CreateQuery<Receipt>()
            .Where(r => r.IssuedOn >= fromDate)
            .Where(r => r.IssuedOn <= toDate)
            .Where(r => r.Status == ReceiptStatus.Issued);

        if (shopId > 0)
            query = query.Where(r => r.ShopId == shopId);

        var result = await Context.LoadAsync(query, page: 1, size: 10000, includeTotalRows: false);

        var receipts = result.ItemCollection.ToList();

        return new ReceiptStatistics
        {
            TotalReceipts = receipts.Count,
            CheckInReceipts = receipts.Count(r => r.ReceiptType == ReceiptTypes.CheckIn),
            SettlementReceipts = receipts.Count(r => r.ReceiptType == ReceiptTypes.Settlement),
            BookingDepositReceipts = receipts.Count(r => r.ReceiptType == ReceiptTypes.BookingDeposit),
            TotalAmount = receipts.Sum(r => r.GrandTotal),
            TotalRefunds = receipts.Where(r => r.ReceiptType == ReceiptTypes.Settlement).Sum(r => r.RefundAmount),
            ByPaymentMethod = receipts
                .SelectMany(r => r.Payments)
                .GroupBy(p => p.Method)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.AmountInBaseCurrency))
        };
    }

    /// <summary>
    /// Gets receipts for a till session.
    /// </summary>
    public async Task<List<Receipt>> GetReceiptsByTillSessionAsync(int tillSessionId)
    {
        var result = await Context.LoadAsync(
            Context.CreateQuery<Receipt>()
                .Where(r => r.TillSessionId == tillSessionId)
                .OrderByDescending(r => r.IssuedOn),
            page: 1, size: 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    #endregion
}

#region DTOs

/// <summary>
/// Statistics for receipts in a date range.
/// </summary>
public class ReceiptStatistics
{
    public int TotalReceipts { get; set; }
    public int CheckInReceipts { get; set; }
    public int SettlementReceipts { get; set; }
    public int BookingDepositReceipts { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalRefunds { get; set; }
    public Dictionary<string, decimal> ByPaymentMethod { get; set; } = new();
}

/// <summary>
/// DTO for passing accessory information to receipt generation.
/// </summary>
public class ReceiptAccessoryInfo
{
    public int AccessoryId { get; set; }
    public string? Name { get; set; }
    public int Quantity { get; set; }
    public decimal DailyRate { get; set; }
    public int Days { get; set; }
    public decimal TotalAmount { get; set; }
}

#endregion
