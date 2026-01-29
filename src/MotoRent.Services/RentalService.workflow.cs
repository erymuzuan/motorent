using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;
using MotoRent.Domain.Settings;

namespace MotoRent.Services;

public partial class RentalService
{
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

            // Load renter for denormalized name
            var renter = await this.Context.LoadOneAsync<Renter>(r => r.RenterId == request.RenterId);

            // 1. Create rental with new fields
            var rental = new Rental
            {
                RentedFromShopId = request.ShopId,
                VehiclePoolId = vehicle.VehiclePoolId,
                RenterId = request.RenterId,
                RenterName = renter?.FullName,
                VehicleId = request.VehicleId,
                DurationType = request.DurationType,
                IntervalMinutes = request.IntervalMinutes,
                RentalHours = request.RentalHours,
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
                PreRentalInspection = request.PreRentalInspection,
                // Till session
                TillSessionId = request.TillSessionId
            };
            // 2. Update vehicle status to "Rented"
            vehicle.Status = VehicleStatus.Rented;
            if (vehicle.SupportsMileageTracking)
                vehicle.Mileage = request.MileageStart;
            session.Attach(vehicle);
            session.Attach(rental);

            // Submit rental first to get the RentalId
            var rentalResult = await session.SubmitChanges("CheckIn-Rental");
            if (!rentalResult.Success)
                return CheckInResult.CreateFailure(rentalResult.Message ?? "Check-in failed");

            // Now create child entities with the actual RentalId
            using var childSession = this.Context.OpenSession(username);

            // 3. Create deposit
            if (request.DepositAmount > 0)
            {
                var deposit = new Deposit
                {
                    RentalId = rental.RentalId,
                    DepositType = request.DepositType,
                    Amount = request.DepositAmount,
                    Status = "Held",
                    CardLast4 = request.CardLast4,
                    TransactionRef = request.TransactionRef,
                    CollectedOn = DateTimeOffset.Now
                };
                childSession.Attach(deposit);
            }

            // 4. Create rental accessories
            foreach (var accessory in request.Accessories)
            {
                var rentalAccessory = new RentalAccessory
                {
                    RentalId = rental.RentalId,
                    AccessoryId = accessory.AccessoryId,
                    Quantity = accessory.Quantity,
                    ChargedAmount = accessory.ChargedAmount
                };
                childSession.Attach(rentalAccessory);
            }

            // 5. Create rental agreement with signature
            if (!string.IsNullOrEmpty(request.SignatureImagePath))
            {
                var agreement = new RentalAgreement
                {
                    RentalId = rental.RentalId,
                    AgreementText = request.AgreementText ?? GetDefaultAgreementText(),
                    SignatureImagePath = request.SignatureImagePath,
                    SignedOn = DateTimeOffset.Now,
                    SignedByIp = request.SignedByIp
                };
                childSession.Attach(agreement);
            }

            // 6. Create payment record for rental amount
            if (request.TotalAmount > 0)
            {
                var durationNote = request.DurationType switch
                {
                    RentalDurationType.FixedInterval => $"{request.IntervalMinutes} minutes",
                    RentalDurationType.Hourly => $"{(int)Math.Ceiling((request.ExpectedEndDate - request.StartDate).TotalHours)} hours",
                    _ => $"{(request.ExpectedEndDate - request.StartDate).TotalDays:N0} days"
                };

                var rentalPayment = new Payment
                {
                    RentalId = rental.RentalId,
                    PaymentType = "Rental",
                    PaymentMethod = request.PaymentMethod,
                    Amount = request.TotalAmount,
                    Status = "Completed",
                    TransactionRef = request.PaymentTransactionRef,
                    PaidOn = DateTimeOffset.Now,
                    Notes = $"Rental payment: {durationNote}"
                };
                childSession.Attach(rentalPayment);
            }

            // 7. Create payment record for deposit
            if (request.DepositAmount > 0)
            {
                var depositPayment = new Payment
                {
                    RentalId = rental.RentalId,
                    PaymentType = "Deposit",
                    PaymentMethod = request.DepositType == "Cash" ? "Cash" : "Card",
                    Amount = request.DepositAmount,
                    Status = "Completed",
                    TransactionRef = request.TransactionRef,
                    PaidOn = DateTimeOffset.Now,
                    Notes = $"Security deposit ({request.DepositType})"
                };
                childSession.Attach(depositPayment);
            }

            var result = await childSession.SubmitChanges("CheckIn-Children");

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
        var renter = await this.Context.LoadOneAsync<Renter>(r => r.RenterId == request.RenterId);

        var rental = new Rental
        {
            RentedFromShopId = request.ShopId,
            RenterId = request.RenterId,
            RenterName = renter?.FullName,
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
            PreRentalInspection = request.PreRentalInspection,
            TillSessionId = request.TillSessionId
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

            // Fuel assessment
            if (request.FuelLevelAtReturn.HasValue)
                rental.FuelLevelAtCheckOut = request.FuelLevelAtReturn;
            rental.FuelSurcharge = request.FuelSurcharge;

            // Cleanliness assessment
            if (!string.IsNullOrEmpty(request.CleanlinessLevel))
                rental.CleanlinessLevel = request.CleanlinessLevel;
            rental.CleaningFee = request.CleaningFee;

            // Accessories return
            if (request.ReturnedAccessories is { Count: > 0 })
                rental.ReturnedAccessories = request.ReturnedAccessories;

            session.Attach(rental);

            // 5. Calculate additional charges
            decimal additionalCharges = 0;
            int extraDays = 0;
            int extraHours = 0;

            // Load daily late fee mode setting (applies to Daily rentals only)
            string dailyLateFeeMode = "Daily";
            if (rental.DurationType == RentalDurationType.Daily && SettingConfig != null)
            {
                dailyLateFeeMode = await SettingConfig.GetStringAsync(
                    SettingKeys.Rental_DailyLateFeeMode, defaultValue: "Daily") ?? "Daily";
            }

            if (rental.DurationType == RentalDurationType.Daily)
            {
                // Check if org uses hourly late fees for daily rentals
                if (dailyLateFeeMode == "Hourly" && vehicle != null
                    && vehicle.HourlyRate.HasValue && vehicle.HourlyRate > 0)
                {
                    extraHours = CalculateExtraHours(rental.ExpectedEndDate, request.ActualEndDate);
                    if (extraHours > 0)
                    {
                        additionalCharges += extraHours * vehicle.HourlyRate.Value;
                    }
                }
                else
                {
                    extraDays = CalculateExtraDays(rental.ExpectedEndDate, request.ActualEndDate);
                    if (extraDays > 0)
                    {
                        additionalCharges += extraDays * rental.RentalRate;
                    }
                }
            }
            else if (rental.DurationType == RentalDurationType.Hourly)
            {
                // Hourly rental: charge per extra hour at the rental rate
                extraHours = CalculateExtraHours(rental.ExpectedEndDate, request.ActualEndDate);
                if (extraHours > 0)
                {
                    additionalCharges += extraHours * rental.RentalRate;
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

            // 5b. Add fuel, cleaning, and accessory charges
            additionalCharges += request.FuelSurcharge;
            additionalCharges += request.CleaningFee;
            additionalCharges += request.AccessoryMissingCharge;

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
                var damageCharges = additionalCharges
                    - (extraDays * rental.RentalRate)
                    - (rental.DurationType == RentalDurationType.Hourly
                        ? extraHours * rental.RentalRate
                        : 0)
                    - (rental.DurationType == RentalDurationType.Daily && dailyLateFeeMode == "Hourly"
                        && vehicle?.HourlyRate > 0
                        ? extraHours * vehicle.HourlyRate.Value
                        : 0);

                string paymentNotes;
                if (rental.DurationType == RentalDurationType.Daily && dailyLateFeeMode == "Hourly" && extraHours > 0)
                {
                    paymentNotes = $"Extra hours (late): {extraHours}, Damage charges: {damageCharges}";
                }
                else if (rental.DurationType == RentalDurationType.Daily)
                {
                    paymentNotes = $"Extra days: {extraDays}, Damage charges: {damageCharges}";
                }
                else if (rental.DurationType == RentalDurationType.Hourly)
                {
                    paymentNotes = $"Extra hours: {extraHours}, Damage charges: {damageCharges}";
                }
                else
                {
                    paymentNotes = $"Overtime charges: {additionalCharges}";
                }

                var payment = new Payment
                {
                    RentalId = request.RentalId,
                    PaymentType = "Additional",
                    PaymentMethod = request.PaymentMethod ?? "Cash",
                    Amount = additionalCharges,
                    Status = "Completed",
                    PaidOn = DateTimeOffset.Now,
                    Notes = paymentNotes
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

    private static int CalculateExtraDays(DateTimeOffset expectedEnd, DateTimeOffset actualEnd)
    {
        if (actualEnd <= expectedEnd)
            return 0;

        return (int)Math.Ceiling((actualEnd - expectedEnd).TotalDays);
    }

    private static int CalculateExtraHours(DateTimeOffset expectedEnd, DateTimeOffset actualEnd)
    {
        if (actualEnd <= expectedEnd)
            return 0;

        return (int)Math.Ceiling((actualEnd - expectedEnd).TotalHours);
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
}
