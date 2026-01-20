using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;

namespace MotoRent.Core.Repository;

public class HybridCacheService(
    IRequestContext requestContext,
    HybridCache cache) : ICacheService
{
    private IRequestContext RequestContext { get; } = requestContext;
    private HybridCache Cache { get; } = cache;

    public async Task RemoveAsync(string? key, string[]? tags = null)
    {
        if (string.IsNullOrWhiteSpace(key) && tags is not [_, ..])
            throw new ArgumentException("Key cannot be null or empty, and tags must be provided if key is not specified.");

        if (!string.IsNullOrWhiteSpace(key))
            await this.Cache.RemoveAsync(key);

        if (tags is [_, ..])
        {
            foreach (var tag in tags)
            {
                await this.Cache.RemoveByTagAsync([tag]);
            }
        }
    }

    public async ValueTask<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T?>> factory,
        HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var account = await this.RequestContext.GetAccountNoAsync();
        var hash = $"{account}:{key}".GetMd5Hash();
        var actualTags = (tags ?? [account ?? "Core"]).ToArray();
        if (actualTags.Length == 0) actualTags = [account ?? "Core"];
        if (string.IsNullOrWhiteSpace(account))
            actualTags = null;

        if (typeof(T) == typeof(JsonElement))
        {
            var je = await this.Cache.GetOrCreateAsync(hash,
                async t =>
                {
                    var k = await factory(t);
                    return k?.ToString() ?? string.Empty;
                },
                options, actualTags, cancellationToken);

            if (!string.IsNullOrEmpty(je) && je.StartsWith('{') && je.EndsWith('}'))
                return (T)(object)JsonDocument.Parse(je).RootElement;
        }

        if (typeof(T).IsAssignableTo(typeof(Entity)))
        {
            var json = await this.Cache.GetOrCreateAsync(hash,
                async t =>
                {
                    var k = await factory(t);
                    return k?.ToJsonString() ?? string.Empty;
                },
                options, actualTags, cancellationToken);

            if (string.IsNullOrEmpty(json)) return default;
            return json.DeserializeFromJson<T>();
        }

        try
        {
            return await this.Cache.GetOrCreateAsync(hash, factory, options, actualTags, cancellationToken);
        }
        catch (JsonException)
        {
            return await factory(cancellationToken);
        }
    }

    public ValueTask<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T?>> factory,
        TimeSpan expiration, TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = new HybridCacheEntryOptions { Expiration = expiration, LocalCacheExpiration = localExpiration };
        return this.GetOrCreateAsync(key, factory, options, tags, cancellationToken);
    }

    public ValueTask<T?> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T?>> factory,
        int expirationSeconds, int? localExpirationSeconds = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromSeconds(expirationSeconds),
            LocalCacheExpiration =
                localExpirationSeconds > 0 ? TimeSpan.FromSeconds(localExpirationSeconds.Value) : null
        };
        return this.GetOrCreateAsync(key, factory, options, tags, cancellationToken);
    }
}

internal static class StringExtensions
{
    public static string GetMd5Hash(this string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes);
    }
}
