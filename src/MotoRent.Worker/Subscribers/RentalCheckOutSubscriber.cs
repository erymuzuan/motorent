using Microsoft.Extensions.Logging;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;
using MotoRent.Worker.Infrastructure;

namespace MotoRent.Worker.Subscribers;

/// <summary>
/// Handles rental check-out events.
/// Updates vehicle status back to Available and sends thank-you notification.
/// </summary>
public class RentalCheckOutSubscriber : Subscriber<Rental>
{
    public override string QueueName => nameof(RentalCheckOutSubscriber);

    public override string[] RoutingKeys =>
    [
        $"{nameof(Rental)}.{CrudOperation.Changed}.CheckOut"
    ];

    protected override async Task ProcessMessage(Rental rental, BrokeredMessage message)
    {
        Logger?.LogInformation(
            "Processing check-out for Rental {RentalId} - Vehicle {VehicleId}, Renter {RenterName}",
            rental.RentalId,
            rental.VehicleId,
            rental.RenterName);

        // TODO: Update vehicle status to Available
        // This would typically:
        // 1. Load the vehicle from database
        // 2. Set Status = "Available"
        // 3. Save changes
        // 4. Publish Vehicle.Changed.StatusUpdate message

        // TODO: Send thank-you notification
        // This would typically:
        // 1. Load renter contact info
        // 2. Send email/SMS notification
        // 3. Include rental summary and receipt

        Logger?.LogInformation(
            "Check-out processed for Rental {RentalId} - Duration: {DurationDisplay}, Total: {TotalAmount:C}",
            rental.RentalId,
            rental.DurationDisplay,
            rental.TotalAmount);

        await Task.CompletedTask;
    }
}
