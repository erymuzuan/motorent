namespace MotoRent.Domain.Settings;

/// <summary>
/// Interface for typed access to organization settings with caching.
/// Settings are scoped by tenant (AccountNo) from IRequestContext.
/// </summary>
public interface ISettingConfig
{
    /// <summary>
    /// Gets a string setting value.
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="userName">Optional username for user-specific settings</param>
    /// <param name="defaultValue">Default value if not found</param>
    Task<string?> GetStringAsync(string key, string? userName = null, string? defaultValue = null);

    /// <summary>
    /// Gets a boolean setting value.
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="userName">Optional username for user-specific settings</param>
    /// <param name="defaultValue">Default value if not found</param>
    Task<bool> GetBoolAsync(string key, string? userName = null, bool defaultValue = false);

    /// <summary>
    /// Gets an integer setting value.
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="userName">Optional username for user-specific settings</param>
    /// <param name="defaultValue">Default value if not found</param>
    Task<int> GetIntAsync(string key, string? userName = null, int defaultValue = 0);

    /// <summary>
    /// Gets a decimal setting value.
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="userName">Optional username for user-specific settings</param>
    /// <param name="defaultValue">Default value if not found</param>
    Task<decimal> GetDecimalAsync(string key, string? userName = null, decimal defaultValue = 0);

    /// <summary>
    /// Gets a string array setting value (stored as comma-separated or JSON array).
    /// </summary>
    /// <param name="key">Setting key</param>
    /// <param name="userName">Optional username for user-specific settings</param>
    Task<string[]> GetArrayAsync(string key, string? userName = null);

    /// <summary>
    /// Sets a setting value.
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    /// <param name="key">Setting key</param>
    /// <param name="value">Value to set</param>
    /// <param name="userName">Optional username for user-specific settings</param>
    Task SetValueAsync<T>(string key, T value, string? userName = null);

    /// <summary>
    /// Sets a setting value with expiration.
    /// </summary>
    /// <typeparam name="T">Value type</typeparam>
    /// <param name="key">Setting key</param>
    /// <param name="value">Value to set</param>
    /// <param name="expires">Expiration time</param>
    /// <param name="userName">Optional username for user-specific settings</param>
    Task SetValueAsync<T>(string key, T value, DateTimeOffset expires, string? userName = null);

    /// <summary>
    /// Clears the settings cache for the current tenant.
    /// Call this after bulk updates to refresh cached values.
    /// </summary>
    void InvalidateCache();
}
