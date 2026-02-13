using System.Collections.Concurrent;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Caching;

/// <summary>
/// In-memory cache implementation for development/testing.
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            return Task.FromResult((T?)entry.Value);
        }

        return Task.FromResult(default(T?));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var entry = new CacheEntry
        {
            Value = value,
            ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null
        };

        _cache[key] = entry;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }

    private class CacheEntry
    {
        public object? Value { get; init; }
        public DateTime? ExpiresAt { get; init; }
        public bool IsExpired => ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }
}
