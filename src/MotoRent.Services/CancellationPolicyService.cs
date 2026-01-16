using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Settings;

namespace MotoRent.Services;

/// <summary>
/// Service for calculating refunds based on shop cancellation policy.
/// </summary>
public class CancellationPolicyService
{
    private readonly RentalDataContext m_context;
    private readonly ISettingConfig m_settings;

    public CancellationPolicyService(RentalDataContext context, ISettingConfig settings)
    {
        m_context = context;
        m_settings = settings;
    }

    /// <summary>
    /// Calculates the refund amount based on shop cancellation policy.
    /// </summary>
    /// <param name="booking">The booking to calculate refund for</param>
    /// <returns>Refund calculation result</returns>
    public async Task<RefundCalculation> CalculateRefundAsync(Booking booking)
    {
        // Get shop settings
        var policy = await m_settings.GetStringAsync(SettingKeys.Booking_CancellationPolicy, defaultValue: "Free") ?? "Free";
        var freeCancelHours = await m_settings.GetIntAsync(SettingKeys.Booking_FreeCancelHours, defaultValue: 24);
        var lateCancelPenaltyPercent = await m_settings.GetIntAsync(SettingKeys.Booking_LateCancelPenaltyPercent, defaultValue: 50);
        var noShowPenaltyPercent = await m_settings.GetIntAsync(SettingKeys.Booking_NoShowPenaltyPercent, defaultValue: 100);

        var result = new RefundCalculation
        {
            BookingId = booking.BookingId,
            AmountPaid = booking.AmountPaid,
            Policy = policy
        };

        // No payment made, no refund needed
        if (booking.AmountPaid <= 0)
        {
            result.RefundAmount = 0;
            result.PenaltyAmount = 0;
            result.Reason = "No payment was made";
            return result;
        }

        // Calculate hours until pickup
        var hoursUntilPickup = (booking.StartDate - DateTimeOffset.Now).TotalHours;
        result.HoursUntilPickup = hoursUntilPickup;

        switch (policy.ToLowerInvariant())
        {
            case "free":
                // Always full refund
                result.RefundAmount = booking.AmountPaid;
                result.PenaltyAmount = 0;
                result.PenaltyPercent = 0;
                result.Reason = "Free cancellation policy - full refund";
                break;

            case "nonrefundable":
                // No refund
                result.RefundAmount = 0;
                result.PenaltyAmount = booking.AmountPaid;
                result.PenaltyPercent = 100;
                result.Reason = "Non-refundable booking - no refund available";
                break;

            case "timebased":
            default:
                // Time-based policy
                if (hoursUntilPickup <= 0)
                {
                    // No-show (past pickup time)
                    result.PenaltyPercent = noShowPenaltyPercent;
                    result.PenaltyAmount = booking.AmountPaid * noShowPenaltyPercent / 100;
                    result.RefundAmount = booking.AmountPaid - result.PenaltyAmount;
                    result.Reason = $"No-show penalty ({noShowPenaltyPercent}% of amount paid)";
                }
                else if (hoursUntilPickup < freeCancelHours)
                {
                    // Late cancellation
                    result.PenaltyPercent = lateCancelPenaltyPercent;
                    result.PenaltyAmount = booking.AmountPaid * lateCancelPenaltyPercent / 100;
                    result.RefundAmount = booking.AmountPaid - result.PenaltyAmount;
                    result.Reason = $"Late cancellation ({lateCancelPenaltyPercent}% penalty - cancelled within {freeCancelHours} hours of pickup)";
                }
                else
                {
                    // Free cancellation (within allowed window)
                    result.RefundAmount = booking.AmountPaid;
                    result.PenaltyAmount = 0;
                    result.PenaltyPercent = 0;
                    result.Reason = $"Free cancellation (more than {freeCancelHours} hours before pickup)";
                }
                break;
        }

        // Ensure refund doesn't exceed amount paid
        if (result.RefundAmount > booking.AmountPaid)
        {
            result.RefundAmount = booking.AmountPaid;
        }

        // Ensure non-negative values
        if (result.RefundAmount < 0)
        {
            result.RefundAmount = 0;
        }

        return result;
    }

    /// <summary>
    /// Gets a human-readable description of the current cancellation policy.
    /// </summary>
    public async Task<CancellationPolicyInfo> GetPolicyInfoAsync()
    {
        var policy = await m_settings.GetStringAsync(SettingKeys.Booking_CancellationPolicy, defaultValue: "Free") ?? "Free";
        var freeCancelHours = await m_settings.GetIntAsync(SettingKeys.Booking_FreeCancelHours, defaultValue: 24);
        var lateCancelPenaltyPercent = await m_settings.GetIntAsync(SettingKeys.Booking_LateCancelPenaltyPercent, defaultValue: 50);
        var noShowPenaltyPercent = await m_settings.GetIntAsync(SettingKeys.Booking_NoShowPenaltyPercent, defaultValue: 100);

        return new CancellationPolicyInfo
        {
            PolicyType = policy,
            FreeCancelHours = freeCancelHours,
            LateCancelPenaltyPercent = lateCancelPenaltyPercent,
            NoShowPenaltyPercent = noShowPenaltyPercent,
            Description = GetPolicyDescription(policy, freeCancelHours, lateCancelPenaltyPercent, noShowPenaltyPercent)
        };
    }

    private static string GetPolicyDescription(string policy, int freeCancelHours, int lateCancelPenaltyPercent, int noShowPenaltyPercent)
    {
        return policy.ToLowerInvariant() switch
        {
            "free" => "Free cancellation at any time. Full refund on all cancellations.",
            "nonrefundable" => "Non-refundable booking. No refunds will be issued for cancellations.",
            "timebased" => $"Free cancellation up to {freeCancelHours} hours before pickup. " +
                           $"Late cancellation fee: {lateCancelPenaltyPercent}% of amount paid. " +
                           $"No-show fee: {noShowPenaltyPercent}% of amount paid.",
            _ => $"Free cancellation up to {freeCancelHours} hours before pickup."
        };
    }

    /// <summary>
    /// Checks if a booking can be cancelled with full refund.
    /// </summary>
    public async Task<bool> CanCancelWithFullRefundAsync(Booking booking)
    {
        var calculation = await CalculateRefundAsync(booking);
        return calculation.RefundAmount >= booking.AmountPaid;
    }

    /// <summary>
    /// Checks if a booking can still be cancelled (not yet checked in).
    /// </summary>
    public static bool CanCancelBooking(Booking booking)
    {
        return booking.Status is BookingStatus.Pending or BookingStatus.Confirmed;
    }
}

/// <summary>
/// Result of refund calculation.
/// </summary>
public class RefundCalculation
{
    public int BookingId { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public int PenaltyPercent { get; set; }
    public double HoursUntilPickup { get; set; }
    public string Policy { get; set; } = "";
    public string Reason { get; set; } = "";

    /// <summary>
    /// Whether any refund will be issued.
    /// </summary>
    public bool HasRefund => RefundAmount > 0;

    /// <summary>
    /// Whether any penalty applies.
    /// </summary>
    public bool HasPenalty => PenaltyAmount > 0;
}

/// <summary>
/// Information about the current cancellation policy.
/// </summary>
public class CancellationPolicyInfo
{
    public string PolicyType { get; set; } = "";
    public int FreeCancelHours { get; set; }
    public int LateCancelPenaltyPercent { get; set; }
    public int NoShowPenaltyPercent { get; set; }
    public string Description { get; set; } = "";
}
