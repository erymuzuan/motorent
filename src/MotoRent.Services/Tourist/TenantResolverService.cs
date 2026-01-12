using System.Collections.Concurrent;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Storage;
using MotoRent.Domain.Tourist;

namespace MotoRent.Services.Tourist;

/// <summary>
/// Interface for resolving tenant context from AccountNo or custom domain.
/// </summary>
public interface ITenantResolverService
{
    /// <summary>
    /// Resolves tenant context by AccountNo.
    /// </summary>
    Task<TenantContext?> ResolveByAccountNoAsync(string accountNo);

    /// <summary>
    /// Resolves tenant context by custom domain (e.g., "adam.co.th").
    /// </summary>
    Task<TenantContext?> ResolveByCustomDomainAsync(string domain);

    /// <summary>
    /// Clears the cache for a specific tenant (e.g., after branding update).
    /// </summary>
    void InvalidateCache(string accountNo);

    /// <summary>
    /// Clears all cached tenant contexts.
    /// </summary>
    void ClearCache();
}

/// <summary>
/// SQL-based implementation of tenant resolver with in-memory caching.
/// </summary>
public class TenantResolverService : ITenantResolverService
{
    private readonly CoreDataContext m_context;
    private readonly IBinaryStore m_binaryStore;

    // Cache with 15-minute expiration
    private static readonly ConcurrentDictionary<string, CachedTenant> s_cache = new();
    private static readonly TimeSpan s_cacheExpiration = TimeSpan.FromMinutes(15);

    public TenantResolverService(CoreDataContext context, IBinaryStore binaryStore)
    {
        m_context = context;
        m_binaryStore = binaryStore;
    }

    public async Task<TenantContext?> ResolveByAccountNoAsync(string accountNo)
    {
        if (string.IsNullOrWhiteSpace(accountNo))
            return null;

        var cacheKey = accountNo.ToLowerInvariant();

        // Check cache first
        if (s_cache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
            return cached.Context;

        // Load from database
        var org = await m_context.LoadOneAsync<Organization>(o => o.AccountNo == accountNo);
        if (org == null || !org.IsActive)
            return null;

        var context = MapToTenantContext(org);

        // Cache the result
        CacheContext(cacheKey, context);

        // Also cache by custom domain if set
        if (!string.IsNullOrEmpty(org.CustomDomain))
        {
            var domainKey = $"domain:{org.CustomDomain.ToLowerInvariant()}";
            CacheContext(domainKey, context);
        }

        return context;
    }

    public async Task<TenantContext?> ResolveByCustomDomainAsync(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return null;

        var cacheKey = $"domain:{domain.ToLowerInvariant()}";

        // Check cache first
        if (s_cache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
            return cached.Context;

        // Load from database by custom domain
        var org = await m_context.LoadOneAsync<Organization>(o => o.CustomDomain == domain);
        if (org == null || !org.IsActive)
            return null;

        var context = MapToTenantContext(org);

        // Cache by both domain and AccountNo
        CacheContext(cacheKey, context);
        CacheContext(org.AccountNo.ToLowerInvariant(), context);

        return context;
    }

    public void InvalidateCache(string accountNo)
    {
        if (string.IsNullOrWhiteSpace(accountNo))
            return;

        var cacheKey = accountNo.ToLowerInvariant();
        s_cache.TryRemove(cacheKey, out _);

        // Also try to remove any domain cache entries for this tenant
        var domainEntries = s_cache
            .Where(kvp => kvp.Key.StartsWith("domain:") && kvp.Value.Context?.AccountNo == accountNo)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in domainEntries)
        {
            s_cache.TryRemove(key, out _);
        }
    }

    public void ClearCache()
    {
        s_cache.Clear();
    }

    private TenantContext MapToTenantContext(Organization org)
    {
        return new TenantContext
        {
            AccountNo = org.AccountNo,
            OrganizationName = org.Name,
            LogoUrl = GetLogoUrl(org.LogoStoreId),
            SmallLogoUrl = GetLogoUrl(org.SmallLogoStoreId),
            Currency = org.Currency,
            Timezone = org.Timezone ?? 7,
            Language = org.Language,
            Phone = org.Phone,
            Email = org.Email,
            WebSite = org.WebSite,
            Branding = org.Branding ?? new TenantBranding()
        };
    }

    private string? GetLogoUrl(string? storeId)
    {
        if (string.IsNullOrEmpty(storeId))
            return null;

        try
        {
            return m_binaryStore.GetImageUrl(storeId);
        }
        catch
        {
            return null;
        }
    }

    private static void CacheContext(string key, TenantContext context)
    {
        s_cache[key] = new CachedTenant(context, DateTime.UtcNow.Add(s_cacheExpiration));
    }

    /// <summary>
    /// Cached tenant context with expiration.
    /// </summary>
    private sealed class CachedTenant
    {
        public TenantContext? Context { get; }
        public DateTime ExpiresAt { get; }
        public bool IsExpired => DateTime.UtcNow > ExpiresAt;

        public CachedTenant(TenantContext? context, DateTime expiresAt)
        {
            Context = context;
            ExpiresAt = expiresAt;
        }
    }
}
