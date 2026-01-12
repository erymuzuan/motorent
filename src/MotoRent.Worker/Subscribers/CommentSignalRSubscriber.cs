using Microsoft.AspNetCore.SignalR.Client;
using MotoRent.Domain.Helps;
using MotoRent.Domain.Messaging;
using MotoRent.Worker.Infrastructure;

namespace MotoRent.Worker.Subscribers;

/// <summary>
/// Broadcasts comment changes via SignalR to connected clients.
/// </summary>
public class CommentSignalRSubscriber : Subscriber<Comment>
{
    private HubConnection? m_connection;

    public override string QueueName => nameof(CommentSignalRSubscriber);

    public override string[] RoutingKeys => [$"{nameof(Comment)}.#.#"];

    private bool IsConnected { get; set; }

    public override void OnStart()
    {
        var server = Environment.GetEnvironmentVariable("MOTORENT_BaseUrl") ?? "https://localhost:5001";
        var token = Environment.GetEnvironmentVariable("MOTORENT_ApiToken");

        m_connection = new HubConnectionBuilder()
            .WithUrl($"{server}/hub-comments",
                options => { options.AccessTokenProvider = () => Task.FromResult(token); })
            .WithAutomaticReconnect([
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromMinutes(15)
            ])
            .Build();

        m_connection.Reconnected += Reconnected;
        m_connection.Reconnecting += Reconnecting;
        m_connection.Closed += ConnectionClosed;
    }

    private Task ConnectionClosed(Exception? error = null)
    {
        if (error is not null)
        {
            WriteError(error, "Disconnected with error: {Message}", error.Message);
        }

        IsConnected = false;
        return Task.CompletedTask;
    }

    private async Task Reconnected(string? arg)
    {
        WriteMessage("Reconnected {ConnectionId}", arg ?? "");
        await Connect();
    }

    private async Task Connect()
    {
        try
        {
            WriteMessage("Connecting to SignalR hub...");
            if (m_connection is { State: HubConnectionState.Disconnected })
            {
                await m_connection.StartAsync();
                IsConnected = true;
            }

            WriteMessage("Connected to SignalR hub");
        }
        catch (Exception ex)
        {
            WriteError(ex, "Failed to connect to SignalR hub");
            await ConnectionClosed();
        }
    }

    private Task Reconnecting(Exception? exc)
    {
        WriteMessage("Attempting to reconnect. Error: {Error}", exc?.Message ?? "unknown");
        return Task.CompletedTask;
    }

    protected override async Task ProcessMessage(Comment item, BrokeredMessage message)
    {
        if (this is { IsConnected: false, m_connection: not null })
            await Connect();

        if (this is { IsConnected: true, m_connection.State: HubConnectionState.Connected })
        {
            await m_connection.InvokeAsync<bool>("CommentAdded", item, message.AccountNo);
            WriteMessage("Broadcasted comment {CommentId} to SignalR", item.CommentId);
        }
    }
}
