using System.Text.Json;
using System.Text.Json.Serialization;

namespace MotoRent.Domain.JsonSupports;

public class TimeOnlyConverter : JsonConverter<TimeOnly>
{
    private const string Format = "HH:mm:ss";

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            return default;

        return TimeOnly.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }
}

public class NullableTimeOnlyConverter : JsonConverter<TimeOnly?>
{
    private const string Format = "HH:mm:ss";

    public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
            return null;

        return TimeOnly.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString(Format));
        else
            writer.WriteNullValue();
    }
}
