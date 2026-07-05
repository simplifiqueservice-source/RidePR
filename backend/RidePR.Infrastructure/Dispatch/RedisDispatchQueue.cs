using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;

namespace RidePR.Infrastructure.Dispatch;

public class RedisDispatchQueue : IDispatchQueue
{
    private const string ActiveTripsKey = "dispatch:active-trips";
    private readonly IDistributedCache _cache;

    public RedisDispatchQueue(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<DispatchStateDto?> GetAsync(Guid tripId)
    {
        var json = await _cache.GetStringAsync(GetTripKey(tripId));

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<DispatchStateDto>(json);
    }

    public async Task SetAsync(DispatchStateDto state)
    {
        await _cache.SetStringAsync(
            GetTripKey(state.TripId),
            JsonSerializer.Serialize(state),
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(2)
            });

        var activeTripIds = await GetActiveTripIdsAsync();

        if (!activeTripIds.Contains(state.TripId))
            activeTripIds.Add(state.TripId);

        await SetActiveTripIdsAsync(activeTripIds);
    }

    public async Task RemoveAsync(Guid tripId)
    {
        await _cache.RemoveAsync(GetTripKey(tripId));

        var activeTripIds = await GetActiveTripIdsAsync();
        activeTripIds.Remove(tripId);
        await SetActiveTripIdsAsync(activeTripIds);
    }

    public async Task<List<Guid>> GetActiveTripIdsAsync()
    {
        var json = await _cache.GetStringAsync(ActiveTripsKey);

        return string.IsNullOrWhiteSpace(json)
            ? new List<Guid>()
            : JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
    }

    private async Task SetActiveTripIdsAsync(List<Guid> tripIds)
    {
        await _cache.SetStringAsync(
            ActiveTripsKey,
            JsonSerializer.Serialize(tripIds.Distinct().ToList()),
            new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(2)
            });
    }

    private static string GetTripKey(Guid tripId)
    {
        return $"dispatch:trip:{tripId}";
    }
}
