namespace MotoRent.Domain.Messaging;

/// <summary>
/// Represents the status of message processing by a subscriber.
/// </summary>
public enum MessageReceiveStatus
{
    /// <summary>
    /// Message was processed successfully and should be acknowledged.
    /// </summary>
    Accepted,

    /// <summary>
    /// Message processing failed and should be sent to dead letter queue.
    /// </summary>
    Rejected,

    /// <summary>
    /// Message should be dropped silently (acknowledged but not processed).
    /// </summary>
    Dropped,

    /// <summary>
    /// Message should be delayed and retried after a specified time.
    /// </summary>
    Delayed,

    /// <summary>
    /// Message should be requeued immediately for retry.
    /// </summary>
    Requeued
}
