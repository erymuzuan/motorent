using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MotoRent.Domain.JsonSupports;

/// <summary>
/// High-performance int converter that handles both numeric and string JSON values.
/// Uses Utf8Parser for maximum performance when parsing string values.
/// </summary>
public class Int32ConverterWithStringSupport : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            if (Utf8Parser.TryParse(span, out int number, out int bytesConsumed) &&
                span.Length == bytesConsumed)
                return number;

            if (int.TryParse(reader.GetString(), out number))
                return number;
        }

        return reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class NullableInt32ConverterWithStringSupport : JsonConverter<int?>
{
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrEmpty(str))
                return null;

            var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            if (Utf8Parser.TryParse(span, out int number, out int bytesConsumed) &&
                span.Length == bytesConsumed)
                return number;

            if (int.TryParse(str, out number))
                return number;

            return null;
        }

        return reader.GetInt32();
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
