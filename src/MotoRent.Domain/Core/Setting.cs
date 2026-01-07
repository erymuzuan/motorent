using MotoRent.Domain.Entities;

namespace MotoRent.Domain.Core;

/// <summary>
/// Key-value setting that can be scoped to an organization, user, or both.
/// Supports expiration for time-limited settings.
/// </summary>
public class Setting : Entity
{
    public int SettingId { get; set; }

    /// <summary>
    /// Organization AccountNo this setting belongs to.
    /// </summary>
    public string AccountNo { get; set; } = "";

    /// <summary>
    /// Setting key/name.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    /// Setting value (stored as string, use GetValue<T> for typed access).
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Optional username for user-specific settings. Null for organization-wide settings.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Setting type hint for UI.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Optional expiration time for temporary settings.
    /// </summary>
    public DateTimeOffset? Expire { get; set; }

    /// <summary>
    /// Gets the value as the specified type.
    /// </summary>
    public T? GetValue<T>()
    {
        if (string.IsNullOrWhiteSpace(Value))
            return default;

        if (typeof(T) == typeof(string))
            return (T?)(object?)Value;

        if (typeof(T) == typeof(int) && int.TryParse(Value, out var intVal))
            return (T)(object)intVal;

        if (typeof(T) == typeof(decimal) && decimal.TryParse(Value, out var decVal))
            return (T)(object)decVal;

        if (typeof(T) == typeof(double) && double.TryParse(Value, out var dblVal))
            return (T)(object)dblVal;

        if (typeof(T) == typeof(bool) && bool.TryParse(Value, out var boolVal))
            return (T)(object)boolVal;

        if (typeof(T) == typeof(DateOnly) && DateOnly.TryParse(Value, out var dateVal))
            return (T)(object)dateVal;

        if (typeof(T) == typeof(DateTimeOffset) && DateTimeOffset.TryParse(Value, out var dtoVal))
            return (T)(object)dtoVal;

        return default;
    }

    /// <summary>
    /// Sets the value from a typed value.
    /// </summary>
    public void SetValue<T>(T value)
    {
        Value = value?.ToString();
    }

    /// <summary>
    /// Checks if the setting has expired.
    /// </summary>
    public bool IsExpired => Expire.HasValue && Expire.Value < DateTimeOffset.Now;

    public override int GetId() => SettingId;
    public override void SetId(int value) => SettingId = value;

    public override string ToString() => $"{Key}={Value}";
}
