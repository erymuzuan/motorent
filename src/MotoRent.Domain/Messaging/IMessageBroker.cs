namespace MotoRent.Domain.Messaging;

/// <summary>
/// Interface for message broker operations (RabbitMQ implementation).
/// </summary>
public interface IMessageBroker : IDisposable
{
    /// <summary>
    /// Connect to the message broker.
    /// </summary>
    /// <param name="disconnected">Callback for disconnection events.</param>
    Task ConnectAsync(Action<string, object> disconnected);

    /// <summary>
    /// Subscribe to messages with a callback handler.
    /// </summary>
    /// <param name="processItem">Async callback to process each message.</param>
    /// <param name="subscription">Subscriber configuration.</param>
    /// <param name="timeOut">Optional timeout for message processing.</param>
    void OnMessageDelivered(Func<BrokeredMessage, Task<MessageReceiveStatus>> processItem, SubscriberOption subscription, double timeOut = double.MaxValue);

    /// <summary>
    /// Create a queue subscription with bindings.
    /// </summary>
    /// <param name="option">Queue declaration options.</param>
    Task CreateSubscriptionAsync(QueueDeclareOption option);

    /// <summary>
    /// Send a message to the broker.
    /// </summary>
    /// <param name="message">Message to send.</param>
    Task SendAsync(BrokeredMessage message);

    /// <summary>
    /// Send a message with delayed delivery.
    /// </summary>
    /// <param name="message">Message to send.</param>
    /// <param name="deliveryTime">When the message should be delivered.</param>
    /// <param name="queue">Target queue name.</param>
    Task SendAsync(BrokeredMessage message, DateTimeOffset deliveryTime, string queue);

    /// <summary>
    /// Get a single message from a queue.
    /// </summary>
    /// <param name="queue">Queue name.</param>
    /// <returns>The message, or null if queue is empty.</returns>
    Task<BrokeredMessage?> GetMessageAsync(string queue);

    /// <summary>
    /// Read a message from the dead letter queue.
    /// </summary>
    Task<BrokeredMessage?> ReadFromDeadLetterAsync();

    /// <summary>
    /// Send a message to the dead letter queue.
    /// </summary>
    /// <param name="message">Message to dead letter.</param>
    Task SendToDeadLetterQueue(BrokeredMessage message);

    /// <summary>
    /// Get statistics for a queue.
    /// </summary>
    /// <param name="queue">Queue name.</param>
    Task<QueueStatistics> GetStatisticsAsync(string queue);

    /// <summary>
    /// Remove a subscription (delete queue).
    /// </summary>
    /// <param name="queue">Queue name to remove.</param>
    Task RemoveSubscriptionAsync(string queue);
}
