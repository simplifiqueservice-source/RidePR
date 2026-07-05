using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Application.Settings;
using RidePR.Shared.Results;

namespace RidePR.Application.Services;

public class RouteService
{
    private readonly IEnumerable<IMapProvider> _providers;
    private readonly IMapsCache _cache;
    private readonly MapsOptions _options;

    public RouteService(
        IEnumerable<IMapProvider> providers,
        IMapsCache cache,
        IOptions<MapsOptions> options)
    {
        _providers = providers;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<RouteResultDto?> GetRouteAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng)
    {
        var result = await GetRouteAsync(new MapRouteRequestDto
        {
            Origin = new CoordinateDto { Latitude = originLat, Longitude = originLng },
            Destination = new CoordinateDto { Latitude = destinationLat, Longitude = destinationLng }
        });

        return result.Data;
    }

    public async Task<Result<RouteResultDto>> GetRouteAsync(MapRouteRequestDto request)
    {
        var provider = ResolveProvider(request.Provider);
        var cacheKey = CreateCacheKey("route", provider.Name, request);
        var cached = await _cache.GetAsync<RouteResultDto>(cacheKey);

        if (cached != null)
            return Result<RouteResultDto>.Ok(cached);

        var route = await provider.GetRouteAsync(request);

        if (route == null)
            return Result<RouteResultDto>.Fail("Rota nao encontrada.");

        await _cache.SetAsync(cacheKey, route, GetCacheTtl());

        return Result<RouteResultDto>.Ok(route);
    }

    public async Task<Result<DistanceMatrixResultDto>> GetDistanceMatrixAsync(DistanceMatrixRequestDto request)
    {
        var provider = ResolveProvider(request.Provider);
        var cacheKey = CreateCacheKey("matrix", provider.Name, request);
        var cached = await _cache.GetAsync<DistanceMatrixResultDto>(cacheKey);

        if (cached != null)
            return Result<DistanceMatrixResultDto>.Ok(cached);

        var matrix = await provider.GetDistanceMatrixAsync(request);

        if (matrix == null)
            return Result<DistanceMatrixResultDto>.Fail("Matriz de distancia nao encontrada.");

        await _cache.SetAsync(cacheKey, matrix, GetCacheTtl());

        return Result<DistanceMatrixResultDto>.Ok(matrix);
    }

    public async Task<Result<EtaResultDto>> GetEtaAsync(EtaRequestDto request)
    {
        var routeResult = await GetRouteAsync(new MapRouteRequestDto
        {
            Origin = request.Origin,
            Destination = request.Destination,
            Provider = request.Provider
        });

        if (!routeResult.Success || routeResult.Data == null)
            return Result<EtaResultDto>.Fail(routeResult.Message);

        return Result<EtaResultDto>.Ok(new EtaResultDto
        {
            DistanceKm = routeResult.Data.DistanceKm,
            EtaMinutes = routeResult.Data.EtaMinutes,
            Provider = routeResult.Data.Provider
        });
    }

    public async Task<Result<GeocodingResultDto>> GeocodeAsync(GeocodingRequestDto request)
    {
        var provider = ResolveProvider(request.Provider);
        var cacheKey = CreateCacheKey("geocode", provider.Name, request);
        var cached = await _cache.GetAsync<GeocodingResultDto>(cacheKey);

        if (cached != null)
            return Result<GeocodingResultDto>.Ok(cached);

        var result = await provider.GeocodeAsync(request);

        if (result == null)
            return Result<GeocodingResultDto>.Fail("Endereco nao encontrado.");

        await _cache.SetAsync(cacheKey, result, GetCacheTtl());

        return Result<GeocodingResultDto>.Ok(result);
    }

    public async Task<Result<GeocodingResultDto>> ReverseGeocodeAsync(ReverseGeocodingRequestDto request)
    {
        var provider = ResolveProvider(request.Provider);
        var cacheKey = CreateCacheKey("reverse", provider.Name, request);
        var cached = await _cache.GetAsync<GeocodingResultDto>(cacheKey);

        if (cached != null)
            return Result<GeocodingResultDto>.Ok(cached);

        var result = await provider.ReverseGeocodeAsync(request);

        if (result == null)
            return Result<GeocodingResultDto>.Fail("Endereco reverso nao encontrado.");

        await _cache.SetAsync(cacheKey, result, GetCacheTtl());

        return Result<GeocodingResultDto>.Ok(result);
    }

    private IMapProvider ResolveProvider(string? providerName)
    {
        var name = string.IsNullOrWhiteSpace(providerName)
            ? _options.DefaultProvider
            : providerName;

        return _providers.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? _providers.First(x => x.Name.Equals(_options.DefaultProvider, StringComparison.OrdinalIgnoreCase));
    }

    private TimeSpan GetCacheTtl()
    {
        return TimeSpan.FromMinutes(_options.CacheMinutes <= 0 ? 15 : _options.CacheMinutes);
    }

    private static string CreateCacheKey<T>(string operation, string provider, T request)
    {
        var json = JsonSerializer.Serialize(request);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));

        return $"maps:{provider.ToLowerInvariant()}:{operation}:{hash}";
    }
}
