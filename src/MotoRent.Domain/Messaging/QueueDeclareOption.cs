namespace MotoRent.Domain.Messaging;

/// <summary>
/// Options for declaring a queue and its bindings.
/// </summary>
public class QueueDeclareOption
{
    public string QueueName { get; }

    public QueueDeclareOption(string queueName, params string[] routingKeys)
    {
        QueueName = queueName;
        RoutingKeys = routingKeys;
    }

    /// <summary>
    /// Routing key patterns for binding (e.g., "Rental.#.#", "Payment.Added.*").
    /// </summary>
    public string[] RoutingKeys { get; set; }

    /// <summary>
    /// Name of the dead letter queue for rejected messages.
    /// </summary>
    public string? DeadLetterQueue { get; set; }

    /// <summary>
    /// Name of the dead letter exchange.
    /// </summary>
    public string? DeadLetterTopic { get; set; }

    /// <summary>
    /// Name of the delayed message exchange.
    /// </summary>
    public string? DelayedExchange { get; set; }

    /// <summary>
    /// Name of the delayed message queue.
    /// </summary>
    public string? DelayedQueue { get; set; }

    /// <summary>
    /// Time-to-live for messages in the queue.
    /// </summary>
    public TimeSpan Ttl { get; set; }

    /// <summary>
    /// Number of messages to prefetch per consumer.
    /// </summary>
    public int PrefetchCount { get; set; }
}
