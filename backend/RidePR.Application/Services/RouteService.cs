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
            Destination = new CoordinateDto { Latitude = destinationLat, Longitude = destinationLng },
            GeometryFormat = _options.OpenStreetMap.RouteGeometryFormat
        });

        return result.Data;
    }

    public async Task<Result<RouteResultDto>> GetRouteAsync(MapRouteRequestDto request)
    {
        var validation = ValidateRouteRequest(request);

        if (!validation.Success)
            return Result<RouteResultDto>.Fail(validation.Message);

        var provider = ResolveProvider(request.Provider);
        var cacheKey = CreateCacheKey("route", provider.Name, request);
        var cached = await _cache.GetAsync<RouteResultDto>(cacheKey);

        if (cached != null)
            return Result<RouteResultDto>.Ok(cached);

        RouteResultDto? route;

        try
        {
            route = await provider.GetRouteAsync(request);
        }
        catch (HttpRequestException)
        {
            route = CreateEstimatedRoute(request, provider.Name);
        }
        catch (TaskCanceledException)
        {
            route = CreateEstimatedRoute(request, provider.Name);
        }

        if (route == null)
            route = CreateEstimatedRoute(request, provider.Name);

        await _cache.SetAsync(cacheKey, route, GetCacheTtl());

        return Result<RouteResultDto>.Ok(route);
    }

    public async Task<Result<DistanceMatrixResultDto>> GetDistanceMatrixAsync(DistanceMatrixRequestDto request)
    {
        var validation = ValidateMatrixRequest(request);

        if (!validation.Success)
            return Result<DistanceMatrixResultDto>.Fail(validation.Message);

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
        if (string.IsNullOrWhiteSpace(request.Address))
            return Result<GeocodingResultDto>.Fail("Endereco e obrigatorio.");

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
        if (!IsValidCoordinate(request.Coordinate))
            return Result<GeocodingResultDto>.Fail("Coordenada invalida.");

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

    public IReadOnlyList<MapProviderDto> GetProviders()
    {
        return _providers
            .Select(x => new MapProviderDto
            {
                Name = x.Name,
                Default = x.Name.Equals(_options.DefaultProvider, StringComparison.OrdinalIgnoreCase)
            })
            .OrderByDescending(x => x.Default)
            .ThenBy(x => x.Name)
            .ToList();
    }

    private IMapProvider ResolveProvider(string? providerName)
    {
        var name = string.IsNullOrWhiteSpace(providerName)
            ? _options.DefaultProvider
            : providerName;

        return _providers.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? _providers.FirstOrDefault(x => x.Name.Equals(_options.DefaultProvider, StringComparison.OrdinalIgnoreCase))
            ?? _providers.First();
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

    private static Result ValidateRouteRequest(MapRouteRequestDto request)
    {
        if (!IsValidCoordinate(request.Origin) || !IsValidCoordinate(request.Destination))
            return Result.Fail("Coordenadas de origem ou destino invalidas.");

        return Result.Ok();
    }

    private Result ValidateMatrixRequest(DistanceMatrixRequestDto request)
    {
        if (request.Origins.Count == 0 || request.Destinations.Count == 0)
            return Result.Fail("Informe ao menos uma origem e um destino.");

        var totalCoordinates = request.Origins.Count + request.Destinations.Count;
        var maxCoordinates = _options.MaxMatrixCoordinates <= 0 ? 25 : _options.MaxMatrixCoordinates;

        if (totalCoordinates > maxCoordinates)
            return Result.Fail($"Matriz permite no maximo {maxCoordinates} coordenadas.");

        if (request.Origins.Concat(request.Destinations).Any(x => !IsValidCoordinate(x)))
            return Result.Fail("Matriz contem coordenadas invalidas.");

        return Result.Ok();
    }

    private static bool IsValidCoordinate(CoordinateDto coordinate)
    {
        return coordinate.Latitude is >= -90 and <= 90 &&
               coordinate.Longitude is >= -180 and <= 180;
    }

    private static RouteResultDto CreateEstimatedRoute(MapRouteRequestDto request, string provider)
    {
        const double averageUrbanSpeedKmH = 30;
        const double earthRadiusKm = 6371;

        var originLat = ToRadians(request.Origin.Latitude);
        var destinationLat = ToRadians(request.Destination.Latitude);
        var deltaLat = ToRadians(request.Destination.Latitude - request.Origin.Latitude);
        var deltaLng = ToRadians(request.Destination.Longitude - request.Origin.Longitude);

        var haversine = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                        Math.Cos(originLat) *
                        Math.Cos(destinationLat) *
                        Math.Pow(Math.Sin(deltaLng / 2), 2);

        var angularDistance = 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
        var distanceKm = Math.Round(earthRadiusKm * angularDistance * 1.25, 2);
        var durationMinutes = Math.Max(1, Math.Round(distanceKm / averageUrbanSpeedKmH * 60, 0));

        return new RouteResultDto
        {
            DistanceKm = (decimal)distanceKm,
            DurationMinutes = (decimal)durationMinutes,
            EtaMinutes = (decimal)durationMinutes,
            Geometry = string.Empty,
            GeometryFormat = request.GeometryFormat ?? string.Empty,
            Provider = $"{provider}-estimated"
        };
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
