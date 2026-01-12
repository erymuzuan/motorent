using Microsoft.Extensions.Logging;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;
using MotoRent.Domain.Search;
using Polly;

namespace MotoRent.Services.Search;

public partial class OpenSearchService
{
    public async ValueTask<T[]> SearchEntityAsync<T>(string term, int skip = 0, int take = 20)
        where T : Entity, ISearchable, new()
    {
        if (NoConnection) return [];

        var accountNo = await m_requestContext.GetAccountNoAsync();
        var entity = typeof(T).Name;
        var index = $"{accountNo}_{entity}".ToLowerInvariant();

        var pr = await Policy.Handle<Exception>(_ => true)
            .WaitAndRetryAsync(3, c => TimeSpan.FromMilliseconds(200 * Math.Pow(2, c)))
            .ExecuteAndCaptureAsync(async () =>
            {
                var query = $$"""
                              {
                                  "from": {{skip}},
                                  "size": {{take}},
                                  "query": {
                                      "query_string" : {
                                          "default_operator" : "AND",
                                          "query" : "{{term}}"
                                      }
                                  },
                                  "_source": ["Id"]
                              }
                              """;

                var response = await Client.PostAsJsonAsync($"{index}/{entity}/_search", query);
                if (!response.IsSuccessStatusCode)
                    return [];

                var json = await response.ReadContentAsJsonElementAsync();
                var results = from hit in json.ReadJsonArray("hits.hits")
                    select hit.ReadJsonValue<int>("_id");

                return results.ToArray();
            });

        if (pr.FinalException is HttpRequestException)
        {
            LastNoConnectionTimestamp = DateTimeOffset.Now;
        }

        if (pr.Result is null or [])
            return [];

        var ids = pr.Result;
        var dbQuery = m_dataContext.CreateQuery<T>()
            .Where(x => ids.IsInList(x.GetId()));
        var lo = await m_dataContext.LoadAsync(dbQuery);

        return lo.ItemCollection.ToArray();
    }

    public async ValueTask<TResult[]> GetDistinctFieldValues<T, TResult>(string field)
        where T : Entity, ISearchable, new()
    {
        if (NoConnection) return [];

        var accountNo = await m_requestContext.GetAccountNoAsync();
        var entity = typeof(T).Name;
        var index = $"{accountNo}_{entity}".ToLowerInvariant();

        if (T.IsShared)
            index = entity.ToLowerInvariant();

        var dsl = $$"""
                    {
                        "size": 0,
                        "aggs": {
                            "values" : {
                                "terms" : {
                                  "field": "Item.{{field}}.keyword",
                                  "size": 500
                                }
                            }
                        }
                    }
                    """;

        var uri = $"{index}/_search";

        var pr = await Policy.Handle<HttpRequestException>(x =>
                x.Message.Contains("connection attempt failed", StringComparison.InvariantCultureIgnoreCase))
            .WaitAndRetryAsync(5, c => TimeSpan.FromMilliseconds(400 * Math.Pow(2, c)))
            .ExecuteAndCaptureAsync(async () => await Client.PostAsJsonAsync(uri, dsl));

        if (pr.FinalException is HttpRequestException he && he.Message.Contains("No connection could be made"))
        {
            LastNoConnectionTimestamp = DateTimeOffset.Now;
            return [];
        }

        if (pr.FinalException != null)
        {
            m_logger.LogError(pr.FinalException, "Failed to get distinct field values: {Dsl}", dsl);
            return [];
        }

        var response = pr.Result;
        if (response is null || !response.IsSuccessStatusCode)
            return [];

        var json = await response.ReadContentAsJsonElementAsync();
        var results = from hit in json.ReadJsonArray("aggregations.values.buckets")
            let key = hit.ReadJsonValue<string>("key")
            select (TResult)(object)key!;

        return results.ToArray();
    }
}
