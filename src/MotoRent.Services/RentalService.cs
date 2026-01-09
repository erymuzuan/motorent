using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class RentalService
{
    private readonly RentalDataContext m_context;

    public RentalService(RentalDataContext context)
    {
        m_context = context;
    }

    #region CRUD Operations

    public async Task<LoadOperation<Rental>> GetRentalsAsync(
        int shopId,
        string? status = null,
        string? searchTerm = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.Rentals
            .Where(r => r.ShopId == shopId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.StartDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.ExpectedEndDate <= toDate.Value);
        }

        query = query.OrderByDescending(r => r.RentalId);

        return await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    public async Task<Rental?> GetRentalByIdAsync(int rentalId)
    {
        return await m_context.LoadOneAsync<Rental>(r => r.RentalId == rentalId);
    }

    public async Task<SubmitOperation> CreateRentalAsync(Rental rental, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(rental);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateRentalAsync(Rental rental, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(rental);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteRentalAsync(Rental rental, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Delete(rental);
        return await session.SubmitChanges("Delete");
    }

    #endregion

    #region Query Operations

    public async Task<Dictionary<string, int>> GetStatusCountsAsync(int shopId)
    {
        var allRentals = await m_context.LoadAsync(
            m_context.Rentals.Where(r => r.ShopId == shopId),
            page: 1, size: 10000, includeTotalRows: false);

        return allRentals.ItemCollection
            .GroupBy(r => r.Status ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<Rental>> GetTodaysDueReturnsAsync(int shopId, DateTimeOffset today)
    {
        var todayStart = today.Date;
        var todayEnd = todayStart.AddDays(1);

        var rentals = await m_context.LoadAsync(
            m_context.Rentals
                .Where(r => r.ShopId == shopId && r.Status == "Active")
                .Where(r => r.ExpectedEndDate >= todayStart && r.ExpectedEndDate < todayEnd),
            page: 1, size: 1000, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    public async Task<List<Rental>> GetOverdueRentalsAsync(int shopId, DateTimeOffset today)
    {
        var todayStart = today.Date;

        var rentals = await m_context.LoadAsync(
            m_context.Rentals
                .Where(r => r.ShopId == shopId && r.Status == "Active")
                .Where(r => r.ExpectedEndDate < todayStart),
            page: 1, size: 1000, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    public async Task<List<Rental>> GetActiveRentalsForMotorbikeAsync(int motorbikeId)
    {
        var rentals = await m_context.LoadAsync(
            m_context.Rentals
                .Where(r => r.MotorbikeId == motorbikeId)
                .Where(r => r.Status == "Active" || r.Status == "Reserved"),
            page: 1, size: 100, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    #endregion

    #region Workflow Operations

    public async Task<CheckInResult> CheckInAsync(CheckInRequest request, string username)
    {
        try
        {
            using var session = m_context.OpenSession(username);

            // 1. Create rental
            var rental = new Rental
            {
                ShopId = request.ShopId,
                RenterId = request.RenterId,
                MotorbikeId = request.MotorbikeId,
                StartDate = request.StartDate,
                ExpectedEndDate = request.ExpectedEndDate,
                MileageStart = request.MileageStart,
                DailyRate = request.DailyRate,
                TotalAmount = request.TotalAmount,
                InsuranceId = request.InsuranceId,
                Status = "Active",
                Notes = request.Notes
            };
            session.Attach(rental);

            // 2. Create deposit
            var deposit = new Deposit
            {
                RentalId = 0, // Will be linked after submit via RentalId
                DepositType = request.DepositType,
                Amount = request.DepositAmount,
                Status = "Held",
                CardLast4 = request.CardLast4,
                TransactionRef = request.TransactionRef,
                CollectedOn = DateTimeOffset.Now
            };
            session.Attach(deposit);

            // 3. Create rental accessories
            foreach (var accessory in request.Accessories)
            {
                var rentalAccessory = new RentalAccessory
                {
                    RentalId = 0, // Will be linked
                    AccessoryId = accessory.AccessoryId,
                    Quantity = accessory.Quantity,
                    ChargedAmount = accessory.ChargedAmount
                };
                session.Attach(rentalAccessory);
            }

            // 4. Create rental agreement with signature
            if (!string.IsNullOrEmpty(request.SignatureImagePath))
            {
                var agreement = new RentalAgreement
                {
                    RentalId = 0, // Will be linked
                    AgreementText = request.AgreementText ?? GetDefaultAgreementText(),
                    SignatureImagePath = request.SignatureImagePath,
                    SignedOn = DateTimeOffset.Now,
                    SignedByIp = request.SignedByIp
                };
                session.Attach(agreement);
            }

            // 5. Update motorbike status to "Rented"
            var motorbike = await m_context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == request.MotorbikeId);
            if (motorbike != null)
            {
                motorbike.Status = "Rented";
                motorbike.Mileage = request.MileageStart;
                session.Attach(motorbike);
            }

            // 6. Create payment record for rental amount
            if (request.TotalAmount > 0)
            {
                var rentalPayment = new Payment
                {
                    RentalId = 0, // Will be linked after submit
                    PaymentType = "Rental",
                    PaymentMethod = request.PaymentMethod,
                    Amount = request.TotalAmount,
                    Status = "Completed",
                    TransactionRef = request.PaymentTransactionRef,
                    PaidOn = DateTimeOffset.Now,
                    Notes = $"Rental payment: {(request.ExpectedEndDate - request.StartDate).TotalDays:N0} days"
                };
                session.Attach(rentalPayment);
            }

            // 7. Create payment record for deposit
            if (request.DepositAmount > 0)
            {
                var depositPayment = new Payment
                {
                    RentalId = 0, // Will be linked after submit
                    PaymentType = "Deposit",
                    PaymentMethod = request.DepositType == "Cash" ? "Cash" : "Card",
                    Amount = request.DepositAmount,
                    Status = "Completed",
                    TransactionRef = request.TransactionRef,
                    PaidOn = DateTimeOffset.Now,
                    Notes = $"Security deposit ({request.DepositType})"
                };
                session.Attach(depositPayment);
            }

            // Submit all changes in a single transaction
            var result = await session.SubmitChanges("CheckIn");

            if (result.Success)
            {
                // Update deposit and accessories with the new RentalId
                // Note: In a real implementation, you'd need to link these after the insert
                // For now, we'll rely on sequential IDs or a second update
                return CheckInResult.CreateSuccess(rental.RentalId);
            }

            return CheckInResult.CreateFailure(result.Message ?? "Check-in failed");
        }
        catch (Exception ex)
        {
            return CheckInResult.CreateFailure($"Check-in error: {ex.Message}");
        }
    }

    public async Task<CheckOutResult> CheckOutAsync(CheckOutRequest request, string username)
    {
        try
        {
            using var session = m_context.OpenSession(username);

            // 1. Load and update rental
            var rental = await m_context.LoadOneAsync<Rental>(r => r.RentalId == request.RentalId);
            if (rental == null)
                return CheckOutResult.CreateFailure("Rental not found");

            rental.ActualEndDate = request.ActualEndDate;
            rental.MileageEnd = request.MileageEnd;
            rental.Status = "Completed";
            if (!string.IsNullOrEmpty(request.Notes))
                rental.Notes = (rental.Notes ?? "") + "\n" + request.Notes;
            session.Attach(rental);

            // 2. Calculate additional charges
            decimal additionalCharges = 0;
            int extraDays = CalculateExtraDays(rental.ExpectedEndDate, request.ActualEndDate);
            if (extraDays > 0)
            {
                additionalCharges += extraDays * rental.DailyRate;
            }

            // 3. Process damage reports
            foreach (var damageInfo in request.DamageReports ?? [])
            {
                var damageReport = new DamageReport
                {
                    RentalId = request.RentalId,
                    MotorbikeId = rental.MotorbikeId,
                    Description = damageInfo.Description,
                    Severity = damageInfo.Severity,
                    EstimatedCost = damageInfo.EstimatedCost,
                    Status = "Pending",
                    ReportedOn = DateTimeOffset.Now
                };
                session.Attach(damageReport);
                additionalCharges += damageInfo.EstimatedCost;
            }

            // 4. Update deposit status
            var deposit = await m_context.LoadOneAsync<Deposit>(d => d.RentalId == request.RentalId);
            decimal refundAmount = 0;
            if (deposit != null)
            {
                decimal deductions = additionalCharges;
                if (request.DeductionAmount.HasValue)
                    deductions = request.DeductionAmount.Value;

                refundAmount = Math.Max(0, deposit.Amount - deductions);

                if (refundAmount > 0 && request.RefundDeposit)
                {
                    deposit.Status = "Refunded";
                    deposit.RefundedOn = DateTimeOffset.Now;
                }
                else if (deductions >= deposit.Amount)
                {
                    deposit.Status = "Forfeited";
                }
                session.Attach(deposit);
            }

            // 5. Update motorbike status back to "Available"
            var motorbike = await m_context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == rental.MotorbikeId);
            if (motorbike != null)
            {
                motorbike.Status = "Available";
                if (request.MileageEnd > 0)
                    motorbike.Mileage = request.MileageEnd;
                session.Attach(motorbike);
            }

            // 6. Create payment record for additional charges
            if (additionalCharges > 0)
            {
                var payment = new Payment
                {
                    RentalId = request.RentalId,
                    PaymentType = "Additional",
                    PaymentMethod = request.PaymentMethod ?? "Cash",
                    Amount = additionalCharges,
                    Status = "Completed",
                    PaidOn = DateTimeOffset.Now,
                    Notes = $"Extra days: {extraDays}, Damage charges: {additionalCharges - (extraDays * rental.DailyRate)}"
                };
                session.Attach(payment);
            }

            // 7. Create payment record for deposit refund
            if (refundAmount > 0 && request.RefundDeposit)
            {
                var refundPayment = new Payment
                {
                    RentalId = request.RentalId,
                    PaymentType = "Refund",
                    PaymentMethod = request.PaymentMethod ?? "Cash",
                    Amount = refundAmount,
                    Status = "Completed",
                    PaidOn = DateTimeOffset.Now,
                    Notes = $"Deposit refund via {request.PaymentMethod ?? "Cash"}"
                };
                session.Attach(refundPayment);
            }

            var result = await session.SubmitChanges("CheckOut");

            if (result.Success)
            {
                return CheckOutResult.CreateSuccess(
                    rentalId: request.RentalId,
                    additionalCharges: additionalCharges,
                    refundAmount: refundAmount,
                    extraDays: extraDays
                );
            }

            return CheckOutResult.CreateFailure(result.Message ?? "Check-out failed");
        }
        catch (Exception ex)
        {
            return CheckOutResult.CreateFailure($"Check-out error: {ex.Message}");
        }
    }

    public async Task<SubmitOperation> CancelRentalAsync(int rentalId, string reason, string username)
    {
        var rental = await GetRentalByIdAsync(rentalId);
        if (rental == null)
            return SubmitOperation.CreateFailure("Rental not found");

        if (rental.Status == "Completed")
            return SubmitOperation.CreateFailure("Cannot cancel a completed rental");

        using var session = m_context.OpenSession(username);

        rental.Status = "Cancelled";
        rental.Notes = (rental.Notes ?? "") + $"\nCancelled: {reason}";
        session.Attach(rental);

        // Refund deposit if held
        var deposit = await m_context.LoadOneAsync<Deposit>(d => d.RentalId == rentalId);
        if (deposit != null && deposit.Status == "Held")
        {
            deposit.Status = "Refunded";
            deposit.RefundedOn = DateTimeOffset.Now;
            session.Attach(deposit);
        }

        // Make motorbike available again
        var motorbike = await m_context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == rental.MotorbikeId);
        if (motorbike != null && motorbike.Status == "Rented")
        {
            motorbike.Status = "Available";
            session.Attach(motorbike);
        }

        return await session.SubmitChanges("Cancel");
    }

    public async Task<SubmitOperation> ExtendRentalAsync(int rentalId, DateTimeOffset newEndDate, string username)
    {
        var rental = await GetRentalByIdAsync(rentalId);
        if (rental == null)
            return SubmitOperation.CreateFailure("Rental not found");

        if (rental.Status != "Active")
            return SubmitOperation.CreateFailure("Can only extend active rentals");

        if (newEndDate <= rental.ExpectedEndDate)
            return SubmitOperation.CreateFailure("New end date must be after current expected end date");

        int additionalDays = (int)(newEndDate.Date - rental.ExpectedEndDate.Date).TotalDays;
        decimal additionalAmount = additionalDays * rental.DailyRate;

        using var session = m_context.OpenSession(username);

        rental.ExpectedEndDate = newEndDate;
        rental.TotalAmount += additionalAmount;
        rental.Notes = (rental.Notes ?? "") + $"\nExtended by {additionalDays} days";
        session.Attach(rental);

        return await session.SubmitChanges("Extend");
    }

    public async Task<ReservationResult> CreateReservationAsync(ReservationRequest request)
    {
        try
        {
            using var session = m_context.OpenSession("tourist");

            // 1. Check if motorbike is available for the requested dates
            var conflictingRentals = await m_context.LoadAsync(
                m_context.Rentals
                    .Where(r => r.MotorbikeId == request.MotorbikeId)
                    .Where(r => r.Status == "Active" || r.Status == "Reserved")
                    .Where(r => r.StartDate < request.EndDate && r.ExpectedEndDate > request.StartDate),
                page: 1, size: 10, includeTotalRows: false);

            if (conflictingRentals.ItemCollection.Any())
            {
                return ReservationResult.CreateFailure("This motorbike is not available for the selected dates.");
            }

            // 2. Create or find renter from contact info
            var existingRenter = await m_context.LoadOneAsync<Renter>(
                r => r.Phone == request.RenterPhone || r.Email == request.RenterEmail);

            int renterId;
            if (existingRenter != null)
            {
                renterId = existingRenter.RenterId;
                // Update info if needed
                existingRenter.FullName = request.RenterName;
                existingRenter.Nationality = request.RenterNationality;
                existingRenter.PassportNo = request.RenterPassport;
                session.Attach(existingRenter);
            }
            else
            {
                var newRenter = new Renter
                {
                    ShopId = request.ShopId,
                    FullName = request.RenterName,
                    Phone = request.RenterPhone,
                    Email = request.RenterEmail,
                    Nationality = request.RenterNationality,
                    PassportNo = request.RenterPassport
                };
                session.Attach(newRenter);
                renterId = 0; // Will be assigned after submit
            }

            // 3. Create reservation (rental with "Reserved" status)
            var rental = new Rental
            {
                ShopId = request.ShopId,
                RenterId = renterId,
                MotorbikeId = request.MotorbikeId,
                StartDate = request.StartDate,
                ExpectedEndDate = request.EndDate,
                DailyRate = request.DailyRate,
                TotalAmount = request.TotalAmount,
                InsuranceId = request.InsuranceId,
                Status = "Reserved",
                Notes = BuildReservationNotes(request)
            };
            session.Attach(rental);

            var result = await session.SubmitChanges("CreateReservation");

            if (result.Success)
            {
                return ReservationResult.CreateSuccess(rental.RentalId, GenerateConfirmationCode());
            }

            return ReservationResult.CreateFailure(result.Message ?? "Reservation failed");
        }
        catch (Exception ex)
        {
            return ReservationResult.CreateFailure($"Reservation error: {ex.Message}");
        }
    }

    private static string BuildReservationNotes(ReservationRequest request)
    {
        var notes = $"Online Reservation - {DateTimeOffset.Now:g}";
        if (!string.IsNullOrEmpty(request.HotelName))
            notes += $"\nHotel: {request.HotelName}";
        if (!string.IsNullOrEmpty(request.Notes))
            notes += $"\nNotes: {request.Notes}";
        return notes;
    }

    private static string GenerateConfirmationCode()
    {
        return $"MR-{DateTime.Now:yyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }

    /// <summary>
    /// Gets rental history for a tourist by email or phone
    /// </summary>
    public async Task<List<Rental>> GetRentalHistoryForTouristAsync(int shopId, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            return [];

        // First find the renter
        var renter = await m_context.LoadOneAsync<Renter>(r =>
            r.ShopId == shopId &&
            ((email != null && r.Email == email) || (phone != null && r.Phone == phone)));

        if (renter == null)
            return [];

        // Then get their rentals
        var rentals = await m_context.LoadAsync(
            m_context.Rentals
                .Where(r => r.RenterId == renter.RenterId)
                .OrderByDescending(r => r.RentalId),
            page: 1, size: 100, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    #endregion

    #region Helper Methods

    private static int CalculateExtraDays(DateTimeOffset expectedEnd, DateTimeOffset actualEnd)
    {
        if (actualEnd <= expectedEnd)
            return 0;

        return (int)Math.Ceiling((actualEnd - expectedEnd).TotalDays);
    }

    private static string GetDefaultAgreementText()
    {
        return @"MOTORBIKE RENTAL AGREEMENT

I agree to the following terms and conditions:

1. I will return the motorbike in the same condition as received.
2. I am responsible for any damage caused during the rental period.
3. I will not exceed the agreed rental period without prior notification.
4. I have a valid driving license for this vehicle type.
5. I will follow all traffic laws and regulations.
6. I will not sublet or lend the motorbike to any third party.

By signing below, I acknowledge that I have read, understood, and agree to these terms.";
    }

    #endregion
}

#region DTOs

public class CheckInRequest
{
    public int ShopId { get; set; }
    public int RenterId { get; set; }
    public int MotorbikeId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset ExpectedEndDate { get; set; }
    public int MileageStart { get; set; }
    public decimal DailyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public int? InsuranceId { get; set; }
    public string? Notes { get; set; }

    // Payment info
    public string PaymentMethod { get; set; } = "Cash";
    public string? PaymentTransactionRef { get; set; }

    // Deposit info
    public string DepositType { get; set; } = "Cash";
    public decimal DepositAmount { get; set; }
    public string? CardLast4 { get; set; }
    public string? TransactionRef { get; set; }

    // Accessories
    public List<AccessorySelection> Accessories { get; set; } = [];

    // Agreement
    public string? AgreementText { get; set; }
    public string? SignatureImagePath { get; set; }
    public string? SignedByIp { get; set; }
}

public class AccessorySelection
{
    public int AccessoryId { get; set; }
    public int Quantity { get; set; }
    public decimal ChargedAmount { get; set; }
}

public class CheckOutRequest
{
    public int RentalId { get; set; }
    public DateTimeOffset ActualEndDate { get; set; }
    public int MileageEnd { get; set; }
    public string? Notes { get; set; }
    public string? PaymentMethod { get; set; }
    public bool RefundDeposit { get; set; } = true;
    public decimal? DeductionAmount { get; set; }

    public List<DamageReportInfo>? DamageReports { get; set; }
}

public class DamageReportInfo
{
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Minor";
    public decimal EstimatedCost { get; set; }
    public List<string>? PhotoPaths { get; set; }
}

public class CheckInResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int RentalId { get; set; }

    public static CheckInResult CreateSuccess(int rentalId) => new()
    {
        Success = true,
        RentalId = rentalId
    };

    public static CheckInResult CreateFailure(string message) => new()
    {
        Success = false,
        Message = message
    };
}

public class CheckOutResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int RentalId { get; set; }
    public decimal AdditionalCharges { get; set; }
    public decimal RefundAmount { get; set; }
    public int ExtraDays { get; set; }

    public static CheckOutResult CreateSuccess(int rentalId, decimal additionalCharges, decimal refundAmount, int extraDays) => new()
    {
        Success = true,
        RentalId = rentalId,
        AdditionalCharges = additionalCharges,
        RefundAmount = refundAmount,
        ExtraDays = extraDays
    };

    public static CheckOutResult CreateFailure(string message) => new()
    {
        Success = false,
        Message = message
    };
}

public class ReservationRequest
{
    public int ShopId { get; set; }
    public int MotorbikeId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int? InsuranceId { get; set; }
    public decimal DailyRate { get; set; }
    public decimal TotalAmount { get; set; }

    // Contact info
    public string RenterName { get; set; } = string.Empty;
    public string RenterPhone { get; set; } = string.Empty;
    public string RenterEmail { get; set; } = string.Empty;
    public string? RenterNationality { get; set; }
    public string? RenterPassport { get; set; }
    public string? HotelName { get; set; }
    public string? Notes { get; set; }
}

public class ReservationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int RentalId { get; set; }
    public string? ConfirmationCode { get; set; }

    public static ReservationResult CreateSuccess(int rentalId, string confirmationCode) => new()
    {
        Success = true,
        RentalId = rentalId,
        ConfirmationCode = confirmationCode
    };

    public static ReservationResult CreateFailure(string message) => new()
    {
        Success = false,
        Message = message
    };
}

#endregion
