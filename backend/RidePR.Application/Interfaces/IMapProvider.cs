using RidePR.Application.DTOs;

namespace RidePR.Application.Interfaces;

public interface IMapProvider
{
    string Name { get; }

    Task<RouteResultDto?> GetRouteAsync(MapRouteRequestDto request);

    Task<DistanceMatrixResultDto?> GetDistanceMatrixAsync(DistanceMatrixRequestDto request);

    Task<GeocodingResultDto?> GeocodeAsync(GeocodingRequestDto request);

    Task<GeocodingResultDto?> ReverseGeocodeAsync(ReverseGeocodingRequestDto request);
}
