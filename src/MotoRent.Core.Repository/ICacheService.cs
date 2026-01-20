using Microsoft.Extensions.Caching.Hybrid;

namespace MotoRent.Core.Repository;

public interface ICacheService
{
    Task RemoveAsync(string? key, string[]? tags = null);

    ValueTask<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T?>> factory,
        HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    ValueTask<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T?>> factory,
        TimeSpan expiration, TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    ValueTask<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T?>> factory,
        int expirationSeconds, int? localExpirationSeconds = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);
}
