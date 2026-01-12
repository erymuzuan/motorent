using Microsoft.Extensions.Logging;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;

namespace MotoRent.Scheduler.Runners;

/// <summary>
/// Scheduled task that checks for expiring rentals and publishes ExpiryCheck messages.
/// </summary>
public class RentalExpiryRunner : ITaskRunner
{
    private readonly RentalDataContext m_context;
    private readonly IMessageBroker m_broker;
    private readonly ILogger<RentalExpiryRunner> m_logger;

    public string Name => nameof(RentalExpiryRunner);
    public string Description => "Checks for expiring rentals and sends reminder notifications";

    public RentalExpiryRunner(
        RentalDataContext context,
        IMessageBroker broker,
        ILogger<RentalExpiryRunner> logger)
    {
        m_context = context;
        m_broker = broker;
        m_logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        m_logger.LogInformation("Starting rental expiry check...");

        var now = DateTimeOffset.Now;
        var tomorrow = now.AddDays(1);

        // Find active rentals expiring within 24 hours
        var query = m_context.CreateQuery<Rental>()
            .Where(r => r.Status == "Active")
            .Where(r => r.ExpectedEndDate <= tomorrow);

        var result = await m_context.LoadAsync(query, 1, 1000, includeTotalRows: true);

        m_logger.LogInformation("Found {Count} rentals expiring within 24 hours", result.TotalRows);

        foreach (var rental in result.ItemCollection)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // Publish expiry check message
                var message = new BrokeredMessage(rental)
                {
                    Entity = nameof(Rental),
                    EntityId = rental.RentalId,
                    Crud = CrudOperation.Changed,
                    Operation = "ExpiryCheck",
                    Id = Guid.NewGuid().ToString("N")
                };

                await m_broker.SendAsync(message);

                m_logger.LogDebug(
                    "Published ExpiryCheck for Rental {RentalId} - Expires: {ExpectedEndDate}",
                    rental.RentalId,
                    rental.ExpectedEndDate);
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to publish ExpiryCheck for Rental {RentalId}", rental.RentalId);
            }
        }

        m_logger.LogInformation("Rental expiry check completed. Processed {Count} rentals.", result.ItemCollection.Count);
    }
}
