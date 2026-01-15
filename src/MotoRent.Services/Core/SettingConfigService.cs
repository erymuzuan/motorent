using System.Collections.Concurrent;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Settings;

namespace MotoRent.Services.Core;

/// <summary>
/// SQL-based implementation of ISettingConfig with in-memory caching.
/// Settings are scoped by tenant (AccountNo) from IRequestContext.
/// </summary>
public class SettingConfigService(
    CoreDataContext context,
    IRequestContext requestContext) : ISettingConfig
{
    private CoreDataContext Context { get; } = context;
    private IRequestContext RequestContext { get; } = requestContext;

    // Cache keyed by AccountNo, value is dictionary of Key -> Setting
    private static readonly ConcurrentDictionary<string, CacheEntry> s_cache = new();
    private static readonly TimeSpan s_cacheExpiration = TimeSpan.FromMinutes(15);

    private class CacheEntry
    {
        public Dictionary<string, Setting> Settings { get; set; } = new();
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsExpired => DateTimeOffset.Now > ExpiresAt;
    }

    #region ISettingConfig Implementation

    public async Task<string?> GetStringAsync(string key, string? userName = null, string? defaultValue = null)
    {
        var setting = await GetSettingAsync(key, userName);
        return setting?.Value ?? defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, string? userName = null, bool defaultValue = false)
    {
        var setting = await GetSettingAsync(key, userName);
        if (setting?.Value == null) return defaultValue;

        return bool.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, string? userName = null, int defaultValue = 0)
    {
        var setting = await GetSettingAsync(key, userName);
        if (setting?.Value == null) return defaultValue;

        return int.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task<decimal> GetDecimalAsync(string key, string? userName = null, decimal defaultValue = 0)
    {
        var setting = await GetSettingAsync(key, userName);
        if (setting?.Value == null) return defaultValue;

        return decimal.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task<string[]> GetArrayAsync(string key, string? userName = null)
    {
        var setting = await GetSettingAsync(key, userName);
        if (setting?.Value == null) return [];

        // Try to parse as comma-separated values
        return setting.Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public async Task SetValueAsync<T>(string key, T value, string? userName = null)
    {
        await SetValueAsync(key, value, (DateTimeOffset?)null, userName);
    }

    public async Task SetValueAsync<T>(string key, T value, DateTimeOffset expires, string? userName = null)
    {
        await SetValueAsync(key, value, (DateTimeOffset?)expires, userName);
    }

    private async Task SetValueAsync<T>(string key, T value, DateTimeOffset? expires, string? userName)
    {
        var accountNo = this.RequestContext.GetAccountNo();
        if (string.IsNullOrWhiteSpace(accountNo))
            throw new InvalidOperationException("AccountNo is required to set settings");

        var currentUserName = this.RequestContext.GetUserName() ?? "system";

        // Load existing setting or create new
        var setting = await GetSettingFromDbAsync(key, userName);

        if (setting == null)
        {
            // Create new setting
            setting = new Setting
            {
                AccountNo = accountNo,
                Key = key,
                UserName = userName,
                WebId = Guid.NewGuid().ToString()
            };
        }

        // Update value
        if (value is string[] array)
        {
            setting.Value = string.Join(",", array);
        }
        else
        {
            setting.SetValue(value);
        }

        setting.Expire = expires;

        // Save to database
        using var session = this.Context.OpenSession(currentUserName);
        session.Attach(setting);
        await session.SubmitChanges("UpdateSetting");

        // Invalidate cache for this tenant
        InvalidateCache();
    }

    public void InvalidateCache()
    {
        var accountNo = this.RequestContext.GetAccountNo();
        if (!string.IsNullOrWhiteSpace(accountNo))
        {
            s_cache.TryRemove(accountNo, out _);
        }
    }

    #endregion

    #region Private Methods

    private async Task<Setting?> GetSettingAsync(string key, string? userName)
    {
        var accountNo = this.RequestContext.GetAccountNo();
        if (string.IsNullOrWhiteSpace(accountNo)) return null;

        // Try cache first
        var cache = await GetOrLoadCacheAsync(accountNo);

        // Build cache key: "key" for org-wide, "key|userName" for user-specific
        var cacheKey = string.IsNullOrWhiteSpace(userName) ? key : $"{key}|{userName}";

        if (cache.Settings.TryGetValue(cacheKey, out var setting))
        {
            // Check if setting has expired
            if (setting.IsExpired)
            {
                cache.Settings.Remove(cacheKey);
                return null;
            }
            return setting;
        }

        // Fall back to org-wide setting if user-specific not found
        if (!string.IsNullOrWhiteSpace(userName) && cache.Settings.TryGetValue(key, out var orgSetting))
        {
            if (!orgSetting.IsExpired)
                return orgSetting;
        }

        return null;
    }

    private async Task<Setting?> GetSettingFromDbAsync(string key, string? userName)
    {
        var accountNo = this.RequestContext.GetAccountNo();
        if (string.IsNullOrWhiteSpace(accountNo)) return null;

        // Use simple query with single Where clauses that CoreRepository can parse
        // Then filter in memory for the exact match
        var query = this.Context.Settings
            .Where(s => s.AccountNo == accountNo)
            .Where(s => s.Key == key);

        var result = await this.Context.LoadAsync(query, page: 1, size: 10, includeTotalRows: false);

        // Filter in memory for user-specific or org-wide setting
        if (string.IsNullOrWhiteSpace(userName))
        {
            return result.ItemCollection.FirstOrDefault(s =>
                string.IsNullOrWhiteSpace(s.UserName));
        }
        else
        {
            return result.ItemCollection.FirstOrDefault(s => s.UserName == userName);
        }
    }

    private async Task<CacheEntry> GetOrLoadCacheAsync(string accountNo)
    {
        if (s_cache.TryGetValue(accountNo, out var entry) && !entry.IsExpired)
        {
            return entry;
        }

        // Load all settings for this tenant
        var query = this.Context.Settings
            .Where(s => s.AccountNo == accountNo)
            .OrderBy(s => s.Key);

        var result = await this.Context.LoadAsync(query, page: 1, size: 1000, includeTotalRows: false);

        // Build cache dictionary
        var settings = new Dictionary<string, Setting>();
        foreach (var setting in result.ItemCollection)
        {
            // Build cache key: "key" for org-wide, "key|userName" for user-specific
            var cacheKey = string.IsNullOrWhiteSpace(setting.UserName)
                ? setting.Key
                : $"{setting.Key}|{setting.UserName}";

            settings[cacheKey] = setting;
        }

        entry = new CacheEntry
        {
            Settings = settings,
            ExpiresAt = DateTimeOffset.Now.Add(s_cacheExpiration)
        };

        s_cache[accountNo] = entry;
        return entry;
    }

    #endregion
}
