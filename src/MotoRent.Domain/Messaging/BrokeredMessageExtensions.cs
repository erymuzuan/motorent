using System.Text;

namespace MotoRent.Domain.Messaging;

/// <summary>
/// Extension methods for BrokeredMessage.
/// </summary>
public static class BrokeredMessageExtensions
{
    private static string ByteToString(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var sr = new StreamReader(stream);
        return sr.ReadToEnd();
    }

    /// <summary>
    /// Get a text value from message headers.
    /// </summary>
    public static string? GetHeaderTextValue(this BrokeredMessage message, string header)
    {
        var headers = message.Headers;
        if (!headers.TryGetValue(header, out var obj)) return null;
        if (obj is byte[] bits)
            return ByteToString(bits);
        if (obj is string str)
            return str;
        return null;
    }

    /// <summary>
    /// Try to get a text value from message headers.
    /// </summary>
    public static bool TryGetHeaderTextValue(this BrokeredMessage message, string header, out string? value)
    {
        var headers = message.Headers;
        if (!headers.TryGetValue(header, out var v))
        {
            value = null;
            return false;
        }

        if (v is byte[] operationBytes)
        {
            value = ByteToString(operationBytes);
            return true;
        }

        if (v is string str)
        {
            value = str;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Get an integer value from message headers.
    /// </summary>
    public static int? GetHeaderInt32Value(this BrokeredMessage message, string header)
    {
        var headers = message.Headers;
        if (!headers.TryGetValue(header, out var obj)) return null;
        if (obj is int value)
            return value;

        var text = message.GetHeaderTextValue(header);
        if (int.TryParse(text, out var val))
            return val;
        return null;
    }

    /// <summary>
    /// Get a boolean value from message headers.
    /// </summary>
    public static bool? GetHeaderBooleanValue(this BrokeredMessage message, string header)
    {
        var headers = message.Headers;
        if (!headers.TryGetValue(header, out var obj)) return null;
        if (obj is bool value)
            return value;

        var text = message.GetHeaderTextValue(header);
        if (bool.TryParse(text, out var val))
            return val;
        return null;
    }

    /// <summary>
    /// Add or replace a header value.
    /// </summary>
    public static void AddOrReplace(this Dictionary<string, object> headers, string key, object value)
    {
        headers[key] = value;
    }
}
