using Microsoft.Extensions.Logging;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;
using MotoRent.Worker.Infrastructure;

namespace MotoRent.Worker.Subscribers;

/// <summary>
/// Handles rental expiry check events.
/// Sends reminder notifications for rentals expiring soon.
/// </summary>
public class RentalExpirySubscriber : Subscriber<Rental>
{
    public override string QueueName => nameof(RentalExpirySubscriber);

    public override string[] RoutingKeys =>
    [
        $"{nameof(Rental)}.{CrudOperation.Changed}.ExpiryCheck"
    ];

    protected override async Task ProcessMessage(Rental rental, BrokeredMessage message)
    {
        // Only process active rentals
        if (rental.Status != "Active")
        {
            Logger?.LogDebug("Skipping expiry check for Rental {RentalId} - Status: {Status}", rental.RentalId, rental.Status);
            return;
        }

        var daysRemaining = (rental.ExpectedEndDate - DateTimeOffset.Now).TotalDays;

        Logger?.LogInformation(
            "Expiry check for Rental {RentalId} - Renter: {RenterName}, Days remaining: {DaysRemaining:F1}",
            rental.RentalId,
            rental.RenterName,
            daysRemaining);

        if (daysRemaining <= 1)
        {
            // TODO: Send expiry warning notification
            // This would typically:
            // 1. Load renter contact info
            // 2. Send email/SMS notification
            // 3. Include return instructions and drop-off location

            Logger?.LogInformation(
                "Sent expiry warning for Rental {RentalId} - Expected return: {ExpectedEndDate}",
                rental.RentalId,
                rental.ExpectedEndDate);
        }
        else if (daysRemaining <= 0)
        {
            // Rental is overdue
            Logger?.LogWarning(
                "Rental {RentalId} is OVERDUE by {OverdueDays:F1} days - Renter: {RenterName}, Vehicle: {VehicleName}",
                rental.RentalId,
                Math.Abs(daysRemaining),
                rental.RenterName,
                rental.VehicleName);

            // TODO: Send overdue notification to shop manager
            // TODO: Flag rental for follow-up
        }

        await Task.CompletedTask;
    }
}
