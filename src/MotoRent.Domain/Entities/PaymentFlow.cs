namespace MotoRent.Domain.Entities;

/// <summary>
/// Constants for payment flow in agent bookings.
/// Determines who collects payment from customer.
/// </summary>
public static class PaymentFlow
{
    /// <summary>
    /// Customer pays shop directly. Shop pays commission to agent later.
    /// </summary>
    public const string CustomerPaysShop = "CustomerPaysShop";

    /// <summary>
    /// Customer pays agent. Agent settles with shop (keeps markup).
    /// </summary>
    public const string CustomerPaysAgent = "CustomerPaysAgent";

    public static readonly string[] All = [CustomerPaysShop, CustomerPaysAgent];
}
