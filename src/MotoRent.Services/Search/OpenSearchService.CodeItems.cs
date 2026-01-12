using MotoRent.Domain.Extensions;
using MotoRent.Domain.Search;
using Polly;

namespace MotoRent.Services.Search;

public partial class OpenSearchService
{
    public async Task<CodeItem[]> SearchCodeItemsAsync(string type, string term)
    {
        if (NoConnection) return [];

        var value = term.ToEmpty()
            .Replace("\n", " ")
            .Replace("\r\n", " ")
            .Replace("\"", "\\\"");

        var query = $$"""
                      {
                        "query": {
                          "match": {
                            "Summary": {
                              "query": "{{value}}",
                              "fuzziness": "AUTO",
                              "operator": "AND"
                            }
                          }
                        }
                      }
                      """;

        var index = $"code_{type}".ToLowerInvariant();
        var url = $"{index}/_search";

        var pr = await Policy.Handle<Exception>(_ => true)
            .WaitAndRetryAsync(3, c => TimeSpan.FromMilliseconds(200 * Math.Pow(2, c)))
            .ExecuteAndCaptureAsync(async () =>
            {
                var response = await m_defaultClient.PostAsJsonAsync(url, query);
                if (!response.IsSuccessStatusCode)
                    return [];

                var json = await response.ReadContentAsJsonElementAsync();
                var total = json.ReadJsonValue<int>("hits.total.value");
                if (total == 0) return [];

                var results = from hit in json.GetProperty("hits").GetProperty("hits").EnumerateArray()
                    let source = hit.GetProperty("_source")
                    let code = source.ReadJsonValue<string>("Code")
                    let summary = source.ReadJsonValue<string>("Summary")
                    let bags = source.TryGetProperty("Item", out var t)
                        ? t.ToString().DeserializeFromJson()
                        : new Dictionary<string, object>()
                    select new CodeItem(type, code ?? string.Empty, summary ?? string.Empty)
                    {
                        Bags = bags
                    };

                return results.ToArray();
            });

        if (pr.FinalException is HttpRequestException)
        {
            LastNoConnectionTimestamp = DateTimeOffset.Now;
        }

        return pr.Result ?? [];
    }
}
