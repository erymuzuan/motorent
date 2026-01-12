using System.Net.Http.Headers;
using System.Text.Json;
using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Extensions;

/// <summary>
/// Extension methods for HttpClient operations.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Reads the response content as a string.
    /// </summary>
    public static async Task<string> ReadContentAsStringAsync(
        this HttpResponseMessage response,
        bool ensureSuccessStatusCode = true,
        string exceptionMessage = "HTTP request failed")
    {
        if (ensureSuccessStatusCode)
            response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Reads the response content as a JsonElement.
    /// </summary>
    public static async Task<JsonElement> ReadContentAsJsonElementAsync(
        this HttpResponseMessage response,
        bool ensureSuccessStatusCode = true,
        string exceptionMessage = "HTTP request failed")
    {
        try
        {
            var text = await response.ReadContentAsStringAsync(ensureSuccessStatusCode, exceptionMessage);
            return JsonDocument.Parse(text).RootElement;
        }
        catch (JsonException e)
        {
            Console.WriteLine(e);
            return default;
        }
    }

    /// <summary>
    /// Posts JSON content to a URL.
    /// </summary>
    public static async Task<HttpResponseMessage> PostAsJsonAsync<T>(
        this HttpClient client,
        string uri,
        T item)
    {
        var content = item switch
        {
            string text => new StringContent(text),
            Entity entity => new StringContent(JsonSerializer.Serialize(entity)),
            _ => new StringContent(JsonSerializer.Serialize(item))
        };

        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var response = await client.PostAsync(uri, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.ReadContentAsStringAsync(false);
            Console.WriteLine($"Response status: {response.StatusCode}");
            Console.WriteLine(body);
        }

        return response;
    }
}
