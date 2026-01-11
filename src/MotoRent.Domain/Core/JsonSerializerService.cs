using System.Text.Json;
using System.Text.Json.Serialization;
using MotoRent.Domain.Entities;
using MotoRent.Domain.JsonSupports;

namespace MotoRent.Domain.Core;

public static class JsonSerializerService
{
    private static readonly JsonSerializerOptions s_defaultOptions = CreateOptions();
    private static readonly JsonSerializerOptions s_camelCaseOptions = CreateOptions(camelCase: true);
    private static readonly JsonSerializerOptions s_prettyOptions = CreateOptions(pretty: true);

    private static JsonSerializerOptions CreateOptions(bool camelCase = false, bool pretty = false)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = pretty,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // Enable populating read-only collection properties during deserialization
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate
        };

        if (camelCase)
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

        // Add custom converters
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new DateOnlyConverter());
        options.Converters.Add(new NullableDateOnlyConverter());
        options.Converters.Add(new TimeOnlyConverter());
        options.Converters.Add(new NullableTimeOnlyConverter());
        options.Converters.Add(new DecimalConverterWithStringSupport());
        options.Converters.Add(new NullableDecimalConverterWithStringSupport());
        options.Converters.Add(new Int32ConverterWithStringSupport());
        options.Converters.Add(new NullableInt32ConverterWithStringSupport());

        return options;
    }

    public static T? DeserializeFromJson<T>(this string json, JsonSerializerOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        options ??= s_defaultOptions;
        return JsonSerializer.Deserialize<T>(json, options);
    }

    public static string ToJsonString<T>(this T value, bool pretty = false, bool camelCase = false)
    {
        if (pretty)
            return JsonSerializer.Serialize(value, s_prettyOptions);
        if (camelCase)
            return JsonSerializer.Serialize(value, s_camelCaseOptions);
        return JsonSerializer.Serialize(value, s_defaultOptions);
    }

    /// <summary>
    /// Serialize entity with type discriminator for polymorphic deserialization
    /// </summary>
    public static string ToJson(this Entity entity)
    {
        return JsonSerializer.Serialize<Entity>(entity, s_defaultOptions);
    }

    /// <summary>
    /// Deserialize entity with polymorphic type resolution
    /// </summary>
    public static Entity? DeserializeEntity(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return JsonSerializer.Deserialize<Entity>(json, s_defaultOptions);
    }

    /// <summary>
    /// Deep clone an object via JSON serialization
    /// </summary>
    public static T Clone<T>(this T source) where T : class
    {
        var json = source.ToJsonString();
        return json.DeserializeFromJson<T>()!;
    }

    public static JsonSerializerOptions GetDefaultOptions() => s_defaultOptions;
    public static JsonSerializerOptions GetCamelCaseOptions() => s_camelCaseOptions;
}
