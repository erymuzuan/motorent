using RabbitMQ.Client;

namespace MotoRent.Messaging;

/// <summary>
/// Event arguments for received RabbitMQ messages.
/// </summary>
public class ReceivedMessageArgs : EventArgs
{
    public string? ConsumerTag { get; set; }
    public ulong DeliveryTag { get; set; }
    public bool Redelivered { get; set; }
    public string? Exchange { get; set; }
    public string? RoutingKey { get; set; }
    public IReadOnlyBasicProperties? Properties { get; set; }
    public byte[] Body { get; set; } = [];
}
