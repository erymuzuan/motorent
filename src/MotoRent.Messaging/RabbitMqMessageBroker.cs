using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace MotoRent.Messaging;

/// <summary>
/// RabbitMQ implementation of IMessageBroker.
/// </summary>
public class RabbitMqMessageBroker : IMessageBroker
{
    private readonly ILogger<RabbitMqMessageBroker> m_logger;
    private readonly Func<Task<string?>> m_getAccountNo;
    private readonly Func<Task<string?>> m_getUserName;

    private string Password { get; } = RabbitMqConfigurationManager.Password;
    private string UserName { get; } = RabbitMqConfigurationManager.UserName;
    private string HostName { get; } = RabbitMqConfigurationManager.Host;
    private int Port { get; } = RabbitMqConfigurationManager.Port;
    protected virtual string VirtualHost { get; } = RabbitMqConfigurationManager.VirtualHost;
    private int ProcessId { get; } = Environment.ProcessId;

    private IConnection? m_connection;
    private IChannel? m_channel;
    private readonly SemaphoreSlim m_lock = new(1, 1);
    private HttpClient? m_client;

    public RabbitMqMessageBroker(
        ILogger<RabbitMqMessageBroker> logger,
        Func<Task<string?>>? getAccountNo = null,
        Func<Task<string?>>? getUserName = null)
    {
        m_logger = logger;
        m_getAccountNo = getAccountNo ?? (() => Task.FromResult<string?>(null));
        m_getUserName = getUserName ?? (() => Task.FromResult<string?>(null));
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (m_channel is { IsClosed: false, IsOpen: true })
        {
            await m_channel.CloseAsync();
            m_channel.Dispose();
        }

        if (m_connection != null)
        {
            try
            {
                await m_connection.CloseAsync();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                m_connection.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        m_client?.Dispose();
    }

    public async Task ConnectAsync(Action<string, object> disconnected)
    {
        var factory = new ConnectionFactory
        {
            UserName = UserName,
            VirtualHost = VirtualHost,
            Password = Password,
            HostName = HostName,
            Port = Port
        };
        m_connection = await factory.CreateConnectionAsync();
        m_connection.ConnectionShutdownAsync += (_, e) =>
        {
            disconnected(e.ReplyText, e);
            return Task.CompletedTask;
        };
        m_channel = await m_connection.CreateChannelAsync();
    }

    public void OnMessageDelivered(
        Func<BrokeredMessage, Task<MessageReceiveStatus>> processItem,
        SubscriberOption subscription,
        double timeOut = double.MaxValue)
    {
        OnMessageDeliveredAsync(processItem, subscription, timeOut).GetAwaiter().GetResult();
    }

    public async Task OnMessageDeliveredAsync(
        Func<BrokeredMessage, Task<MessageReceiveStatus>> processItem,
        SubscriberOption subscription,
        double timeOut = double.MaxValue)
    {
        const bool NO_ACK = false;
        var basicConsumer = new TaskBasicConsumer(m_channel!);

        basicConsumer.Received += async (_, e) =>
        {
            var header = new MessageHeaders(e);
            var message = new BrokeredMessage
            {
                Crud = header.Crud,
                Id = header.MessageId,
                Username = header.Username,
                Operation = header.Operation,
                TryCount = header.TryCount,
                ReplyTo = header.ReplyTo,
                RetryDelay = TimeSpan.FromMilliseconds(5000)
            };

            var rawHeaders = header.GetRawHeaders();
            foreach (var key in rawHeaders.Keys)
            {
                message.Headers[key] = rawHeaders[key];
            }

            message.EntityId = message.GetHeaderInt32Value("entity-id") ?? 0;
            message.Username = message.GetHeaderTextValue("username");
            message.AccountNo = message.GetHeaderTextValue("account-no");

            // Parse message body
            if (e.Properties?.ContentType == "application/json")
            {
                var json = rawHeaders.TryGetValue("compressed", out var compressed) && compressed is false
                    ? Encoding.UTF8.GetString(e.Body)
                    : await e.Body.DecompressAsync();

                var entity = message.GetHeaderTextValue("entity");
                if (!string.IsNullOrEmpty(entity))
                {
                    var type = Type.GetType($"MotoRent.Domain.Entities.{entity}, MotoRent.Domain");
                    if (type != null)
                    {
                        try
                        {
                            var item = JsonSerializer.Deserialize(json, type, JsonSerializerOptions) as Entity;
                            message.Item = item;
                            message.Entity = entity;
                        }
                        catch (JsonException ex)
                        {
                            m_logger.LogError(ex, "Failed to deserialize message body for {Entity}", entity);
                            await m_channel!.BasicAckAsync(e.DeliveryTag, false);
                            return;
                        }
                    }
                    else
                    {
                        message.Body = json;
                    }
                }
                else
                {
                    message.Body = json;
                }
            }
            else
            {
                var text = rawHeaders.TryGetValue("compressed", out var compressed) && compressed is false
                    ? Encoding.UTF8.GetString(e.Body)
                    : await e.Body.DecompressAsync();
                if (string.IsNullOrWhiteSpace(text)) text = Encoding.UTF8.GetString(e.Body);
                message.Body = text;
            }

            var sw = Stopwatch.StartNew();
            var status = await processItem(message);
            sw.Stop();
            m_logger.LogInformation("{QueueName} processed message in {Elapsed}ms", subscription.QueueName, sw.ElapsedMilliseconds);

            switch (status)
            {
                case MessageReceiveStatus.Accepted:
                    await m_channel!.BasicAckAsync(e.DeliveryTag, false);
                    break;
                case MessageReceiveStatus.Rejected:
                    await m_channel!.BasicRejectAsync(e.DeliveryTag, false);
                    break;
                case MessageReceiveStatus.Dropped:
                    await m_channel!.BasicAckAsync(e.DeliveryTag, false);
                    break;
                case MessageReceiveStatus.Delayed:
                    if (message.RetryDelay == TimeSpan.Zero)
                        throw new InvalidOperationException("You must set RetryDelay for delayed messages");

                    await m_channel!.BasicAckAsync(e.DeliveryTag, false);
                    await PublishToDelayQueueAsync(message, subscription.QueueName);
                    break;
                case MessageReceiveStatus.Requeued:
                    await m_channel!.BasicNackAsync(e.DeliveryTag, false, true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        };

        var prefetchCount = subscription.PrefetchCount <= 1 ? 1 : subscription.PrefetchCount;
        await m_channel!.BasicQosAsync(0, (ushort)prefetchCount, false);
        var consumerTag = $"{ProcessId}_{subscription.Name}";
        var tag = await m_channel.BasicConsumeAsync(subscription.QueueName, NO_ACK, consumerTag, basicConsumer);
        m_logger.LogInformation("Subscribed to {QueueName} with tag {Tag}", subscription.QueueName, tag);
    }

    public async Task CreateSubscriptionAsync(QueueDeclareOption option)
    {
        m_logger.LogInformation("Creating subscription for {QueueName}", option.QueueName);
        await m_lock.WaitAsync();
        try
        {
            var exchangeName = RabbitMqConfigurationManager.DefaultExchange;
            var deadLetterExchange = option.DeadLetterTopic ?? RabbitMqConfigurationManager.DefaultDeadLetterExchange;
            var deadLetterQueue = option.DeadLetterQueue ?? RabbitMqConfigurationManager.DefaultDeadLetterQueue;

            // Declare exchanges
            await m_channel!.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic, true);
            await m_channel.ExchangeDeclareAsync(deadLetterExchange, ExchangeType.Topic, true);

            // Declare main queue with dead letter exchange
            var args = new Dictionary<string, object?> { { "x-dead-letter-exchange", deadLetterExchange } };
            await m_channel.QueueDeclareAsync(option.QueueName, true, false, false, args);

            // Declare and bind dead letter queue
            await m_channel.QueueDeclareAsync(deadLetterQueue, true, false, false, args);
            await m_channel.QueueBindAsync(deadLetterQueue, deadLetterExchange, "#", null);

            // Bind main queue to exchange
            await m_channel.QueueBindAsync(option.QueueName, exchangeName, option.QueueName, null);
            foreach (var routingKey in option.RoutingKeys)
            {
                await m_channel.QueueBindAsync(option.QueueName, exchangeName, routingKey, null);
            }

            // Delay exchange and queue for retry mechanism
            var delayExchange = option.DelayedExchange ?? ($"motorent.delay.exchange.{option.QueueName}");
            var delayQueue = $"motorent.delay.queue.{option.QueueName}";
            var delayQueueArgs = new Dictionary<string, object?>
            {
                { "x-dead-letter-exchange", exchangeName },
                { "x-dead-letter-routing-key", option.QueueName }
            };

            await m_channel.ExchangeDeclareAsync(delayExchange, "direct");
            await m_channel.QueueDeclareAsync(delayQueue, true, false, false, delayQueueArgs);
            await m_channel.QueueBindAsync(delayQueue, delayExchange, string.Empty, null);

            await m_channel.BasicQosAsync(0, (ushort)option.PrefetchCount, false);
        }
        finally
        {
            m_lock.Release();
        }
    }

    public async Task SendAsync(BrokeredMessage message)
    {
        var result = await Policy
            .Handle<AlreadyClosedException>(x => x.Message.Contains("close-reason"))
            .Or<BrokerUnreachableException>()
            .Or<ObjectDisposedException>()
            .WaitAndRetryAsync(5, c => TimeSpan.FromMilliseconds(400 * Math.Pow(2, c)))
            .ExecuteAndCaptureAsync(async () =>
            {
                if (m_channel is not { IsOpen: true })
                {
                    await ConnectAsync((_, _) => { });
                    await SendAsync(message);
                    return;
                }

                var account = message.AccountNo ?? await m_getAccountNo();
                var user = message.Username ?? await m_getUserName();

                var bodyContent = message is { Body.Length: > 0, Item: null }
                    ? await message.Body.CompressAsync()
                    : await JsonSerializer.Serialize(message.Item, JsonSerializerOptions).CompressAsync();

                var props = new BasicProperties
                {
                    DeliveryMode = DeliveryModes.Persistent,
                    Persistent = true,
                    ContentType = "application/json",
                    Headers = new MessageHeaders(message).ToDictionary()
                };

                if (!string.IsNullOrWhiteSpace(message.Entity))
                    props.Headers!["entity"] = message.Entity;

                props.Headers!["crud"] = message.Crud.ToString();
                props.Headers["action"] = message.Operation;
                props.Headers["operation"] = message.Operation;
                props.Headers["entity-id"] = message.EntityId;
                props.Headers["routing-key"] = message.RoutingKey;
                props.Headers["username"] = user;
                props.Headers["account-no"] = account;

                foreach (var header in message.Headers)
                {
                    props.Headers.TryAdd(header.Key, header.Value);
                }

                await m_channel.BasicPublishAsync(
                    exchange: RabbitMqConfigurationManager.DefaultExchange,
                    routingKey: message.RoutingKey,
                    mandatory: false,
                    basicProperties: props,
                    body: bodyContent);
            });

        if (result.FinalException != null)
            throw result.FinalException;
    }

    public async Task SendAsync(BrokeredMessage message, DateTimeOffset deliveryTime, string queue)
    {
        if (m_channel is not { IsOpen: true })
        {
            await ConnectAsync((_, _) => { });
            await SendAsync(message, deliveryTime, queue);
            return;
        }

        var account = message.AccountNo ?? await m_getAccountNo();
        var user = message.Username ?? await m_getUserName();

        var bodyContent = message is { Body.Length: > 0, Item: null }
            ? await message.Body.CompressAsync()
            : await JsonSerializer.Serialize(message.Item, JsonSerializerOptions).CompressAsync();

        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            Persistent = true,
            ContentType = "application/json",
            Headers = new MessageHeaders(message).ToDictionary()
        };

        if (!string.IsNullOrWhiteSpace(message.Entity))
            props.Headers!["entity"] = message.Entity;

        props.Headers!["crud"] = message.Crud.ToString();
        props.Headers["operation"] = message.Operation;
        props.Headers["entity-id"] = message.EntityId;
        props.Headers["routing-key"] = message.RoutingKey;
        props.Headers["username"] = user;
        props.Headers["account-no"] = account;

        foreach (var header in message.Headers)
        {
            props.Headers.TryAdd(header.Key, header.Value);
        }

        props.Expiration = deliveryTime < DateTimeOffset.Now
            ? "500"
            : (deliveryTime - DateTimeOffset.Now).TotalMilliseconds.ToString("F0");

        var delayExchange = $"motorent.delay.exchange.{queue}";
        await m_channel.BasicPublishAsync(
            exchange: delayExchange,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: props,
            body: bodyContent);
    }

    private async Task PublishToDelayQueueAsync(BrokeredMessage message, string queue)
    {
        var delayExchange = $"motorent.delay.exchange.{queue}";

        message.TryCount = (message.TryCount ?? 0) + 1;

        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            Persistent = true,
            ContentType = "application/json"
        };

        var headers = new MessageHeaders(message);
        props.Headers = headers.ToDictionary();
        props.Headers[MessageHeaders.SPH_TRYCOUNT] = message.TryCount;

        var delay = message.RetryDelay.TotalMilliseconds;
        props.Expiration = delay.ToString(CultureInfo.InvariantCulture);

        m_logger.LogInformation("Delaying message for {Delay} (attempt {TryCount})", message.RetryDelay, message.TryCount);

        var body = await JsonSerializer.Serialize(message.Item, JsonSerializerOptions).CompressAsync();
        await m_channel!.BasicPublishAsync(
            exchange: delayExchange,
            routingKey: string.Empty,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    public async Task<BrokeredMessage?> GetMessageAsync(string queue)
    {
        var result = await m_channel!.BasicGetAsync(queue, false);
        if (result == null)
            return null;

        var header = new MessageHeaders(new ReceivedMessageArgs
        {
            Body = result.Body.ToArray(),
            Properties = result.BasicProperties,
            DeliveryTag = result.DeliveryTag,
            Exchange = result.Exchange,
            RoutingKey = result.RoutingKey,
            Redelivered = result.Redelivered
        });

        var json = await result.Body.ToArray().DecompressAsync();
        var item = JsonSerializer.Deserialize<Entity>(json, JsonSerializerOptions);

        async Task MessageAcknowledgedAsync(BrokeredMessage msg, MessageReceiveStatus status)
        {
            switch (status)
            {
                case MessageReceiveStatus.Accepted:
                    await m_channel.BasicAckAsync(result.DeliveryTag, false);
                    break;
                case MessageReceiveStatus.Rejected:
                    await m_channel.BasicRejectAsync(result.DeliveryTag, false);
                    break;
                case MessageReceiveStatus.Dropped:
                    await m_channel.BasicAckAsync(result.DeliveryTag, false);
                    break;
                case MessageReceiveStatus.Delayed:
                    await PublishToDelayQueueAsync(msg, queue);
                    break;
                case MessageReceiveStatus.Requeued:
                    await m_channel.BasicNackAsync(result.DeliveryTag, false, true);
                    break;
            }
        }

        var message = new BrokeredMessage((msg, status) => MessageAcknowledgedAsync(msg, status).GetAwaiter().GetResult())
        {
            Item = item,
            Crud = header.Crud,
            Id = header.MessageId,
            Username = header.Username,
            Operation = header.Operation,
            TryCount = header.TryCount,
            ReplyTo = header.ReplyTo,
            RetryDelay = TimeSpan.FromMilliseconds(5000)
        };

        var rawHeaders = header.GetRawHeaders();
        foreach (var key in rawHeaders.Keys)
        {
            message.Headers[key] = rawHeaders[key];
        }

        return message;
    }

    public Task<BrokeredMessage?> ReadFromDeadLetterAsync()
    {
        return GetMessageAsync(RabbitMqConfigurationManager.DefaultDeadLetterQueue);
    }

    public async Task SendToDeadLetterQueue(BrokeredMessage message)
    {
        var bodyContent = message is { Body.Length: > 0, Item: null }
            ? await message.Body.CompressAsync()
            : await JsonSerializer.Serialize(message.Item, JsonSerializerOptions).CompressAsync();

        var props = new BasicProperties
        {
            DeliveryMode = DeliveryModes.Persistent,
            Persistent = true,
            ContentType = "application/json",
            Headers = new MessageHeaders(message).ToDictionary()
        };

        await m_channel!.BasicPublishAsync(
            exchange: RabbitMqConfigurationManager.DefaultDeadLetterExchange,
            routingKey: message.RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: bodyContent);
    }

    public async Task<QueueStatistics> GetStatisticsAsync(string queue)
    {
        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(
                RabbitMqConfigurationManager.UserName,
                RabbitMqConfigurationManager.Password)
        };

        m_client ??= new HttpClient(handler)
        {
            BaseAddress = new Uri(
                $"{RabbitMqConfigurationManager.ManagementScheme}://{RabbitMqConfigurationManager.Host}:{RabbitMqConfigurationManager.ManagementPort}")
        };

        var response = await m_client.GetAsync($"api/queues/{RabbitMqConfigurationManager.VirtualHost}/{queue}");
        if (!response.IsSuccessStatusCode)
            return new QueueStatistics();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var count = json.TryGetProperty("messages", out var messages) ? messages.GetInt32() : 0;
        var unacked = json.TryGetProperty("messages_unacknowledged", out var unackedProp) ? unackedProp.GetInt32() : 0;

        double published = 0;
        double delivered = 0;

        if (json.TryGetProperty("message_stats", out var stats))
        {
            if (stats.TryGetProperty("publish_details", out var pubDetails) &&
                pubDetails.TryGetProperty("rate", out var pubRate))
                published = pubRate.GetDouble();

            if (stats.TryGetProperty("deliver_details", out var delDetails) &&
                delDetails.TryGetProperty("rate", out var delRate))
                delivered = delRate.GetDouble();
        }

        return new QueueStatistics
        {
            PublishedRate = published,
            DeliveryRate = delivered,
            Count = count,
            Processing = unacked
        };
    }

    public async Task RemoveSubscriptionAsync(string queue)
    {
        m_logger.LogInformation("Removing subscription for {Queue}", queue);

        var url = $"http://{RabbitMqConfigurationManager.Host}:{RabbitMqConfigurationManager.ManagementPort}";
        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(
                RabbitMqConfigurationManager.UserName,
                RabbitMqConfigurationManager.Password)
        };

        using var client = new HttpClient(handler) { BaseAddress = new Uri(url) };

        var response = await client.DeleteAsync($"/api/queues/{RabbitMqConfigurationManager.VirtualHost}/{queue}");
        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            m_logger.LogError("Failed to delete queue {Queue}: {StatusCode}", queue, response.StatusCode);
        }

        var delayQueue = $"motorent.delay.queue.{queue}";
        var ttlResponse = await client.DeleteAsync($"/api/queues/{RabbitMqConfigurationManager.VirtualHost}/{delayQueue}");
        if (ttlResponse.StatusCode != HttpStatusCode.NoContent)
        {
            m_logger.LogError("Failed to delete delay queue {Queue}: {StatusCode}", delayQueue, ttlResponse.StatusCode);
        }
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
