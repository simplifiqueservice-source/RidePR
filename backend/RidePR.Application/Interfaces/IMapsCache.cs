namespace RidePR.Application.Interfaces;

public interface IMapsCache
{
    Task<T?> GetAsync<T>(string key);

    Task SetAsync<T>(string key, T value, TimeSpan ttl);
}
