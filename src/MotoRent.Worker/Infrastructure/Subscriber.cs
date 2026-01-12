using Microsoft.Extensions.Logging;
using MotoRent.Domain.Messaging;

namespace MotoRent.Worker.Infrastructure;

/// <summary>
/// Abstract base class for message subscribers.
/// </summary>
public abstract class Subscriber
{
    public virtual TimeSpan ProcessingInterval => TimeSpan.FromMilliseconds(200);
    public virtual CancellationToken StopRequested => CancellationToken.None;

    /// <summary>
    /// Name of the queue to subscribe to.
    /// </summary>
    public abstract string QueueName { get; }

    /// <summary>
    /// Routing key patterns for binding (e.g., "Rental.#.#", "Payment.Added.*").
    /// </summary>
    public abstract string[] RoutingKeys { get; }

    /// <summary>
    /// Number of messages to prefetch.
    /// </summary>
    public virtual int PrefetchCount { get; set; } = 1;

    /// <summary>
    /// Logger instance.
    /// </summary>
    protected ILogger? Logger { get; set; }

    /// <summary>
    /// Process a message as raw JSON.
    /// </summary>
    protected virtual Task<MessageReceiveStatus> ProcessMessage(string json, string routingKey)
    {
        return Task.FromResult(MessageReceiveStatus.Accepted);
    }

    /// <summary>
    /// Process a message.
    /// </summary>
    protected virtual Task<MessageReceiveStatus> ProcessMessage(BrokeredMessage message)
    {
        return Task.FromResult(MessageReceiveStatus.Accepted);
    }

    /// <summary>
    /// Called when the subscriber starts.
    /// </summary>
    public virtual void OnStart()
    {
    }

    /// <summary>
    /// Run the subscriber.
    /// </summary>
    public virtual async Task RunAsync(IMessageBroker broker, IServiceProvider services, ILogger logger)
    {
        Logger = logger;
        OnStart();

        // Create subscription with queue bindings
        var option = new QueueDeclareOption(QueueName, RoutingKeys)
        {
            PrefetchCount = PrefetchCount
        };
        await broker.CreateSubscriptionAsync(option);

        // Subscribe to messages
        broker.OnMessageDelivered(
            async message =>
            {
                try
                {
                    var status = await ProcessMessage(message);
                    return status;
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, "Error processing message in {Queue}", QueueName);
                    return MessageReceiveStatus.Rejected;
                }
            },
            new SubscriberOption(QueueName)
            {
                PrefetchCount = PrefetchCount
            });

        logger.LogInformation("Subscriber {Name} started on queue {Queue}", GetType().Name, QueueName);
    }

    protected void WriteMessage(string message, params object[] args)
    {
        Logger?.LogInformation(message, args);
    }

    protected void WriteError(Exception ex, string message, params object[] args)
    {
        Logger?.LogError(ex, message, args);
    }
}
