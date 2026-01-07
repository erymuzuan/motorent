using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MotoRent.Domain.JsonSupports;

/// <summary>
/// High-performance decimal converter that handles both numeric and string JSON values.
/// Uses Utf8Parser for maximum performance when parsing string values.
/// </summary>
public class DecimalConverterWithStringSupport : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            if (Utf8Parser.TryParse(span, out decimal number, out int bytesConsumed) &&
                span.Length == bytesConsumed)
                return number;

            if (decimal.TryParse(reader.GetString(), out number))
                return number;
        }

        return reader.GetDecimal();
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

public class NullableDecimalConverterWithStringSupport : JsonConverter<decimal?>
{
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString();
            if (string.IsNullOrEmpty(str))
                return null;

            var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            if (Utf8Parser.TryParse(span, out decimal number, out int bytesConsumed) &&
                span.Length == bytesConsumed)
                return number;

            if (decimal.TryParse(str, out number))
                return number;

            return null;
        }

        return reader.GetDecimal();
    }

    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteNumberValue(value.Value);
        else
            writer.WriteNullValue();
    }
}
