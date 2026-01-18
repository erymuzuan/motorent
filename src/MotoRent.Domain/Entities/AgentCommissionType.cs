namespace MotoRent.Domain.Entities;

/// <summary>
/// Constants for commission calculation types.
/// </summary>
public static class AgentCommissionType
{
    /// <summary>
    /// Commission as percentage of booking total (e.g., 10% of ฿3,000 = ฿300).
    /// </summary>
    public const string Percentage = "Percentage";

    /// <summary>
    /// Fixed amount per booking regardless of total (e.g., ฿200 per booking).
    /// </summary>
    public const string FixedPerBooking = "FixedPerBooking";

    /// <summary>
    /// Fixed amount per vehicle in booking (e.g., ฿100 per bike).
    /// </summary>
    public const string FixedPerVehicle = "FixedPerVehicle";

    /// <summary>
    /// Fixed amount per day across all vehicles (e.g., ฿50 per day).
    /// </summary>
    public const string FixedPerDay = "FixedPerDay";

    public static readonly string[] All =
    [
        Percentage,
        FixedPerBooking,
        FixedPerVehicle,
        FixedPerDay
    ];
}
