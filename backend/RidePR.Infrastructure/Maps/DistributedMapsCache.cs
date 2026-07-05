using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RidePR.Application.Interfaces;

namespace RidePR.Infrastructure.Maps;

public class DistributedMapsCache : IMapsCache
{
    private readonly IDistributedCache _cache;

    public DistributedMapsCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _cache.GetStringAsync(key);

        return string.IsNullOrWhiteSpace(json)
            ? default
            : JsonSerializer.Deserialize<T>(json);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);

        await _cache.SetStringAsync(
            key,
            json,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            });
    }
}
