using RabbitMQ.Client;

namespace MotoRent.Messaging;

public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

/// <summary>
/// Custom RabbitMQ consumer that converts basic delivery to strongly-typed events.
/// </summary>
public class TaskBasicConsumer(IChannel channel) : AsyncDefaultBasicConsumer(channel)
{
    public event AsyncEventHandler<ReceivedMessageArgs>? Received;

    public override async Task HandleBasicDeliverAsync(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default)
    {
        if (Received is not null)
        {
            var args = new ReceivedMessageArgs
            {
                ConsumerTag = consumerTag,
                DeliveryTag = deliveryTag,
                Redelivered = redelivered,
                Exchange = exchange,
                RoutingKey = routingKey,
                Body = body.ToArray(),
                Properties = properties
            };
            await Received(this, args);
        }
        await base.HandleBasicDeliverAsync(consumerTag, deliveryTag, redelivered, exchange, routingKey, properties, body, cancellationToken);
    }
}
