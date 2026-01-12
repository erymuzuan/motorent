using Microsoft.Extensions.Logging;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;

namespace MotoRent.Worker.Infrastructure;

/// <summary>
/// Generic typed subscriber for processing specific entity types.
/// </summary>
/// <typeparam name="T">Entity type to process.</typeparam>
public abstract class Subscriber<T> : Subscriber where T : Entity
{
    /// <summary>
    /// Tenant data context for rental operations.
    /// </summary>
    protected RentalDataContext DataContext => ObjectBuilder.GetObject<RentalDataContext>();

    /// <summary>
    /// Core data context for cross-tenant operations.
    /// </summary>
    protected CoreDataContext CoreDataContext => ObjectBuilder.GetObject<CoreDataContext>();

    /// <summary>
    /// Process the typed entity message.
    /// </summary>
    protected abstract Task ProcessMessage(T item, BrokeredMessage message);

    protected override async Task<MessageReceiveStatus> ProcessMessage(BrokeredMessage message)
    {
        if (message.Item is not T item)
        {
            Logger?.LogWarning("Expected entity type {Expected} but got {Actual}", typeof(T).Name, message.Item?.GetType().Name);
            return MessageReceiveStatus.Dropped;
        }

        try
        {
            await ProcessMessage(item, message);
            return MessageReceiveStatus.Accepted;
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Error processing {Entity} message", typeof(T).Name);

            // Retry logic with exponential backoff
            var tryCount = message.TryCount ?? 0;
            if (tryCount < 3)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, tryCount) * 30);
                message.Delay(delay);
                return MessageReceiveStatus.Delayed;
            }

            return MessageReceiveStatus.Rejected;
        }
    }
}
