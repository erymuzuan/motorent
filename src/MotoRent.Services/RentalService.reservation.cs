using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;

namespace MotoRent.Services;

public partial class RentalService
{
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
                        .Where(r => r.VehicleId == request.VehicleId
                            && (r.Status == "Active" || r.Status == "Reserved")
                            && r.StartDate < request.EndDate && r.ExpectedEndDate > request.StartDate));

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
}
