using System.Dynamic;
using MotoRent.Domain.Messaging;

namespace MotoRent.Messaging;

/// <summary>
/// Helper class for reading and writing message headers.
/// </summary>
public class MessageHeaders : DynamicObject
{
    private readonly string? m_operation;
    private readonly ReceivedMessageArgs? m_args;
    private readonly string? m_username;
    private readonly string? m_id;
    private readonly CrudOperation m_crud;

    public const string SPH_TRYCOUNT = "sph.trycount";
    public const string SPH_DELAY = "sph.delay";

    public MessageHeaders(ReceivedMessageArgs args)
    {
        m_args = args;
    }

    public MessageHeaders(BrokeredMessage msg)
    {
        m_username = msg.Username;
        m_id = msg.Id;
        m_crud = msg.Crud;
        m_operation = msg.Operation;
    }

    public MessageHeaders(string username, string id, CrudOperation crud, string operation)
    {
        m_username = username;
        m_id = id;
        m_crud = crud;
        m_operation = operation;
    }

    public Dictionary<string, object?> ToDictionary()
    {
        return new Dictionary<string, object?>
        {
            { "username", m_username },
            { "message-id", m_id },
            { "operation", m_operation },
            { "crud", m_crud.ToString() },
            { "log", "" }
        };
    }

    private static string ByteToString(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var sr = new StreamReader(stream);
        return sr.ReadToEnd();
    }

    public string? Operation
    {
        get
        {
            var headers = m_args?.Properties?.Headers;
            if (headers == null || !headers.ContainsKey("operation")) return null;
            if (headers["operation"] is byte[] operationBytes)
                return ByteToString(operationBytes);
            return null;
        }
    }

    public string? AccountNo
    {
        get
        {
            var headers = m_args?.Properties?.Headers;
            if (headers == null || !headers.ContainsKey("account-no")) return null;
            if (headers["account-no"] is byte[] operationBytes)
                return ByteToString(operationBytes);
            return null;
        }
    }

    public string? MessageId
    {
        get
        {
            var headers = m_args?.Properties?.Headers;
            if (headers == null || !headers.ContainsKey("message-id")) return null;
            if (headers["message-id"] is byte[] operationBytes)
                return ByteToString(operationBytes);
            return null;
        }
    }

    public int? TryCount
    {
        get
        {
            var headers = m_args?.Properties?.Headers;
            if (headers == null || !headers.TryGetValue(SPH_TRYCOUNT, out var blob))
                return null;
            if (blob is int i)
                return i;

            if (blob is byte[] operationBytes)
            {
                var sct = ByteToString(operationBytes);
                if (int.TryParse(sct, out var tryCount))
                    return tryCount;
            }

            return null;
        }
    }

    public long? Delay
    {
        get
        {
            var headers = m_args?.Properties?.Headers;
            if (headers == null || !headers.ContainsKey(SPH_DELAY))
                return null;
            var blob = headers[SPH_DELAY];
            if (blob is int i)
                return i;
            if (blob is long l)
                return l;

            if (blob is byte[] operationBytes)
            {
                var sct = ByteToString(operationBytes);
                if (long.TryParse(sct, out var delayText))
                    return delayText;
            }

            return null;
        }
    }

    public string? Username
    {
        get
        {
            var headers = m_args?.Properties?.Headers;
            if (headers == null || !headers.ContainsKey("username")) return null;
            var prop = headers["username"];
            if (prop is string user && !string.IsNullOrWhiteSpace(user))
                return user;

            if (prop is byte[] operationBytes)
                return ByteToString(operationBytes);

            return null;
        }
    }

    public IDictionary<string, object> GetRawHeaders()
    {
        var headers = m_args?.Properties?.Headers;
        if (headers == null) return new Dictionary<string, object>();
        return headers.ToDictionary(k => k.Key, v => v.Value ?? new object());
    }

    public CrudOperation Crud
    {
        get
        {
            var crud = CrudOperation.None;
            var headers = m_args?.Properties?.Headers;
            if (headers == null || !headers.ContainsKey("crud")) return crud;
            if (headers["crud"] is byte[] operationBytes)
            {
                var v = ByteToString(operationBytes);
                if (Enum.TryParse(v, true, out crud))
                    return crud;
            }

            return crud;
        }
    }

    public string? ReplyTo
    {
        get
        {
            var headers = m_args?.Properties?.Headers;
            if (headers == null || !headers.ContainsKey("reply-to"))
                return null;
            var prop = headers["reply-to"];
            if (prop is string replyTo && !string.IsNullOrWhiteSpace(replyTo))
                return replyTo;

            if (prop is byte[] operationBytes)
                return ByteToString(operationBytes);

            return null;
        }
    }

    public T? GetValue<T>(string key)
    {
        var headers = m_args?.Properties?.Headers;
        if (headers == null || !headers.ContainsKey(key))
            return default;
        var blob = headers[key];
        if (blob?.GetType() == typeof(T))
            return (T)blob;

        if (blob is not byte[] operationBytes) return default;
        var sct = ByteToString(operationBytes);

        if (typeof(T) == typeof(bool) && bool.TryParse(sct, out var boolValue))
            return (T)(object)boolValue;

        if (typeof(T) == typeof(int) && int.TryParse(sct, out var intVal))
            return (T)(object)intVal;

        if (typeof(T) == typeof(double) && double.TryParse(sct, out var doubleVal))
            return (T)(object)doubleVal;

        if (typeof(T) == typeof(decimal) && decimal.TryParse(sct, out var decimalVal))
            return (T)(object)decimalVal;

        if (typeof(T) == typeof(DateTime) && DateTime.TryParse(sct, out var dateVal))
            return (T)(object)dateVal;

        return default;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        result = null;
        var headers = m_args?.Properties?.Headers;
        if (headers == null || !headers.ContainsKey(binder.Name))
            return false;

        var value = headers[binder.Name];
        if (value == null) return true;

        if (value is string strVal)
        {
            result = strVal;
            return true;
        }

        if (value is int intVal)
        {
            result = intVal;
            return true;
        }

        if (value is byte[] operationBytes)
        {
            result = ByteToString(operationBytes);
            return true;
        }

        return false;
    }
}
