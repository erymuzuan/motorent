using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

/// <summary>
/// Service for managing bookings/reservations.
/// Supports cross-shop flexibility - customers can check-in at any shop with matching vehicles.
/// </summary>
public class BookingService
{
    private readonly RentalDataContext m_context;
    private readonly VehicleService m_vehicleService;
    private static readonly Random s_random = new();
    private const string c_bookingRefChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Avoiding confusing chars: 0,O,1,I

    public BookingService(RentalDataContext context, VehicleService vehicleService)
    {
        m_context = context;
        m_vehicleService = vehicleService;
    }

    #region Core Methods

    /// <summary>
    /// Creates a new booking.
    /// </summary>
    public async Task<CreateBookingResult> CreateBookingAsync(CreateBookingRequest request, string username)
    {
        var booking = new Booking
        {
            BookingRef = await GenerateBookingRefAsync(),
            Status = BookingStatus.Pending,
            PreferredShopId = request.PreferredShopId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            CustomerEmail = request.CustomerEmail,
            CustomerNationality = request.CustomerNationality,
            CustomerPassportNo = request.CustomerPassportNo,
            HotelName = request.HotelName,
            Notes = request.Notes,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PickupTime = request.PickupTime,
            PickupLocationId = request.PickupLocationId,
            PickupLocationName = request.PickupLocationName,
            PickupLocationFee = request.PickupLocationFee,
            DropoffLocationId = request.DropoffLocationId,
            DropoffLocationName = request.DropoffLocationName,
            DropoffLocationFee = request.DropoffLocationFee,
            BookingSource = request.BookingSource,
            PreferredShopName = request.PreferredShopName
        };

        // Add items
        foreach (var itemRequest in request.Items)
        {
            var item = new BookingItem
            {
                VehicleGroupKey = itemRequest.VehicleGroupKey,
                PreferredVehicleId = itemRequest.PreferredVehicleId,
                PreferredColor = itemRequest.PreferredColor,
                InsuranceId = itemRequest.InsuranceId,
                AccessoryIds = itemRequest.AccessoryIds ?? [],
                DailyRate = itemRequest.DailyRate,
                InsuranceRate = itemRequest.InsuranceRate,
                AccessoriesTotal = itemRequest.AccessoriesTotal,
                DepositAmount = itemRequest.DepositAmount,
                ItemTotal = itemRequest.ItemTotal,
                VehicleDisplayName = itemRequest.VehicleDisplayName,
                InsuranceName = itemRequest.InsuranceName
            };
            booking.Items.Add(item);
        }

        // Calculate totals
        RecalculateTotals(booking);

        // Add creation history
        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.Created,
            Description = $"Booking created via {request.BookingSource}"
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        var result = await session.SubmitChanges("Create");

        if (result.Success)
        {
            return CreateBookingResult.CreateSuccess(booking);
        }

        return CreateBookingResult.CreateFailure(result.Message ?? "Failed to create booking");
    }

    /// <summary>
    /// Gets a booking by its reference code.
    /// </summary>
    public async Task<Booking?> GetBookingByRefAsync(string bookingRef)
    {
        return await m_context.LoadOneAsync<Booking>(b => b.BookingRef == bookingRef);
    }

    /// <summary>
    /// Gets a booking by ID.
    /// </summary>
    public async Task<Booking?> GetBookingByIdAsync(int bookingId)
    {
        return await m_context.LoadOneAsync<Booking>(b => b.BookingId == bookingId);
    }

    /// <summary>
    /// Gets bookings with filters.
    /// </summary>
    public async Task<LoadOperation<Booking>> GetBookingsAsync(
        int? preferredShopId = null,
        string? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.CreateQuery<Booking>();

        if (preferredShopId.HasValue)
        {
            query = query.Where(b => b.PreferredShopId == preferredShopId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(b => b.StartDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.StartDate <= toDate.Value);
        }

        query = query.OrderByDescending(b => b.BookingId);

        return await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    /// <summary>
    /// Gets upcoming bookings for a specific date.
    /// </summary>
    public async Task<List<Booking>> GetUpcomingBookingsAsync(DateTimeOffset date, int? shopId = null)
    {
        var dateStart = date.Date;
        var dateEnd = dateStart.AddDays(1);

        var query = m_context.CreateQuery<Booking>()
            .Where(b => b.StartDate >= dateStart && b.StartDate < dateEnd)
            .Where(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed);

        if (shopId.HasValue)
        {
            query = query.Where(b => b.PreferredShopId == shopId.Value);
        }

        var result = await m_context.LoadAsync(query, 1, 1000, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Gets upcoming bookings for a date range with pagination (for dashboard widget).
    /// </summary>
    public async Task<LoadOperation<Booking>> GetUpcomingBookingsAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        int page = 1,
        int size = 10)
    {
        var query = m_context.CreateQuery<Booking>()
            .Where(b => b.StartDate >= startDate && b.StartDate < endDate)
            .Where(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed)
            .OrderBy(b => b.StartDate);

        return await m_context.LoadAsync(query, page, size, includeTotalRows: true);
    }

    /// <summary>
    /// Gets bookings for a tourist by email or phone (for rental history page).
    /// Only returns bookings that haven't been checked in yet (Pending, Confirmed).
    /// </summary>
    public async Task<List<Booking>> GetBookingsForTouristAsync(string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phone))
            return [];

        var query = m_context.CreateQuery<Booking>()
            .Where(b => b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed);

        // Filter by email or phone
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(phone))
        {
            query = query.Where(b => b.CustomerEmail == email || b.CustomerPhone == phone);
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            query = query.Where(b => b.CustomerEmail == email);
        }
        else
        {
            query = query.Where(b => b.CustomerPhone == phone);
        }

        query = query.OrderByDescending(b => b.StartDate);

        var result = await m_context.LoadAsync(query, 1, 50, includeTotalRows: false);
        return result.ItemCollection.ToList();
    }

    /// <summary>
    /// Generates a unique 6-character booking reference.
    /// </summary>
    public async Task<string> GenerateBookingRefAsync()
    {
        string bookingRef;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            bookingRef = GenerateRandomRef(6);
            var existing = await m_context.LoadOneAsync<Booking>(b => b.BookingRef == bookingRef);
            if (existing == null)
            {
                return bookingRef;
            }
            attempts++;
        } while (attempts < maxAttempts);

        // Fallback: include timestamp component
        return GenerateRandomRef(4) + DateTime.UtcNow.ToString("HH");
    }

    private static string GenerateRandomRef(int length)
    {
        var chars = new char[length];
        for (int i = 0; i < length; i++)
        {
            chars[i] = c_bookingRefChars[s_random.Next(c_bookingRefChars.Length)];
        }
        return new string(chars);
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Confirms a booking.
    /// </summary>
    public async Task<SubmitOperation> ConfirmBookingAsync(int bookingId, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        var oldStatus = booking.Status;
        booking.Status = BookingStatus.Confirmed;

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.StatusChange,
            Description = $"Status changed from {oldStatus} to {BookingStatus.Confirmed}",
            OldValue = oldStatus,
            NewValue = BookingStatus.Confirmed
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("Confirm");
    }

    /// <summary>
    /// Cancels a booking.
    /// </summary>
    public async Task<CancelBookingResult> CancelBookingAsync(int bookingId, string reason, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return CancelBookingResult.CreateFailure("Booking not found");
        }

        if (!booking.CanBeCancelled)
        {
            return CancelBookingResult.CreateFailure("Booking cannot be cancelled in its current state");
        }

        var refundAmount = CalculateRefund(booking);

        var oldStatus = booking.Status;
        booking.Status = BookingStatus.Cancelled;
        booking.CancelledOn = DateTimeOffset.UtcNow;
        booking.CancellationReason = reason;
        booking.RefundAmount = refundAmount;

        // Cancel all pending items
        foreach (var item in booking.Items.Where(i => i.IsPending))
        {
            item.ItemStatus = BookingItemStatus.Cancelled;
        }

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.Cancelled,
            Description = $"Booking cancelled. Reason: {reason}. Refund: {refundAmount:C}",
            OldValue = oldStatus,
            NewValue = BookingStatus.Cancelled
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        var result = await session.SubmitChanges("Cancel");

        if (result.Success)
        {
            return CancelBookingResult.CreateSuccess(refundAmount);
        }

        return CancelBookingResult.CreateFailure(result.Message ?? "Failed to cancel booking");
    }

    /// <summary>
    /// Updates booking status.
    /// </summary>
    public async Task<SubmitOperation> UpdateStatusAsync(int bookingId, string newStatus, string? notes, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        var oldStatus = booking.Status;
        booking.Status = newStatus;

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.StatusChange,
            Description = string.IsNullOrEmpty(notes)
                ? $"Status changed from {oldStatus} to {newStatus}"
                : $"Status changed from {oldStatus} to {newStatus}. Notes: {notes}",
            OldValue = oldStatus,
            NewValue = newStatus
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("UpdateStatus");
    }

    #endregion

    #region Schedule & Location

    /// <summary>
    /// Updates booking dates.
    /// </summary>
    public async Task<SubmitOperation> UpdateDatesAsync(int bookingId, DateTimeOffset startDate, DateTimeOffset endDate, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        var oldDates = $"{booking.StartDate:d} - {booking.EndDate:d}";
        booking.StartDate = startDate;
        booking.EndDate = endDate;

        // Recalculate item totals based on new duration
        RecalculateTotals(booking);

        var newDates = $"{startDate:d} - {endDate:d}";
        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.DateChange,
            Description = $"Dates changed from {oldDates} to {newDates}",
            OldValue = oldDates,
            NewValue = newDates
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("UpdateDates");
    }

    /// <summary>
    /// Updates pickup time.
    /// </summary>
    public async Task<SubmitOperation> UpdatePickupTimeAsync(int bookingId, TimeSpan? pickupTime, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        var oldTime = booking.PickupTime?.ToString(@"hh\:mm") ?? "Not set";
        booking.PickupTime = pickupTime;
        var newTime = pickupTime?.ToString(@"hh\:mm") ?? "Not set";

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.LocationChange,
            Description = $"Pickup time changed from {oldTime} to {newTime}",
            OldValue = oldTime,
            NewValue = newTime
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("UpdatePickupTime");
    }

    /// <summary>
    /// Moves booking to a different shop.
    /// </summary>
    public async Task<SubmitOperation> MoveToShopAsync(int bookingId, int newShopId, string newShopName, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        var oldShop = booking.PreferredShopName ?? $"Shop {booking.PreferredShopId}";
        booking.PreferredShopId = newShopId;
        booking.PreferredShopName = newShopName;

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.ShopChange,
            Description = $"Preferred shop changed from {oldShop} to {newShopName}",
            OldValue = oldShop,
            NewValue = newShopName
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("MoveToShop");
    }

    #endregion

    #region Vehicle Selection

    /// <summary>
    /// Adds a vehicle item to the booking.
    /// </summary>
    public async Task<SubmitOperation> AddVehicleItemAsync(int bookingId, BookingItem item, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        booking.Items.Add(item);
        RecalculateTotals(booking);

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.VehicleAdded,
            Description = $"Vehicle added: {item.VehicleDisplayName}"
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("AddVehicle");
    }

    /// <summary>
    /// Removes a vehicle item from the booking.
    /// </summary>
    public async Task<SubmitOperation> RemoveVehicleItemAsync(int bookingId, string itemId, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        var item = booking.Items.FirstOrDefault(i => i.ItemId == itemId);
        if (item == null)
        {
            return SubmitOperation.CreateFailure("Item not found");
        }

        if (item.IsCheckedIn)
        {
            return SubmitOperation.CreateFailure("Cannot remove checked-in item");
        }

        booking.Items.Remove(item);
        RecalculateTotals(booking);

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.VehicleRemoved,
            Description = $"Vehicle removed: {item.VehicleDisplayName}"
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("RemoveVehicle");
    }

    /// <summary>
    /// Updates a vehicle item in the booking.
    /// </summary>
    public async Task<SubmitOperation> UpdateVehicleItemAsync(int bookingId, string itemId, BookingItem updatedItem, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        var existingItem = booking.Items.FirstOrDefault(i => i.ItemId == itemId);
        if (existingItem == null)
        {
            return SubmitOperation.CreateFailure("Item not found");
        }

        if (existingItem.IsCheckedIn)
        {
            return SubmitOperation.CreateFailure("Cannot update checked-in item");
        }

        var oldVehicle = existingItem.VehicleDisplayName;

        // Update item properties
        existingItem.VehicleGroupKey = updatedItem.VehicleGroupKey;
        existingItem.PreferredVehicleId = updatedItem.PreferredVehicleId;
        existingItem.PreferredColor = updatedItem.PreferredColor;
        existingItem.InsuranceId = updatedItem.InsuranceId;
        existingItem.InsuranceName = updatedItem.InsuranceName;
        existingItem.AccessoryIds = updatedItem.AccessoryIds;
        existingItem.DailyRate = updatedItem.DailyRate;
        existingItem.InsuranceRate = updatedItem.InsuranceRate;
        existingItem.AccessoriesTotal = updatedItem.AccessoriesTotal;
        existingItem.DepositAmount = updatedItem.DepositAmount;
        existingItem.ItemTotal = updatedItem.ItemTotal;
        existingItem.VehicleDisplayName = updatedItem.VehicleDisplayName;

        RecalculateTotals(booking);

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.VehicleChange,
            Description = $"Vehicle changed from {oldVehicle} to {updatedItem.VehicleDisplayName}",
            OldValue = oldVehicle,
            NewValue = updatedItem.VehicleDisplayName
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("UpdateVehicle");
    }

    #endregion

    #region Payment & Deposit

    /// <summary>
    /// Records a payment for the booking.
    /// </summary>
    public async Task<SubmitOperation> RecordPaymentAsync(int bookingId, BookingPayment payment, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        payment.RecordedBy = username;
        booking.Payments.Add(payment);
        booking.AmountPaid += payment.Amount;

        // Update payment status
        if (booking.AmountPaid >= booking.TotalAmount)
        {
            booking.PaymentStatus = BookingPaymentStatus.FullyPaid;
        }
        else if (booking.AmountPaid > 0)
        {
            booking.PaymentStatus = BookingPaymentStatus.PartiallyPaid;
        }

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.PaymentReceived,
            Description = $"Payment received: {payment.Amount:C} via {payment.PaymentMethod} ({payment.PaymentType})"
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("RecordPayment");
    }

    /// <summary>
    /// Processes a refund for the booking.
    /// </summary>
    public async Task<SubmitOperation> ProcessRefundAsync(int bookingId, decimal amount, string reason, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        var refundPayment = new BookingPayment
        {
            Amount = -amount, // Negative for refund
            PaymentType = BookingPaymentType.Refund,
            PaymentMethod = "Refund",
            Notes = reason,
            RecordedBy = username
        };

        booking.Payments.Add(refundPayment);
        booking.AmountPaid -= amount;
        booking.RefundAmount = (booking.RefundAmount ?? 0) + amount;

        // Update payment status
        if (booking.AmountPaid >= booking.TotalAmount)
        {
            booking.PaymentStatus = BookingPaymentStatus.FullyPaid;
        }
        else if (booking.AmountPaid > 0)
        {
            booking.PaymentStatus = BookingPaymentStatus.PartiallyPaid;
        }
        else
        {
            booking.PaymentStatus = BookingPaymentStatus.Unpaid;
        }

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.RefundProcessed,
            Description = $"Refund processed: {amount:C}. Reason: {reason}"
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("ProcessRefund");
    }

    /// <summary>
    /// Calculates refund amount based on cancellation policy.
    /// </summary>
    public decimal CalculateRefund(Booking booking)
    {
        if (booking.AmountPaid <= 0)
        {
            return 0;
        }

        // TODO: Implement cancellation policy from settings
        // For now, simple logic:
        // - More than 24 hours before: full refund
        // - Less than 24 hours: 50% refund
        // - No-show: no refund

        var hoursUntilPickup = (booking.StartDate - DateTimeOffset.UtcNow).TotalHours;

        if (hoursUntilPickup > 24)
        {
            return booking.AmountPaid;
        }
        else if (hoursUntilPickup > 0)
        {
            return booking.AmountPaid * 0.5m;
        }
        else
        {
            return 0;
        }
    }

    #endregion

    #region Customer Info

    /// <summary>
    /// Updates customer information.
    /// </summary>
    public async Task<SubmitOperation> UpdateCustomerInfoAsync(int bookingId, UpdateCustomerRequest request, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        var changes = new List<string>();

        if (request.CustomerName != null && request.CustomerName != booking.CustomerName)
        {
            changes.Add($"Name: {booking.CustomerName} → {request.CustomerName}");
            booking.CustomerName = request.CustomerName;
        }

        if (request.CustomerPhone != null && request.CustomerPhone != booking.CustomerPhone)
        {
            changes.Add($"Phone: {booking.CustomerPhone} → {request.CustomerPhone}");
            booking.CustomerPhone = request.CustomerPhone;
        }

        if (request.CustomerEmail != null && request.CustomerEmail != booking.CustomerEmail)
        {
            changes.Add($"Email: {booking.CustomerEmail} → {request.CustomerEmail}");
            booking.CustomerEmail = request.CustomerEmail;
        }

        if (request.CustomerNationality != null)
        {
            booking.CustomerNationality = request.CustomerNationality;
        }

        if (request.CustomerPassportNo != null)
        {
            booking.CustomerPassportNo = request.CustomerPassportNo;
        }

        if (request.HotelName != null)
        {
            booking.HotelName = request.HotelName;
        }

        if (request.Notes != null)
        {
            booking.Notes = request.Notes;
        }

        if (changes.Count > 0)
        {
            booking.ChangeHistory.Add(new BookingChange
            {
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedBy = username,
                ChangeType = BookingChangeType.CustomerInfoChange,
                Description = $"Customer info updated: {string.Join(", ", changes)}"
            });
        }

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("UpdateCustomer");
    }

    /// <summary>
    /// Links booking to an existing renter.
    /// </summary>
    public async Task<SubmitOperation> LinkRenterAsync(int bookingId, int renterId, string renterName, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        booking.RenterId = renterId;
        booking.RenterName = renterName;

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.CustomerInfoChange,
            Description = $"Linked to renter: {renterName} (ID: {renterId})"
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("LinkRenter");
    }

    #endregion

    #region Cross-Shop Methods

    /// <summary>
    /// Finds available vehicles at a shop matching the vehicle group key.
    /// </summary>
    public async Task<List<Vehicle>> FindMatchingVehiclesAsync(int shopId, string vehicleGroupKey)
    {
        var vehicles = await m_context.LoadAsync(
            m_context.CreateQuery<Vehicle>()
                .Where(v => v.CurrentShopId == shopId)
                .Where(v => v.Status == VehicleStatus.Available),
            1, 1000, includeTotalRows: false);

        // Filter by vehicle group key (Brand|Model|Year|Type|Engine)
        return vehicles.ItemCollection
            .Where(v => v.GetGroupKey() == vehicleGroupKey)
            .ToList();
    }

    /// <summary>
    /// Checks if a shop can fulfill all items in the booking.
    /// </summary>
    public async Task<bool> CanFulfillAtShopAsync(int bookingId, int shopId)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        foreach (var item in booking.Items.Where(i => i.IsPending))
        {
            var matchingVehicles = await FindMatchingVehiclesAsync(shopId, item.VehicleGroupKey);
            if (matchingVehicles.Count == 0)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks in a booking item at a specific shop.
    /// </summary>
    public async Task<CheckInItemResult> CheckInItemAsync(int bookingId, string itemId, int assignedVehicleId, int shopId, int rentalId, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return CheckInItemResult.CreateFailure("Booking not found");
        }

        var item = booking.Items.FirstOrDefault(i => i.ItemId == itemId);
        if (item == null)
        {
            return CheckInItemResult.CreateFailure("Item not found");
        }

        if (item.IsCheckedIn)
        {
            return CheckInItemResult.CreateFailure("Item already checked in");
        }

        // Get the assigned vehicle
        var vehicle = await m_context.LoadOneAsync<Vehicle>(v => v.VehicleId == assignedVehicleId);
        if (vehicle == null)
        {
            return CheckInItemResult.CreateFailure("Vehicle not found");
        }

        // Get shop name
        var shop = await m_context.LoadOneAsync<Shop>(s => s.ShopId == shopId);
        var shopName = shop?.Name ?? $"Shop {shopId}";

        // Update item
        item.AssignedVehicleId = assignedVehicleId;
        item.AssignedVehiclePlate = vehicle.LicensePlate;
        item.AssignedVehicleName = vehicle.DisplayName;
        item.RentalId = rentalId;
        item.ItemStatus = BookingItemStatus.CheckedIn;

        // Update booking
        if (!booking.CheckedInAtShopId.HasValue)
        {
            booking.CheckedInAtShopId = shopId;
            booking.CheckedInAtShopName = shopName;
        }

        // Update status if all items checked in
        if (booking.AllItemsCheckedIn)
        {
            booking.Status = BookingStatus.CheckedIn;
        }

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.CheckedIn,
            Description = $"Item checked in at {shopName}: {item.VehicleDisplayName} → {vehicle.LicensePlate}"
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        var result = await session.SubmitChanges("CheckInItem");

        if (result.Success)
        {
            return CheckInItemResult.CreateSuccess(item);
        }

        return CheckInItemResult.CreateFailure(result.Message ?? "Failed to check in item");
    }

    #endregion

    #region Agent Booking Methods

    /// <summary>
    /// Applies an agent to an existing booking.
    /// </summary>
    public async Task<SubmitOperation> ApplyAgentToBookingAsync(
        int bookingId,
        int agentId,
        string agentCode,
        string agentName,
        decimal commission,
        decimal surcharge,
        bool surchargeHidden,
        string paymentFlow,
        string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        booking.AgentId = agentId;
        booking.AgentCode = agentCode;
        booking.AgentName = agentName;
        booking.IsAgentBooking = true;
        booking.AgentCommission = commission;
        booking.AgentSurcharge = surcharge;
        booking.SurchargeHidden = surchargeHidden;
        booking.AgentPaymentFlow = paymentFlow;

        // Calculate customer visible total and shop receivable
        booking.CustomerVisibleTotal = surchargeHidden
            ? booking.TotalAmount + surcharge
            : booking.TotalAmount;
        booking.ShopReceivableAmount = booking.TotalAmount - commission;

        // Update booking source
        booking.BookingSource = "Agent";

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.AgentAssigned,
            Description = $"Agent assigned: {agentName} ({agentCode}). Commission: {commission:C}, Surcharge: {surcharge:C}"
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("ApplyAgent");
    }

    /// <summary>
    /// Removes agent from a booking.
    /// </summary>
    public async Task<SubmitOperation> RemoveAgentFromBookingAsync(int bookingId, string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        if (!booking.IsAgentBooking)
        {
            return SubmitOperation.CreateFailure("Booking is not an agent booking");
        }

        var oldAgent = booking.AgentName;

        booking.AgentId = null;
        booking.AgentCode = null;
        booking.AgentName = null;
        booking.IsAgentBooking = false;
        booking.AgentCommission = 0;
        booking.AgentSurcharge = 0;
        booking.SurchargeHidden = false;
        booking.AgentPaymentFlow = PaymentFlow.CustomerPaysShop;
        booking.CustomerVisibleTotal = 0;
        booking.ShopReceivableAmount = 0;
        booking.BookingSource = "Staff";

        booking.ChangeHistory.Add(new BookingChange
        {
            ChangedAt = DateTimeOffset.UtcNow,
            ChangedBy = username,
            ChangeType = BookingChangeType.AgentRemoved,
            Description = $"Agent removed: {oldAgent}"
        });

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("RemoveAgent");
    }

    /// <summary>
    /// Gets bookings for a specific agent.
    /// </summary>
    public async Task<LoadOperation<Booking>> GetAgentBookingsAsync(
        int agentId,
        string? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = m_context.CreateQuery<Booking>()
            .Where(b => b.AgentId == agentId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(b => b.Status == status);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(b => b.StartDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(b => b.StartDate <= toDate.Value);
        }

        query = query.OrderByDescending(b => b.BookingId);

        return await m_context.LoadAsync(query, page, pageSize, includeTotalRows: true);
    }

    /// <summary>
    /// Updates agent financial details on booking.
    /// </summary>
    public async Task<SubmitOperation> UpdateAgentFinancialsAsync(
        int bookingId,
        decimal commission,
        decimal surcharge,
        bool surchargeHidden,
        string username)
    {
        var booking = await GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            return SubmitOperation.CreateFailure("Booking not found");
        }

        if (!booking.IsAgentBooking)
        {
            return SubmitOperation.CreateFailure("Booking is not an agent booking");
        }

        var changes = new List<string>();
        if (booking.AgentCommission != commission)
        {
            changes.Add($"Commission: {booking.AgentCommission:C} → {commission:C}");
            booking.AgentCommission = commission;
        }

        if (booking.AgentSurcharge != surcharge)
        {
            changes.Add($"Surcharge: {booking.AgentSurcharge:C} → {surcharge:C}");
            booking.AgentSurcharge = surcharge;
        }

        if (booking.SurchargeHidden != surchargeHidden)
        {
            changes.Add($"Surcharge hidden: {booking.SurchargeHidden} → {surchargeHidden}");
            booking.SurchargeHidden = surchargeHidden;
        }

        // Recalculate derived amounts
        booking.CustomerVisibleTotal = surchargeHidden
            ? booking.TotalAmount + surcharge
            : booking.TotalAmount;
        booking.ShopReceivableAmount = booking.TotalAmount - commission;

        if (changes.Count > 0)
        {
            booking.ChangeHistory.Add(new BookingChange
            {
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedBy = username,
                ChangeType = BookingChangeType.AgentFinancialsUpdated,
                Description = $"Agent financials updated: {string.Join(", ", changes)}"
            });
        }

        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("UpdateAgentFinancials");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Recalculates booking totals based on items.
    /// </summary>
    private void RecalculateTotals(Booking booking)
    {
        var days = booking.Days;

        foreach (var item in booking.Items)
        {
            item.ItemTotal = (item.DailyRate + item.InsuranceRate + item.AccessoriesTotal) * days;
        }

        booking.TotalAmount = booking.Items.Sum(i => i.ItemTotal) + booking.PickupLocationFee + booking.DropoffLocationFee;
        booking.DepositRequired = booking.Items.Sum(i => i.DepositAmount);
    }

    /// <summary>
    /// Saves booking changes.
    /// </summary>
    public async Task<SubmitOperation> SaveBookingAsync(Booking booking, string username)
    {
        using var session = m_context.OpenSession(username);
        session.Attach(booking);
        return await session.SubmitChanges("Update");
    }

    #endregion
}

#region Request/Result Models

public class CreateBookingRequest
{
    public int? PreferredShopId { get; set; }
    public string? PreferredShopName { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerNationality { get; set; }
    public string? CustomerPassportNo { get; set; }
    public string? HotelName { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public TimeSpan? PickupTime { get; set; }
    public int? PickupLocationId { get; set; }
    public string? PickupLocationName { get; set; }
    public decimal PickupLocationFee { get; set; }
    public int? DropoffLocationId { get; set; }
    public string? DropoffLocationName { get; set; }
    public decimal DropoffLocationFee { get; set; }
    public string BookingSource { get; set; } = "Staff";
    public List<BookingItemRequest> Items { get; set; } = [];
}

public class BookingItemRequest
{
    public string VehicleGroupKey { get; set; } = string.Empty;
    public int? PreferredVehicleId { get; set; }
    public string? PreferredColor { get; set; }
    public int? InsuranceId { get; set; }
    public string? InsuranceName { get; set; }
    public List<int> AccessoryIds { get; set; } = [];
    public decimal DailyRate { get; set; }
    public decimal InsuranceRate { get; set; }
    public decimal AccessoriesTotal { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal ItemTotal { get; set; }
    public string? VehicleDisplayName { get; set; }
}

public class UpdateCustomerRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerNationality { get; set; }
    public string? CustomerPassportNo { get; set; }
    public string? HotelName { get; set; }
    public string? Notes { get; set; }
}

public class CreateBookingResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public Booking? Booking { get; set; }

    public static CreateBookingResult CreateSuccess(Booking booking) => new() { Success = true, Booking = booking };
    public static CreateBookingResult CreateFailure(string error) => new() { Success = false, Error = error };
}

public class CancelBookingResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public decimal RefundAmount { get; set; }

    public static CancelBookingResult CreateSuccess(decimal refundAmount) => new() { Success = true, RefundAmount = refundAmount };
    public static CancelBookingResult CreateFailure(string error) => new() { Success = false, Error = error };
}

public class CheckInItemResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public BookingItem? Item { get; set; }

    public static CheckInItemResult CreateSuccess(BookingItem item) => new() { Success = true, Item = item };
    public static CheckInItemResult CreateFailure(string error) => new() { Success = false, Error = error };
}

#endregion
