using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;
using MotoRent.Domain.Search;
using Polly;

namespace MotoRent.Services.Search;

/// <summary>
/// OpenSearch-based implementation of ISearchService.
/// </summary>
public partial class OpenSearchService : ISearchService
{
    private readonly ILogger<OpenSearchService> m_logger;
    private readonly IRequestContext m_requestContext;
    private readonly RentalDataContext m_dataContext;
    private readonly HttpClient m_defaultClient;
    private readonly Dictionary<string, HttpClient> m_clients = new();

    private HttpClient Client => m_clients.GetValueOrDefault(
        m_requestContext.GetAccountNo() ?? string.Empty, m_defaultClient);

    private HttpClient SharedIndexClient { get; }

    private DateTimeOffset? LastNoConnectionTimestamp { get; set; }

    private bool NoConnection => LastNoConnectionTimestamp switch
    {
        null => false,
        { Year: > 2022 } when (DateTimeOffset.Now - LastNoConnectionTimestamp.Value).TotalSeconds < 60d => true,
        _ => false
    };

    public OpenSearchService(
        IHttpClientFactory clientFactory,
        ILogger<OpenSearchService> logger,
        IRequestContext requestContext,
        RentalDataContext dataContext)
    {
        m_logger = logger;
        m_requestContext = requestContext;
        m_dataContext = dataContext;

        SharedIndexClient = clientFactory.CreateClient("OpenSearchHost");
        m_defaultClient = clientFactory.CreateClient("OpenSearchHost");

        // Support multiple OpenSearch hosts for different accounts
        string[] servers = ["", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"];
        foreach (var svr in servers)
        {
            var accounts = Environment.GetEnvironmentVariable($"MOTORENT_OpenSearch{svr}Clients")
                ?.Split([','], StringSplitOptions.RemoveEmptyEntries) ?? [];
            if (accounts.Length == 0) continue;

            var host = $"OpenSearchHost{svr}";
            var c = clientFactory.CreateClient(host);
            foreach (var acc in accounts)
            {
                m_clients.TryAdd(acc, c);
            }
        }
    }

    public async ValueTask<SearchResult[]> SearchAsync<T>(
        string term,
        SearchOptions? options = null,
        int skip = 0,
        int take = 20) where T : Entity, ISearchable, new()
    {
        if (NoConnection) return [];

        var op = options?.Operator switch
        {
            ChainAndOr.Or => "OR",
            _ => "AND"
        };

        var value = term.ToEmpty()
            .Replace("\n", " ")
            .Replace("\r\n", " ")
            .Replace("\"", "\\\"");

        var sort = GetSortDsl(options);
        var query = $$"""
                      {
                          "from": {{skip}},
                          "size": {{take}},
                          "query": {
                              "query_string" : {
                                  "default_operator" : "{{op}}",
                                  "query" : "{{value}}"
                              }
                          }{{sort}}
                      }
                      """;

        var accountNo = await m_requestContext.GetAccountNoAsync();
        var loop = 0;
        while (string.IsNullOrWhiteSpace(accountNo) && loop < 20)
        {
            await Task.Delay(200);
            accountNo = await m_requestContext.GetAccountNoAsync();
            loop++;
        }

        var entity = typeof(T).Name;
        var index = $"{accountNo}_{entity}".ToLowerInvariant();
        var client = Client;

        if (T.IsShared)
        {
            index = entity.ToLowerInvariant();
            client = SharedIndexClient;

            if (options is { Field.Length: > 0 })
            {
                var opStr = options.Operator == ChainAndOr.And ? "AND" : "OR";
                var fuzziness = string.IsNullOrWhiteSpace(options.Fuzziness) ? "AUTO" : options.Fuzziness;
                query = $$"""
                          {
                            "query": {
                              "match": {
                                "Item.{{options.Field}}": {
                                  "query": "{{value}}",
                                  "fuzziness": "{{fuzziness}}",
                                  "operator": "{{opStr}}"
                                }
                              }
                            }
                          }
                          """;
            }
        }

        var url = $"{index}/_search";

        var pr = await Policy.Handle<Exception>(_ => true)
            .WaitAndRetryAsync(3, c => TimeSpan.FromMilliseconds(200 * Math.Pow(2, c)))
            .ExecuteAndCaptureAsync(async () =>
            {
                var response = await client.PostAsJsonAsync(url, query);
                if (!response.IsSuccessStatusCode)
                    return [];

                var json = await response.ReadContentAsJsonElementAsync();
                var total = json.ReadJsonValue<int>("hits.total.value");
                if (total == 0) return [];

                var results = from hit in json.GetProperty("hits").GetProperty("hits").EnumerateArray()
                    let source = hit.GetProperty("_source")
                    let id = hit.ReadJsonValue<int>("_id")
                    let score = hit.ReadJsonValue<decimal>("_score")
                    let permission = source.ReadJsonValue<int>("PermissionPolicy")
                    let title = source.ReadJsonValue<string>("Title")
                    let status = source.ReadJsonValue<string>("Status")
                    let et = source.ReadJsonValue<string>("EntityType")
                    let type = source.ReadJsonValue<string>("Type")
                    let summary = source.ReadJsonValue<string>("Summary")
                    let text = source.ReadJsonValue<string>("Text")
                    let date = source.ReadDateOnly("Date")
                    let image = source.ReadJsonValue<string>("ImageStoreId")
                    let bags = source.TryGetProperty("Item", out var t)
                        ? t.ToString().DeserializeFromJson()
                        : new Dictionary<string, object>()
                    select new SearchResult(id, type ?? string.Empty, title ?? string.Empty,
                        summary ?? string.Empty, text ?? string.Empty)
                    {
                        Item = bags ?? new Dictionary<string, object>(),
                        Status = status,
                        Date = date,
                        EntityType = et,
                        ImageStoreId = image,
                        Score = Convert.ToDouble(score),
                        PermissionPolicy = permission,
                        TotalHits = total
                    };

                return results.ToArray();
            });

        if (pr.FinalException is HttpRequestException)
        {
            LastNoConnectionTimestamp = DateTimeOffset.Now;
        }

        return pr.Result ?? [];
    }

    public async Task<SearchResult[]> SearchAsync(string term, string[] types, int skip = 0, int take = 20)
    {
        if (NoConnection) return [];

        var accountNo = await m_requestContext.GetAccountNoAsync();
        var sort = """
                   ,
                     "sort": [
                       {
                         "Date": {
                           "order": "desc"
                         }
                       }
                     ]
                   """;

        var query = $$"""
                      {
                          "from": {{skip}},
                          "size": {{take}},
                          "query": {
                              "query_string" : {
                                  "fields": ["Title","Text"],
                                  "default_operator" : "AND",
                                  "query" : "{{term}}"
                              }
                          }{{sort}}
                      }
                      """;

        var index = types.ToString(",", x => $"{accountNo}_{x}".ToLowerInvariant());

        var pr = await Policy.Handle<Exception>(_ => true)
            .WaitAndRetryAsync(3, c => TimeSpan.FromMilliseconds(200 * Math.Pow(2, c)))
            .ExecuteAndCaptureAsync(async () =>
            {
                var response = await Client.PostAsJsonAsync($"{index}/_search", query);
                if (!response.IsSuccessStatusCode)
                    return [];

                var json = await response.ReadContentAsJsonElementAsync();
                var total = json.ReadJsonValue<int>("hits.total.value");
                if (total == 0) return [];

                var results = from hit in json.GetProperty("hits").GetProperty("hits").EnumerateArray()
                    let source = hit.GetProperty("_source")
                    let id = hit.ReadJsonValue<int>("_id")
                    let score = hit.ReadJsonValue<decimal>("_score")
                    let permission = source.ReadJsonValue<int>("PermissionPolicy")
                    let title = source.ReadJsonValue<string>("Title")
                    let entityType = source.ReadJsonValue<string>("EntityType")
                    let type = source.ReadJsonValue<string>("Type")
                    let summary = source.ReadJsonValue<string>("Summary")
                    let text = source.ReadJsonValue<string>("Text")
                    let date = source.ReadDateOnly("Date")
                    let image = source.ReadJsonValue<string>("ImageStoreId")
                    let bags = source.TryGetProperty("Item", out var t)
                        ? t.ToString().DeserializeFromJson()
                        : new Dictionary<string, object>()
                    select new SearchResult(id, type ?? string.Empty, title ?? string.Empty,
                        summary ?? string.Empty, text ?? string.Empty)
                    {
                        Item = bags ?? new Dictionary<string, object>(),
                        Date = date,
                        EntityType = entityType,
                        ImageStoreId = image
                    };

                return results.ToArray();
            });

        if (pr.FinalException is HttpRequestException)
        {
            LastNoConnectionTimestamp = DateTimeOffset.Now;
        }

        return pr.Result ?? [];
    }

    public async ValueTask<LoadOperation<T>> PagedSearchAsync<T>(string term, int skip = 0, int take = 20)
        where T : Entity, new()
    {
        if (NoConnection)
            return new LoadOperation<T>();

        var accountNo = await m_requestContext.GetAccountNoAsync();
        var entity = typeof(T).Name;
        var index = $"{accountNo}_{entity}".ToLowerInvariant();

        var url = $"{index}/_search?q={term}&size={take}&from={skip}";
        var response = await Client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new LoadOperation<T>();

        var lo = new LoadOperation<T>();
        var json = await response.ReadContentAsJsonElementAsync();
        var total = json.ReadJsonValue<int>("hits.total.value");
        lo.TotalRows = total;
        lo.PageSize = take;

        return lo;
    }

    public async Task DeleteAsync<T, TField>(Func<T, TField> selector, TField value)
    {
        var text = value switch
        {
            string => $"\"{value}\"",
            DateTime d => $"\"{d:s}\"",
            DateOnly d => $"\"{d:O}\"",
            DateTimeOffset d => $"\"{d:O}\"",
            true => "true",
            false => "false",
            _ => $"{value}"
        };

        var query = $$"""
                      {
                        "query": {
                          "match": {
                            "{{selector.Method.Name}}": {{text}}
                          }
                        }
                      }
                      """;

        var accountNo = await m_requestContext.GetAccountNoAsync();
        var entity = typeof(T).Name;
        var index = $"{accountNo}_{entity}".ToLowerInvariant();
        var uri = $"{index}/{entity}/_delete_by_query";

        var content = new StringContent(query, Encoding.UTF8, "application/json");

        var pr = await Policy.Handle<Exception>(_ => true)
            .WaitAndRetryAsync(5, c => TimeSpan.FromMilliseconds(400 * Math.Pow(2, c)))
            .ExecuteAndCaptureAsync(async () =>
            {
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await Client.PostAsync(uri, content);
                response.EnsureSuccessStatusCode();
            });

        if (pr.FinalException != null)
            throw pr.FinalException;
    }

    private static string GetSortDsl(SearchOptions? options)
    {
        if (options is { SortDesc.Length: > 0 })
            return $$"""
                     ,
                       "sort": [
                         {
                           "{{options.SortDesc}}": {
                             "order": "desc"
                           }
                         }
                       ]
                     """;

        if (options is { SortAsc.Length: > 0 })
            return $$"""
                     ,
                       "sort": [
                         {
                           "{{options.SortAsc}}": {
                             "order": "asc"
                           }
                         }
                       ]
                     """;

        return string.Empty;
    }
}
