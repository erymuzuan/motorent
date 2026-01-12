namespace MotoRent.Domain.Entities;

/// <summary>
/// How the vehicle owner is compensated for their vehicle.
/// </summary>
public enum OwnerPaymentModel
{
    /// <summary>
    /// Fixed daily rate regardless of rental price (e.g., 200 THB/day).
    /// Payment = RentalDays × OwnerDailyRate
    /// </summary>
    DailyRate,

    /// <summary>
    /// Percentage of gross rental amount only (e.g., 30% of rental rate).
    /// Payment = (RentalRate × Days) × OwnerRevenueSharePercent
    /// Does NOT include insurance, accessories, driver fees, etc.
    /// </summary>
    RevenueShare
}
