using System.Globalization;
using System.Text.Json;

namespace MotoRent.Domain.Extensions;

/// <summary>
/// Extension methods for JsonElement operations.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Reads a nested JSON array from a property path.
    /// </summary>
    public static JsonElement.ArrayEnumerator ReadJsonArray(this JsonElement je, string property)
    {
        if (je.TryGetProperty(property, out var sp) && sp.ValueKind == JsonValueKind.Array)
        {
            return sp.EnumerateArray();
        }

        if (property.Contains('.') && !property.StartsWith("\""))
        {
            var path = property.Split(["."], StringSplitOptions.RemoveEmptyEntries);
            if (path is [var p, ..]
                && je.TryGetProperty(p, out var jp)
                && jp.ValueKind == JsonValueKind.Object
                && property.Length > p.Length + 1)
            {
                return jp.ReadJsonArray(property[(p.Length + 1)..]);
            }
        }

        throw new InvalidOperationException($"{property} is not a JSON array");
    }

    /// <summary>
    /// Reads a typed value from a property path (supports nested paths like "hits.total.value").
    /// </summary>
    public static TResult? ReadJsonValue<TResult>(this JsonElement je, string property)
    {
        // Handle nested paths
        if (property.Contains('.') && !property.StartsWith('\"'))
        {
            var path = property.Split(["."], StringSplitOptions.RemoveEmptyEntries);
            if (path is [var p, ..]
                && je.TryGetProperty(p, out var jp)
                && jp.ValueKind == JsonValueKind.Object
                && property.Length > p.Length + 1)
            {
                return jp.ReadJsonValue<TResult>(property[(p.Length + 1)..]);
            }
        }

        if (!je.TryGetProperty(property, out var jp2))
            return default;

        var type = typeof(TResult);

        try
        {
            return type switch
            {
                _ when type == typeof(string) && jp2.ValueKind == JsonValueKind.String => (TResult)(object)jp2.GetString()!,
                _ when type == typeof(string) && jp2.ValueKind == JsonValueKind.Number => (TResult)(object)jp2.GetRawText(),
                _ when type == typeof(string) && jp2.ValueKind == JsonValueKind.Null => default,
                _ when type == typeof(int) && jp2.TryGetInt32(out var intVal) => (TResult)(object)intVal,
                _ when type == typeof(int) && jp2.ValueKind == JsonValueKind.String
                    && int.TryParse(jp2.GetString(), out var intVal2) => (TResult)(object)intVal2,
                _ when type == typeof(long) && jp2.TryGetInt64(out var longVal) => (TResult)(object)longVal,
                _ when type == typeof(double) && jp2.TryGetDouble(out var dblVal) => (TResult)(object)dblVal,
                _ when type == typeof(decimal) && jp2.TryGetDecimal(out var decVal) => (TResult)(object)decVal,
                _ when type == typeof(bool) && jp2.ValueKind is JsonValueKind.True or JsonValueKind.False
                    => (TResult)(object)jp2.GetBoolean(),
                _ when type == typeof(DateOnly) && jp2.ValueKind == JsonValueKind.String
                    && DateOnly.TryParse(jp2.GetString(), out var dateOnly) => (TResult)(object)dateOnly,
                _ when type == typeof(DateTime) && jp2.TryGetDateTime(out var dt) => (TResult)(object)dt,
                _ when type == typeof(DateTimeOffset) && jp2.TryGetDateTimeOffset(out var dto) => (TResult)(object)dto,
                _ => default
            };
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Reads a DateOnly from a property path.
    /// </summary>
    public static DateOnly? ReadDateOnly(this JsonElement element, string path)
    {
        if (!element.TryGetProperty(path, out var prop))
            return null;

        var text = prop.GetString()?.Replace("+0800", "+08:00") ?? string.Empty;
        var formats = new[] { "yyyy-MM-ddTHH:mm:sszzz", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd" };

        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var dt) && dt.Year > 2000)
        {
            return DateOnly.FromDateTime(dt);
        }

        return null;
    }

    /// <summary>
    /// Deserializes JSON to a dictionary.
    /// </summary>
    public static Dictionary<string, object>? DeserializeFromJson(this string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }
}
