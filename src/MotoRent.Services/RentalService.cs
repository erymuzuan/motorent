using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;

namespace MotoRent.Services;

public class RentalService(
    RentalDataContext context,
    VehiclePoolService? poolService = null,
    BookingService? bookingService = null,
    AgentCommissionService? commissionService = null)
{
    private RentalDataContext Context { get; } = context;
    private VehiclePoolService? PoolService { get; } = poolService;
    private BookingService? BookingService { get; } = bookingService;
    private AgentCommissionService? CommissionService { get; } = commissionService;

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
        var query = this.Context.CreateQuery<Rental>()
            .Where(r => r.RentedFromShopId == shopId || r.ReturnedToShopId == shopId);

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

        return await this.Context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    public async Task<Rental?> GetRentalByIdAsync(int rentalId)
    {
        return await this.Context.LoadOneAsync<Rental>(r => r.RentalId == rentalId);
    }

    public async Task<SubmitOperation> CreateRentalAsync(Rental rental, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(rental);
        return await session.SubmitChanges("Create");
    }

    public async Task<SubmitOperation> UpdateRentalAsync(Rental rental, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(rental);
        return await session.SubmitChanges("Update");
    }

    public async Task<SubmitOperation> DeleteRentalAsync(Rental rental, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Delete(rental);
        return await session.SubmitChanges("Delete");
    }

    #endregion

    #region Query Operations

    public async Task<Dictionary<string, int>> GetStatusCountsAsync(int shopId)
    {
        var allRentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>().Where(r => r.RentedFromShopId == shopId),
            page: 1, size: 10000, includeTotalRows: false);

        return allRentals.ItemCollection
            .GroupBy(r => r.Status ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Gets dynamic pricing statistics for completed rentals within a date range.
    /// </summary>
    public async Task<DynamicPricingStats> GetDynamicPricingStatsAsync(int shopId, DateTimeOffset fromDate, DateTimeOffset toDate)
    {
        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.RentedFromShopId == shopId)
                .Where(r => r.StartDate >= fromDate && r.StartDate <= toDate),
            page: 1, size: 10000, includeTotalRows: false);

        var completedRentals = rentals.ItemCollection.Where(r => r.Status is "Completed" or "Active").ToList();
        var dynamicPricingRentals = completedRentals.Where(r => r.DynamicPricingApplied).ToList();

        if (dynamicPricingRentals.Count == 0)
        {
            return new DynamicPricingStats
            {
                TotalRentals = completedRentals.Count,
                RentalsWithDynamicPricing = 0,
                BaseRevenue = completedRentals.Sum(r => r.BaseRentalRate * r.DayPricingBreakdown.Count),
                ActualRevenue = completedRentals.Sum(r => r.TotalAmount),
                DynamicPricingPremium = 0,
                AverageMultiplier = 1.0m
            };
        }

        var baseRevenue = dynamicPricingRentals.Sum(r => r.BaseRentalRate * r.DayPricingBreakdown.Count);
        var adjustedRevenue = dynamicPricingRentals.Sum(r => r.DayPricingBreakdown.Sum(d => d.AdjustedRate));
        var dynamicPricingPremium = adjustedRevenue - baseRevenue;
        var avgMultiplier = dynamicPricingRentals.Average(r => r.AverageMultiplier);

        return new DynamicPricingStats
        {
            TotalRentals = completedRentals.Count,
            RentalsWithDynamicPricing = dynamicPricingRentals.Count,
            BaseRevenue = baseRevenue,
            ActualRevenue = adjustedRevenue,
            DynamicPricingPremium = dynamicPricingPremium,
            AverageMultiplier = avgMultiplier,
            RuleBreakdown = dynamicPricingRentals
                .SelectMany(r => r.DayPricingBreakdown.Where(d => d.HasAdjustment))
                .GroupBy(d => d.RuleName ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<List<Rental>> GetTodaysDueReturnsAsync(int shopId, DateTimeOffset today)
    {
        var todayStart = today.Date;
        var todayEnd = todayStart.AddDays(1);

        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.RentedFromShopId == shopId && r.Status == "Active")
                .Where(r => r.ExpectedEndDate >= todayStart && r.ExpectedEndDate < todayEnd),
            page: 1, size: 1000, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    public async Task<List<Rental>> GetOverdueRentalsAsync(int shopId, DateTimeOffset today)
    {
        var todayStart = today.Date;

        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.RentedFromShopId == shopId && r.Status == "Active")
                .Where(r => r.ExpectedEndDate < todayStart),
            page: 1, size: 1000, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    public async Task<List<Rental>> GetActiveRentalsForVehicleAsync(int vehicleId)
    {
        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.VehicleId == vehicleId)
                .Where(r => r.Status == "Active" || r.Status == "Reserved"),
            page: 1, size: 100, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    [Obsolete("Use GetActiveRentalsForVehicleAsync instead")]
    public async Task<List<Rental>> GetActiveRentalsForMotorbikeAsync(int motorbikeId)
    {
        return await GetActiveRentalsForVehicleAsync(motorbikeId);
    }

    #endregion

    #region Workflow Operations

    public async Task<CheckInResult> CheckInAsync(CheckInRequest request, string username)
    {
        try
        {
            using var session = this.Context.OpenSession(username);

            // Load vehicle to get pool info
            var vehicle = await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == request.VehicleId);
            if (vehicle == null)
            {
                // Fallback to Motorbike for backward compatibility
                var motorbike = await this.Context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == request.VehicleId);
                if (motorbike == null)
                    return CheckInResult.CreateFailure("Vehicle not found");

                // Use motorbike as vehicle equivalent
                return await CheckInWithMotorbikeAsync(request, motorbike, session, username);
            }

            // Validate pool membership for pooled vehicles
            if (vehicle.IsPooled && PoolService != null)
            {
                var pool = await PoolService.GetPoolByIdAsync(vehicle.VehiclePoolId!.Value);
                if (pool != null && !pool.ShopIds.Contains(request.ShopId))
                {
                    return CheckInResult.CreateFailure("This shop is not authorized to rent this pooled vehicle");
                }
            }

            // 1. Create rental with new fields
            var rental = new Rental
            {
                RentedFromShopId = request.ShopId,
                VehiclePoolId = vehicle.VehiclePoolId,
                RenterId = request.RenterId,
                VehicleId = request.VehicleId,
                DurationType = request.DurationType,
                IntervalMinutes = request.IntervalMinutes,
                StartDate = request.StartDate,
                ExpectedEndDate = request.ExpectedEndDate,
                MileageStart = request.MileageStart,
                RentalRate = request.RentalRate,
                TotalAmount = request.TotalAmount,
                IncludeDriver = request.IncludeDriver,
                IncludeGuide = request.IncludeGuide,
                DriverFee = request.DriverFee,
                GuideFee = request.GuideFee,
                InsuranceId = request.InsuranceId,
                Status = "Active",
                Notes = request.Notes,
                VehicleName = $"{vehicle.Brand} {vehicle.Model}",
                // Pick-up location and fees
                PickupLocationId = request.PickupLocationId,
                PickupLocationName = request.PickupLocationName,
                ScheduledPickupTime = request.ScheduledPickupTime,
                PickupLocationFee = request.PickupLocationFee,
                OutOfHoursPickupFee = request.OutOfHoursPickupFee,
                IsOutOfHoursPickup = request.IsOutOfHoursPickup,
                OutOfHoursPickupBand = request.OutOfHoursPickupBand,
                // Expected drop-off location and fees
                DropoffLocationId = request.DropoffLocationId,
                DropoffLocationName = request.DropoffLocationName,
                ScheduledDropoffTime = request.ScheduledDropoffTime,
                DropoffLocationFee = request.DropoffLocationFee,
                OutOfHoursDropoffFee = request.OutOfHoursDropoffFee,
                IsOutOfHoursDropoff = request.IsOutOfHoursDropoff,
                OutOfHoursDropoffBand = request.OutOfHoursDropoffBand,
                // Pre-rental inspection
                PreRentalInspection = request.PreRentalInspection
            };
            session.Attach(rental);

            // 2. Create deposit
            var deposit = new Deposit
            {
                RentalId = 0,
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
                    RentalId = 0,
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
                    RentalId = 0,
                    AgreementText = request.AgreementText ?? GetDefaultAgreementText(),
                    SignatureImagePath = request.SignatureImagePath,
                    SignedOn = DateTimeOffset.Now,
                    SignedByIp = request.SignedByIp
                };
                session.Attach(agreement);
            }

            // 5. Update vehicle status to "Rented"
            vehicle.Status = VehicleStatus.Rented;
            if (vehicle.SupportsMileageTracking)
                vehicle.Mileage = request.MileageStart;
            session.Attach(vehicle);

            // 6. Create payment record for rental amount
            if (request.TotalAmount > 0)
            {
                var durationNote = request.DurationType == RentalDurationType.FixedInterval
                    ? $"{request.IntervalMinutes} minutes"
                    : $"{(request.ExpectedEndDate - request.StartDate).TotalDays:N0} days";

                var rentalPayment = new Payment
                {
                    RentalId = 0,
                    PaymentType = "Rental",
                    PaymentMethod = request.PaymentMethod,
                    Amount = request.TotalAmount,
                    Status = "Completed",
                    TransactionRef = request.PaymentTransactionRef,
                    PaidOn = DateTimeOffset.Now,
                    Notes = $"Rental payment: {durationNote}"
                };
                session.Attach(rentalPayment);
            }

            // 7. Create payment record for deposit
            if (request.DepositAmount > 0)
            {
                var depositPayment = new Payment
                {
                    RentalId = 0,
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

            var result = await session.SubmitChanges("CheckIn");

            if (result.Success)
            {
                return CheckInResult.CreateSuccess(rental.RentalId);
            }

            return CheckInResult.CreateFailure(result.Message ?? "Check-in failed");
        }
        catch (Exception ex)
        {
            return CheckInResult.CreateFailure($"Check-in error: {ex.Message}");
        }
    }

    private async Task<CheckInResult> CheckInWithMotorbikeAsync(CheckInRequest request, Motorbike motorbike, PersistenceSession session, string username)
    {
        // Legacy path for Motorbike entities
        var rental = new Rental
        {
            RentedFromShopId = request.ShopId,
            RenterId = request.RenterId,
            VehicleId = request.VehicleId,
            DurationType = RentalDurationType.Daily,
            StartDate = request.StartDate,
            ExpectedEndDate = request.ExpectedEndDate,
            MileageStart = request.MileageStart,
            RentalRate = request.RentalRate,
            TotalAmount = request.TotalAmount,
            InsuranceId = request.InsuranceId,
            Status = "Active",
            Notes = request.Notes,
            VehicleName = $"{motorbike.Brand} {motorbike.Model}",
            PreRentalInspection = request.PreRentalInspection
        };
        session.Attach(rental);

        // Update motorbike status
        motorbike.Status = "Rented";
        motorbike.Mileage = request.MileageStart;
        session.Attach(motorbike);

        // Rest of check-in logic...
        var result = await session.SubmitChanges("CheckIn");
        return result.Success
            ? CheckInResult.CreateSuccess(rental.RentalId)
            : CheckInResult.CreateFailure(result.Message ?? "Check-in failed");
    }

    public async Task<CheckOutResult> CheckOutAsync(CheckOutRequest request, string username)
    {
        try
        {
            using var session = this.Context.OpenSession(username);

            // 1. Load and update rental
            var rental = await this.Context.LoadOneAsync<Rental>(r => r.RentalId == request.RentalId);
            if (rental == null)
                return CheckOutResult.CreateFailure("Rental not found");

            // 2. Load vehicle (try Vehicle first, then Motorbike for backward compat)
            Vehicle? vehicle = await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == rental.VehicleId);
            Motorbike? motorbike = null;

            if (vehicle == null)
            {
                motorbike = await this.Context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == rental.VehicleId);
                if (motorbike == null)
                    return CheckOutResult.CreateFailure("Vehicle not found");
            }

            // 3. Validate return shop for pooled vehicles
            int returnShopId = request.ReturnShopId ?? rental.RentedFromShopId;

            if (vehicle != null && vehicle.IsPooled && PoolService != null)
            {
                var pool = await PoolService.GetPoolByIdAsync(vehicle.VehiclePoolId!.Value);
                if (pool != null && !pool.ShopIds.Contains(returnShopId))
                {
                    return CheckOutResult.CreateFailure("This shop cannot accept returns for this pooled vehicle");
                }
            }
            else if (vehicle != null && !vehicle.IsPooled && returnShopId != rental.RentedFromShopId)
            {
                return CheckOutResult.CreateFailure("Non-pooled vehicles must be returned to the rental shop");
            }

            // 4. Update rental
            rental.ActualEndDate = request.ActualEndDate;
            rental.ReturnedToShopId = returnShopId;
            rental.MileageEnd = request.MileageEnd;
            rental.Status = "Completed";
            if (!string.IsNullOrEmpty(request.Notes))
                rental.Notes = (rental.Notes ?? "") + "\n" + request.Notes;

            // Update drop-off location and fees (may override check-in values)
            if (request.DropoffLocationId.HasValue)
            {
                rental.DropoffLocationId = request.DropoffLocationId;
                rental.DropoffLocationName = request.DropoffLocationName;
            }
            if (request.ScheduledDropoffTime.HasValue)
                rental.ScheduledDropoffTime = request.ScheduledDropoffTime;
            if (request.DropoffLocationFee > 0)
                rental.DropoffLocationFee = request.DropoffLocationFee;
            if (request.OutOfHoursDropoffFee > 0 || request.IsOutOfHoursDropoff)
            {
                rental.OutOfHoursDropoffFee = request.OutOfHoursDropoffFee;
                rental.IsOutOfHoursDropoff = request.IsOutOfHoursDropoff;
                rental.OutOfHoursDropoffBand = request.OutOfHoursDropoffBand;
            }

            // Post-rental inspection
            if (request.PostRentalInspection != null)
                rental.PostRentalInspection = request.PostRentalInspection;

            session.Attach(rental);

            // 5. Calculate additional charges
            decimal additionalCharges = 0;
            int extraDays = 0;

            if (rental.DurationType == RentalDurationType.Daily)
            {
                extraDays = CalculateExtraDays(rental.ExpectedEndDate, request.ActualEndDate);
                if (extraDays > 0)
                {
                    additionalCharges += extraDays * rental.RentalRate;
                }
            }
            else
            {
                // Interval rental overtime
                var expectedEnd = rental.StartDate.AddMinutes(rental.IntervalMinutes ?? 60);
                if (request.ActualEndDate > expectedEnd)
                {
                    var extraMinutes = (request.ActualEndDate - expectedEnd).TotalMinutes;
                    // Charge at hourly rate (prorated)
                    additionalCharges += (decimal)(extraMinutes / 60) * rental.RentalRate;
                }
            }

            // 6. Process damage reports
            foreach (var damageInfo in request.DamageReports ?? [])
            {
                var damageReport = new DamageReport
                {
                    RentalId = request.RentalId,
                    MotorbikeId = rental.VehicleId,
                    Description = damageInfo.Description,
                    Severity = damageInfo.Severity,
                    EstimatedCost = damageInfo.EstimatedCost,
                    Status = "Pending",
                    ReportedOn = DateTimeOffset.Now
                };
                session.Attach(damageReport);
                additionalCharges += damageInfo.EstimatedCost;
            }

            // 7. Update deposit status
            var deposit = await this.Context.LoadOneAsync<Deposit>(d => d.RentalId == request.RentalId);
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

            // 8. Update vehicle status and location
            if (vehicle != null)
            {
                vehicle.Status = VehicleStatus.Available;
                vehicle.CurrentShopId = returnShopId; // Update location for pooled vehicles
                if (vehicle.SupportsMileageTracking && request.MileageEnd > 0)
                    vehicle.Mileage = request.MileageEnd;
                session.Attach(vehicle);
            }
            else if (motorbike != null)
            {
                motorbike.Status = "Available";
                if (request.MileageEnd > 0)
                    motorbike.Mileage = request.MileageEnd;
                session.Attach(motorbike);
            }

            // 9. Create payment record for additional charges
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
                    Notes = rental.DurationType == RentalDurationType.Daily
                        ? $"Extra days: {extraDays}, Damage charges: {additionalCharges - (extraDays * rental.RentalRate)}"
                        : $"Overtime charges: {additionalCharges}"
                };
                session.Attach(payment);
            }

            // 10. Create payment record for deposit refund
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

            // 11. Create owner payment if vehicle is third-party owned
            if (vehicle is { IsThirdPartyOwned: true })
            {
                var owner = await this.Context.LoadOneAsync<VehicleOwner>(
                    o => o.VehicleOwnerId == vehicle.VehicleOwnerId);

                if (owner is not null)
                {
                    var ownerPayment = new OwnerPayment
                    {
                        VehicleOwnerId = owner.VehicleOwnerId,
                        VehicleId = vehicle.VehicleId,
                        RentalId = rental.RentalId,
                        PaymentModel = vehicle.OwnerPaymentModel ?? OwnerPaymentModel.DailyRate,
                        RentalStartDate = rental.StartDate,
                        RentalEndDate = request.ActualEndDate,
                        VehicleOwnerName = owner.Name,
                        VehicleName = vehicle.DisplayName,
                        Status = OwnerPaymentStatus.Pending
                    };

                    if (vehicle.OwnerPaymentModel is OwnerPaymentModel.DailyRate)
                    {
                        ownerPayment.RentalDays = rental.RentalDays;
                        ownerPayment.GrossRentalAmount = rental.TotalAmount;
                        ownerPayment.CalculationRate = vehicle.OwnerDailyRate ?? 0;
                        ownerPayment.Amount = ownerPayment.RentalDays * ownerPayment.CalculationRate;
                    }
                    else // RevenueShare - GROSS rental only
                    {
                        ownerPayment.RentalDays = rental.RentalDays;
                        ownerPayment.GrossRentalAmount = rental.RentalRate * rental.RentalDays;
                        ownerPayment.CalculationRate = vehicle.OwnerRevenueSharePercent ?? 0;
                        ownerPayment.Amount = ownerPayment.GrossRentalAmount * ownerPayment.CalculationRate;
                    }

                    session.Attach(ownerPayment);
                }
            }

            var result = await session.SubmitChanges("CheckOut");

            if (result.Success)
            {
                // 12. Make agent commission eligible if this rental is from an agent booking
                await MakeAgentCommissionEligibleAsync(rental, username);

                return CheckOutResult.CreateSuccess(
                    rentalId: request.RentalId,
                    additionalCharges: additionalCharges,
                    refundAmount: refundAmount,
                    extraDays: extraDays,
                    isCrossShopReturn: returnShopId != rental.RentedFromShopId
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
        var rental = await this.GetRentalByIdAsync(rentalId);
        if (rental == null)
            return SubmitOperation.CreateFailure("Rental not found");

        if (rental.Status == "Completed")
            return SubmitOperation.CreateFailure("Cannot cancel a completed rental");

        using var session = this.Context.OpenSession(username);

        rental.Status = "Cancelled";
        rental.Notes = (rental.Notes ?? "") + $"\nCancelled: {reason}";
        session.Attach(rental);

        // Refund deposit if held
        var deposit = await this.Context.LoadOneAsync<Deposit>(d => d.RentalId == rentalId);
        if (deposit != null && deposit.Status == "Held")
        {
            deposit.Status = "Refunded";
            deposit.RefundedOn = DateTimeOffset.Now;
            session.Attach(deposit);
        }

        // Make vehicle available again
        var vehicle = await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == rental.VehicleId);
        if (vehicle != null && vehicle.Status == VehicleStatus.Rented)
        {
            vehicle.Status = VehicleStatus.Available;
            session.Attach(vehicle);
        }
        else
        {
            // Fallback for Motorbike
            var motorbike = await this.Context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == rental.VehicleId);
            if (motorbike != null && motorbike.Status == "Rented")
            {
                motorbike.Status = "Available";
                session.Attach(motorbike);
            }
        }

        return await session.SubmitChanges("Cancel");
    }

    public async Task<SubmitOperation> ExtendRentalAsync(int rentalId, DateTimeOffset newEndDate, string username)
    {
        var rental = await this.GetRentalByIdAsync(rentalId);
        if (rental == null)
            return SubmitOperation.CreateFailure("Rental not found");

        if (rental.Status != "Active")
            return SubmitOperation.CreateFailure("Can only extend active rentals");

        if (rental.DurationType == RentalDurationType.FixedInterval)
            return SubmitOperation.CreateFailure("Cannot extend interval-based rentals. Start a new session instead.");

        if (newEndDate <= rental.ExpectedEndDate)
            return SubmitOperation.CreateFailure("New end date must be after current expected end date");

        int additionalDays = (int)(newEndDate.Date - rental.ExpectedEndDate.Date).TotalDays;
        decimal additionalAmount = additionalDays * rental.RentalRate;

        using var session = this.Context.OpenSession(username);

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
            using var session = this.Context.OpenSession("tourist");

            // 1. Check if vehicle is available for the requested dates
            // For group reservations (VehicleId == 0, VehicleGroupKey set), skip individual vehicle check
            // since the actual vehicle will be assigned at check-in
            bool isGroupReservation = request.VehicleId == 0 && !string.IsNullOrEmpty(request.VehicleGroupKey);

            if (!isGroupReservation && request.VehicleId > 0)
            {
                var hasConflict = await this.Context.ExistAsync(
                    this.Context.CreateQuery<Rental>()
                        .Where(r => r.VehicleId == request.VehicleId)
                        .Where(r => r.Status == "Active" || r.Status == "Reserved")
                        .Where(r => r.StartDate < request.EndDate && r.ExpectedEndDate > request.StartDate));

                if (hasConflict)
                {
                    return ReservationResult.CreateFailure("This vehicle is not available for the selected dates.");
                }
            }

            // 2. Create or find renter from contact info
            var existingRenter = await this.Context.LoadOneAsync<Renter>(
                r => r.Phone == request.RenterPhone || r.Email == request.RenterEmail);

            int renterId;
            if (existingRenter != null)
            {
                renterId = existingRenter.RenterId;
                existingRenter.FullName = request.RenterName;
                existingRenter.Nationality = request.RenterNationality;
                existingRenter.PassportNo = request.RenterPassport;
                session.Attach(existingRenter);
            }
            else
            {
                var newRenter = new Renter
                {
                    FullName = request.RenterName,
                    Phone = request.RenterPhone,
                    Email = request.RenterEmail,
                    Nationality = request.RenterNationality,
                    PassportNo = request.RenterPassport
                };
                session.Attach(newRenter);
                renterId = 0;
            }

            // 3. Create reservation (rental with "Reserved" status)
            var rental = new Rental
            {
                RentedFromShopId = request.ShopId,
                RenterId = renterId,
                VehicleId = request.VehicleId,
                VehicleGroupKey = request.VehicleGroupKey,
                PreferredColor = request.PreferredColor,
                DurationType = request.DurationType,
                IntervalMinutes = request.IntervalMinutes,
                StartDate = request.StartDate,
                ExpectedEndDate = request.EndDate,
                RentalRate = request.RentalRate,
                TotalAmount = request.TotalAmount,
                IncludeDriver = request.IncludeDriver,
                IncludeGuide = request.IncludeGuide,
                DriverFee = request.DriverFee,
                GuideFee = request.GuideFee,
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

    public async Task<List<Rental>> GetRentalHistoryForTouristAsync(int shopId, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            return [];

        // Find renter by email or phone (renters are universal, not shop-specific)
        var renter = await this.Context.LoadOneAsync<Renter>(r =>
            (email != null && r.Email == email) || (phone != null && r.Phone == phone));

        if (renter == null)
            return [];

        var rentals = await this.Context.LoadAsync(
            this.Context.CreateQuery<Rental>()
                .Where(r => r.RenterId == renter.RenterId)
                .OrderByDescending(r => r.RentalId),
            page: 1, size: 100, includeTotalRows: false);

        return rentals.ItemCollection.ToList();
    }

    /// <summary>
    /// Assigns a specific vehicle to a group-based reservation.
    /// Used during check-in to convert a model reservation to a specific vehicle rental.
    /// </summary>
    /// <param name="rentalId">The rental/reservation ID</param>
    /// <param name="vehicleId">The specific vehicle to assign (optional - auto-selects if not provided)</param>
    /// <param name="username">The staff member performing the assignment</param>
    /// <returns>The assigned vehicle, or null if no suitable vehicle found</returns>
    public async Task<Vehicle?> AssignVehicleToRentalAsync(int rentalId, int? vehicleId, string username)
    {
        var rental = await this.Context.LoadOneAsync<Rental>(r => r.RentalId == rentalId);
        if (rental == null)
            return null;

        // If not a group reservation, just return the already-assigned vehicle
        if (!rental.IsGroupReservation)
        {
            return await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == rental.VehicleId);
        }

        Vehicle? vehicle = null;

        if (vehicleId.HasValue && vehicleId.Value > 0)
        {
            // Staff selected a specific vehicle
            vehicle = await this.Context.LoadOneAsync<Vehicle>(v =>
                v.VehicleId == vehicleId.Value &&
                v.Status == VehicleStatus.Available);
        }
        else if (!string.IsNullOrEmpty(rental.VehicleGroupKey))
        {
            // Auto-select based on group key and color preference
            var vehicles = await this.Context.LoadAsync(
                this.Context.CreateQuery<Vehicle>()
                    .Where(v => v.Status == VehicleStatus.Available));

            var availableInGroup = vehicles.ItemCollection
                .Where(v => VehicleGroup.CreateGroupKey(v) == rental.VehicleGroupKey)
                .ToList();

            if (availableInGroup.Count > 0)
            {
                // Try to match preferred color first
                if (!string.IsNullOrEmpty(rental.PreferredColor))
                {
                    vehicle = availableInGroup.FirstOrDefault(v =>
                        string.Equals(v.Color, rental.PreferredColor, StringComparison.OrdinalIgnoreCase));
                }

                // Fall back to any available
                vehicle ??= availableInGroup.First();
            }
        }

        if (vehicle == null)
            return null;

        // Assign the vehicle to the rental
        using var session = this.Context.OpenSession(username);

        rental.VehicleId = vehicle.VehicleId;
        rental.VehicleName = $"{vehicle.Brand} {vehicle.Model} {vehicle.LicensePlate}";
        session.Attach(rental);

        // Update vehicle status
        vehicle.Status = VehicleStatus.Rented;
        session.Attach(vehicle);

        await session.SubmitChanges("AssignVehicle");

        return vehicle;
    }

    /// <summary>
    /// Gets available vehicles that match a group reservation's criteria.
    /// Used by staff to see which vehicles can be assigned to a reservation.
    /// </summary>
    public async Task<List<Vehicle>> GetAvailableVehiclesForReservationAsync(int rentalId)
    {
        var rental = await this.Context.LoadOneAsync<Rental>(r => r.RentalId == rentalId);
        if (rental == null || string.IsNullOrEmpty(rental.VehicleGroupKey))
            return [];

        var vehicles = await this.Context.LoadAsync(
            this.Context.CreateQuery<Vehicle>()
                .Where(v => v.Status == VehicleStatus.Available));

        return vehicles.ItemCollection
            .Where(v => VehicleGroup.CreateGroupKey(v) == rental.VehicleGroupKey)
            .ToList();
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
        return @"VEHICLE RENTAL AGREEMENT

I agree to the following terms and conditions:

1. I will return the vehicle in the same condition as received.
2. I am responsible for any damage caused during the rental period.
3. I will not exceed the agreed rental period without prior notification.
4. I have a valid driving license for this vehicle type.
5. I will follow all traffic laws and regulations.
6. I will not sublet or lend the vehicle to any third party.

By signing below, I acknowledge that I have read, understood, and agree to these terms.";
    }

    /// <summary>
    /// Makes agent commission eligible when a rental linked to an agent booking is completed.
    /// </summary>
    private async Task MakeAgentCommissionEligibleAsync(Rental rental, string username)
    {
        if (!rental.BookingId.HasValue || BookingService == null || CommissionService == null)
            return;

        var booking = await BookingService.GetBookingByIdAsync(rental.BookingId.Value);
        if (booking == null || !booking.IsAgentBooking)
            return;

        var commission = await CommissionService.GetCommissionByBookingAsync(booking.BookingId);
        if (commission != null && commission.Status == AgentCommissionStatus.Pending && !commission.RentalId.HasValue)
        {
            await CommissionService.MakeEligibleAsync(commission.AgentCommissionId, rental.RentalId, username);
        }
    }

    #endregion
}

#region DTOs

public class CheckInRequest
{
    public int ShopId { get; set; }
    public int RenterId { get; set; }
    public int VehicleId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset ExpectedEndDate { get; set; }
    public int MileageStart { get; set; }
    public decimal RentalRate { get; set; }
    public decimal TotalAmount { get; set; }
    public int? InsuranceId { get; set; }
    public string? Notes { get; set; }

    // Duration type for different vehicle types
    public RentalDurationType DurationType { get; set; } = RentalDurationType.Daily;
    public int? IntervalMinutes { get; set; }

    // Driver/Guide options
    public bool IncludeDriver { get; set; }
    public bool IncludeGuide { get; set; }
    public decimal DriverFee { get; set; }
    public decimal GuideFee { get; set; }

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

    // Pick-up location and fees
    public int? PickupLocationId { get; set; }
    public string? PickupLocationName { get; set; }
    public TimeSpan? ScheduledPickupTime { get; set; }
    public decimal PickupLocationFee { get; set; }
    public decimal OutOfHoursPickupFee { get; set; }
    public bool IsOutOfHoursPickup { get; set; }
    public string? OutOfHoursPickupBand { get; set; }

    // Expected drop-off location and fees (for reservations)
    public int? DropoffLocationId { get; set; }
    public string? DropoffLocationName { get; set; }
    public TimeSpan? ScheduledDropoffTime { get; set; }
    public decimal DropoffLocationFee { get; set; }
    public decimal OutOfHoursDropoffFee { get; set; }
    public bool IsOutOfHoursDropoff { get; set; }
    public string? OutOfHoursDropoffBand { get; set; }

    // Pre-rental inspection
    public InspectionInfo? PreRentalInspection { get; set; }

    // Backward compatibility
    [Obsolete("Use VehicleId instead")]
    public int MotorbikeId { get => VehicleId; set => VehicleId = value; }

    [Obsolete("Use RentalRate instead")]
    public decimal DailyRate { get => RentalRate; set => RentalRate = value; }
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

    // For cross-shop returns (pooled vehicles)
    public int? ReturnShopId { get; set; }

    // Drop-off location and fees
    public int? DropoffLocationId { get; set; }
    public string? DropoffLocationName { get; set; }
    public TimeSpan? ScheduledDropoffTime { get; set; }
    public decimal DropoffLocationFee { get; set; }
    public decimal OutOfHoursDropoffFee { get; set; }
    public bool IsOutOfHoursDropoff { get; set; }
    public string? OutOfHoursDropoffBand { get; set; }

    // Post-rental inspection
    public InspectionInfo? PostRentalInspection { get; set; }

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
    public bool IsCrossShopReturn { get; set; }

    public static CheckOutResult CreateSuccess(int rentalId, decimal additionalCharges, decimal refundAmount, int extraDays, bool isCrossShopReturn = false) => new()
    {
        Success = true,
        RentalId = rentalId,
        AdditionalCharges = additionalCharges,
        RefundAmount = refundAmount,
        ExtraDays = extraDays,
        IsCrossShopReturn = isCrossShopReturn
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
    public int VehicleId { get; set; }

    /// <summary>
    /// For group-based reservations: the vehicle group key (Brand|Model|Year|Type|Engine).
    /// When set, VehicleId may be 0 until a specific vehicle is assigned at check-in.
    /// </summary>
    public string? VehicleGroupKey { get; set; }

    /// <summary>
    /// Optional color preference for group reservations (not guaranteed).
    /// </summary>
    public string? PreferredColor { get; set; }

    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int? InsuranceId { get; set; }
    public decimal RentalRate { get; set; }
    public decimal TotalAmount { get; set; }

    // Duration type
    public RentalDurationType DurationType { get; set; } = RentalDurationType.Daily;
    public int? IntervalMinutes { get; set; }

    // Driver/Guide options
    public bool IncludeDriver { get; set; }
    public bool IncludeGuide { get; set; }
    public decimal DriverFee { get; set; }
    public decimal GuideFee { get; set; }

    // Contact info
    public string RenterName { get; set; } = string.Empty;
    public string RenterPhone { get; set; } = string.Empty;
    public string RenterEmail { get; set; } = string.Empty;
    public string? RenterNationality { get; set; }
    public string? RenterPassport { get; set; }
    public string? HotelName { get; set; }
    public string? Notes { get; set; }

    // Backward compatibility
    [Obsolete("Use VehicleId instead")]
    public int MotorbikeId { get => VehicleId; set => VehicleId = value; }

    [Obsolete("Use RentalRate instead")]
    public decimal DailyRate { get => RentalRate; set => RentalRate = value; }
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

/// <summary>
/// Statistics about dynamic pricing usage and revenue impact.
/// </summary>
public class DynamicPricingStats
{
    /// <summary>
    /// Total number of rentals in the period.
    /// </summary>
    public int TotalRentals { get; set; }

    /// <summary>
    /// Number of rentals with dynamic pricing applied.
    /// </summary>
    public int RentalsWithDynamicPricing { get; set; }

    /// <summary>
    /// What revenue would have been without dynamic pricing.
    /// </summary>
    public decimal BaseRevenue { get; set; }

    /// <summary>
    /// Actual revenue with dynamic pricing.
    /// </summary>
    public decimal ActualRevenue { get; set; }

    /// <summary>
    /// Additional revenue earned from dynamic pricing (ActualRevenue - BaseRevenue).
    /// </summary>
    public decimal DynamicPricingPremium { get; set; }

    /// <summary>
    /// Average multiplier applied across all dynamic pricing rentals.
    /// </summary>
    public decimal AverageMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Breakdown by pricing rule name (e.g., "High Season" -> count of days applied).
    /// </summary>
    public Dictionary<string, int> RuleBreakdown { get; set; } = [];

    /// <summary>
    /// Percentage of rentals using dynamic pricing.
    /// </summary>
    public decimal DynamicPricingUsageRate => TotalRentals > 0
        ? (decimal)RentalsWithDynamicPricing / TotalRentals * 100
        : 0;

    /// <summary>
    /// Percentage increase in revenue from dynamic pricing.
    /// </summary>
    public decimal RevenueIncrease => BaseRevenue > 0
        ? DynamicPricingPremium / BaseRevenue * 100
        : 0;
}

#endregion
