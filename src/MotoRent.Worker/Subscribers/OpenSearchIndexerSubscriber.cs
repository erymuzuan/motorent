using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Messaging;
using MotoRent.Domain.Search;
using MotoRent.Worker.Infrastructure;
using Polly;

namespace MotoRent.Worker.Subscribers;

/// <summary>
/// Indexes entities to OpenSearch when they are created, updated, or deleted.
/// </summary>
public class OpenSearchIndexerSubscriber : Subscriber
{
    private HttpClient m_defaultClient = new();
    private HttpClient m_defaultDeleteClient = new();

    public override string QueueName => nameof(OpenSearchIndexerSubscriber);

    public override string[] RoutingKeys => typeof(Entity)
        .Assembly
        .GetTypes()
        .Where(x => !x.IsAbstract && x.IsClass)
        .Where(x => x.GetInterface(nameof(ISearchable)) is not null)
        .Select(x => x.Name)
        .Distinct()
        .Select(x => $"{x}.#.#")
        .ToArray();

    private readonly Dictionary<string, HttpClient> m_clients = new();
    private readonly Dictionary<string, HttpClient> m_deleteClients = new();

    private HttpClient GetClient(string? accountNo) =>
        m_clients.GetValueOrDefault(accountNo ?? string.Empty, m_defaultClient);

    private HttpClient GetDeleteClient(string? accountNo) =>
        m_deleteClients.GetValueOrDefault(accountNo ?? string.Empty, m_defaultDeleteClient);

    public override void OnStart()
    {
        base.OnStart();

        var searchServiceEnabled = Environment.GetEnvironmentVariable("MOTO_SearchService");
        if (searchServiceEnabled != "OpenSearch")
        {
            WriteMessage("OpenSearch indexing disabled (set MOTO_SearchService=OpenSearch to enable)");
            return;
        }

        var clientFactory = ObjectBuilder.GetObject<IHttpClientFactory>();
        if (clientFactory is null)
        {
            WriteMessage("IHttpClientFactory not available");
            return;
        }

        string[] servers = ["", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];
        foreach (var svr in servers)
        {
            var accounts = Environment.GetEnvironmentVariable($"MOTO_OpenSearch{svr}Clients")
                ?.Split([','], StringSplitOptions.RemoveEmptyEntries) ?? [];
            if (accounts.Length == 0 && svr != "") continue;

            var host = $"OpenSearchHost{svr}";
            var address = Environment.GetEnvironmentVariable($"MOTO_{host}");
            if (string.IsNullOrWhiteSpace(address)) continue;

            var c = clientFactory.CreateClient(host);
            var d = clientFactory.CreateClient($"OpenSearchDeleteClient{svr}");

            foreach (var acc in accounts)
            {
                m_clients.TryAdd(acc, c);
                m_deleteClients.TryAdd(acc, d);
            }

            if (svr == "")
            {
                m_defaultClient = c;
                m_defaultDeleteClient = d;
            }
        }

        WriteMessage("OpenSearch indexer initialized with {Count} client configurations", m_clients.Count);
    }

    protected override async Task<MessageReceiveStatus> ProcessMessage(BrokeredMessage message)
    {
        if (Environment.GetEnvironmentVariable("MOTO_SearchService") != "OpenSearch")
            return MessageReceiveStatus.Accepted;
        if (message.GetHeaderBooleanValue("no-index") ?? false)
            return MessageReceiveStatus.Accepted;
        if (message is not { Entity.Length: > 0 })
            return MessageReceiveStatus.Accepted;
        if (message is not { AccountNo.Length: > 0 })
            return MessageReceiveStatus.Accepted;
        if (message is not { EntityId: > 0 })
            return MessageReceiveStatus.Accepted;
        if (message.Item is not ISearchable sv)
            return MessageReceiveStatus.Accepted;
        if (message.Item is not { } et)
            return MessageReceiveStatus.Accepted;

        var accountNo = message.AccountNo;
        var type = message.Entity;
        var id = message.EntityId;

        var index = $"{accountNo}_{type}".ToLowerInvariant();

        // Check if entity uses shared index
        var isSharedProp = sv.GetType().GetProperty("IsShared", BindingFlags.Public | BindingFlags.Static);
        if (isSharedProp?.GetValue(null) is true)
            index = type.ToLowerInvariant();

        var uri = $"/{index}/_doc/{id}";
        WriteMessage("Indexing {AccountNo}-{Type} : {Id}", accountNo, type, id);

        // Handle deletion
        if (message.Crud == CrudOperation.Deleted)
        {
            await Policy.Handle<Exception>(_ => true)
                .WaitAndRetryAsync(3, c => TimeSpan.FromMilliseconds(100 * Math.Pow(2, c)))
                .ExecuteAndCaptureAsync(async () =>
                {
                    var client = GetDeleteClient(accountNo);
                    var gr = await client.GetAsync(uri);
                    if (gr.StatusCode == HttpStatusCode.NotFound) return;

                    var dr = await client.DeleteAsync(uri);
                    WriteMessage("[DELETE] {Uri} -> {Status}", uri, dr.StatusCode);
                    if (!dr.IsSuccessStatusCode)
                    {
                        WriteMessage("[WARN] Failed to delete {Uri} with status {Status}", uri, dr.StatusCode);
                    }
                });
            return MessageReceiveStatus.Accepted;
        }

        // Build index document
        var temp = new
        {
            sv.Title,
            sv.Date,
            sv.Summary,
            sv.Id,
            sv.Text,
            EntityType = et.GetType().Name,
            sv.Type,
            sv.ImageStoreId,
            sv.Status,
            sv.PermissionPolicy,
            Item = sv.CustomFields
        };

        var esj = System.Text.Json.JsonSerializer.Serialize(temp, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        var appJson = new MediaTypeWithQualityHeaderValue("application/json");
        var client2 = GetClient(accountNo);
        var response = await client2.PutAsync(uri, new StringContent(esj, appJson));
        WriteMessage("{Uri} -> {Status}", uri, response.StatusCode);

        // Create index if it doesn't exist
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var createIndexUri = $"/{index}";
            var createIndex = await client2.PutAsync(createIndexUri, new StringContent("{}", appJson));
            WriteMessage("[WARN] Create index [{Type}] {Uri} -> {Status}", type, createIndexUri, createIndex.StatusCode);

            if (createIndex.IsSuccessStatusCode)
                await ProcessMessage(message);
        }

        // Handle blocked index
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            var deleteUri = $"/{index}/_settings";
            const string blockedReadOnly = """
                                           { "index": { "blocks": { "read_only_allow_delete": "false" } } }
                                           """;
            await Policy.Handle<TaskCanceledException>(x => x.Message.Contains("canceled"))
                .WaitAndRetryAsync(2, c => TimeSpan.FromMilliseconds(200 * Math.Pow(2, c)))
                .ExecuteAndCaptureAsync(async () =>
                {
                    await client2.PutAsync(deleteUri, new StringContent(blockedReadOnly, appJson));
                });
        }

        return MessageReceiveStatus.Accepted;
    }
}
