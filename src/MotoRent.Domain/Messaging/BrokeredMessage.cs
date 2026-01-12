using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Messaging;

/// <summary>
/// Represents a message that can be sent through the message broker.
/// </summary>
public class BrokeredMessage
{
    private readonly Action<BrokeredMessage, MessageReceiveStatus> m_messageAcknowledge;

    public BrokeredMessage(Action<BrokeredMessage, MessageReceiveStatus> messageAcknowledge)
    {
        m_messageAcknowledge = messageAcknowledge;
    }

    public BrokeredMessage(Action<BrokeredMessage, MessageReceiveStatus> messageAcknowledge, Entity item)
    {
        m_messageAcknowledge = messageAcknowledge;
        Item = item;
    }

    public BrokeredMessage()
    {
        m_messageAcknowledge = (_, _) => { };
    }

    public BrokeredMessage(Entity item, IDictionary<string, object>? headers = null)
    {
        m_messageAcknowledge = (_, _) => { };
        Item = item;
        if (headers is null) return;
        foreach (var (key, value) in headers)
        {
            Headers.TryAdd(key, value);
        }
    }

    /// <summary>
    /// Raw message body content.
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Computed routing key in format: {Entity}.{Crud}.{Operation}
    /// </summary>
    public string RoutingKey => $"{Entity}.{Crud}.{Operation}";

    /// <summary>
    /// Unique message identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Custom headers attached to the message.
    /// </summary>
    public Dictionary<string, object> Headers { get; } = new();

    /// <summary>
    /// The deserialized entity associated with this message.
    /// </summary>
    public Entity? Item { get; set; }

    /// <summary>
    /// Custom operation name (e.g., "CheckIn", "CheckOut").
    /// </summary>
    public string? Operation { get; set; }

    /// <summary>
    /// The CRUD operation type.
    /// </summary>
    public CrudOperation Crud { get; set; }

    /// <summary>
    /// Number of times this message has been retried.
    /// </summary>
    public int? TryCount { get; set; }

    /// <summary>
    /// Username of the user who triggered this message.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Reply-to address for RPC-style messaging.
    /// </summary>
    public string? ReplyTo { get; set; }

    /// <summary>
    /// Entity type name.
    /// </summary>
    public string? Entity { get; set; }

    /// <summary>
    /// ID of the entity.
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Flag indicating if this is a data import operation.
    /// </summary>
    public bool IsDataImport => HasDataImport(this);

    /// <summary>
    /// Delay before retry.
    /// </summary>
    public TimeSpan RetryDelay { get; set; }

    /// <summary>
    /// Account number (tenant identifier).
    /// </summary>
    public string? AccountNo { get; set; }

    private static bool HasDataImport(BrokeredMessage message)
    {
        var headers = message.Headers;
        if (headers.TryGetValue("data-import", out var header) && header is bool b)
        {
            return b;
        }
        return false;
    }

    /// <summary>
    /// Get a typed value from the headers.
    /// </summary>
    public T? GetValue<T>(string key)
    {
        if (!Headers.TryGetValue(key, out var blob))
            return default;

        if (blob.GetType() == typeof(T))
            return (T)blob;

        if (blob is not byte[] operationBytes) return default;

        var sct = System.Text.Encoding.UTF8.GetString(operationBytes);

        if (typeof(T) == typeof(bool) && bool.TryParse(sct, out var boolValue))
            return (T)(object)boolValue;

        if (typeof(T) == typeof(int) && int.TryParse(sct, out var intValue))
            return (T)(object)intValue;

        if (typeof(T) == typeof(double) && double.TryParse(sct, out var doubleValue))
            return (T)(object)doubleValue;

        if (typeof(T) == typeof(decimal) && decimal.TryParse(sct, out var decimalValue))
            return (T)(object)decimalValue;

        if (typeof(T) == typeof(float) && float.TryParse(sct, out var floatValue))
            return (T)(object)floatValue;

        if (typeof(T) == typeof(DateTime) && DateTime.TryParse(sct, out var dateValue))
            return (T)(object)dateValue;

        return default;
    }

    /// <summary>
    /// Get a nullable typed value from the headers.
    /// </summary>
    public T? GetNullableValue<T>(string key) where T : struct
    {
        if (!Headers.TryGetValue(key, out var blob))
            return null;

        if (blob?.GetType() == typeof(T))
            return (T)blob;

        if (blob is not byte[] bytes) return null;

        var sct = System.Text.Encoding.UTF8.GetString(bytes);

        if (typeof(T) == typeof(bool) && bool.TryParse(sct, out var boolValue))
            return (T)(object)boolValue;

        if (typeof(T) == typeof(int) && int.TryParse(sct, out var intValue))
            return (T)(object)intValue;

        if (typeof(T) == typeof(double) && double.TryParse(sct, out var doubleValue))
            return (T)(object)doubleValue;

        if (typeof(T) == typeof(decimal) && decimal.TryParse(sct, out var decimalValue))
            return (T)(object)decimalValue;

        if (typeof(T) == typeof(float) && float.TryParse(sct, out var floatValue))
            return (T)(object)floatValue;

        if (typeof(T) == typeof(DateTime) && DateTime.TryParse(sct, out var dateValue))
            return (T)(object)dateValue;

        return null;
    }

    /// <summary>
    /// Delay message delivery and increase the retry count.
    /// </summary>
    /// <param name="ttl">Timespan for delayed delivery</param>
    public void Delay(TimeSpan ttl)
    {
        RetryDelay = ttl;
        TryCount = (TryCount ?? 0) + 1;
        m_messageAcknowledge(this, MessageReceiveStatus.Delayed);
    }

    /// <summary>
    /// Requeue the message immediately for retry.
    /// </summary>
    public void Requeue()
    {
        m_messageAcknowledge(this, MessageReceiveStatus.Requeued);
    }

    /// <summary>
    /// Accept (acknowledge) the message.
    /// </summary>
    public void Accept()
    {
        m_messageAcknowledge(this, MessageReceiveStatus.Accepted);
    }

    /// <summary>
    /// Drop the message silently.
    /// </summary>
    public void Drop()
    {
        m_messageAcknowledge(this, MessageReceiveStatus.Dropped);
    }

    /// <summary>
    /// Reject the message and send to dead letter queue.
    /// </summary>
    public void Reject()
    {
        m_messageAcknowledge(this, MessageReceiveStatus.Rejected);
    }
}
