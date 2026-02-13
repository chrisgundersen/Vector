using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Vector.Application.Common.Interfaces;

namespace Vector.Infrastructure.Caching;

/// <summary>
/// Redis cache implementation for production.
/// </summary>
public class RedisCacheService(
    IConnectionMultiplexer redis,
    ILogger<RedisCacheService> logger) : ICacheService
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);

        if (value.IsNullOrEmpty)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await _database.StringSetAsync(key, json, expiration);

        logger.LogDebug("Cached key {Key} with expiration {Expiration}", key, expiration);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _database.KeyDeleteAsync(key);
        logger.LogDebug("Removed cache key {Key}", key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _database.KeyExistsAsync(key);
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
}
