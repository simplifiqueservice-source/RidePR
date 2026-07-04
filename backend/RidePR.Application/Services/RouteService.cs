using System.Text.Json;
using RidePR.Application.DTOs;

namespace RidePR.Application.Services;

public class RouteService
{
    private readonly HttpClient _http;

    public RouteService(HttpClient http)
    {
        _http = http;
    }

    public async Task<RouteResultDto?> GetRouteAsync(
        double originLat,
        double originLng,
        double destinationLat,
        double destinationLng)
    {
        var url =
            $"https://router.project-osrm.org/route/v1/driving/" +
            $"{originLng},{originLat};{destinationLng},{destinationLat}" +
            "?overview=full&geometries=polyline";

        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);

        var route = document.RootElement
            .GetProperty("routes")[0];

        return new RouteResultDto
        {
            DistanceKm =
                (decimal)route.GetProperty("distance").GetDouble() / 1000m,

            DurationMinutes =
                (decimal)route.GetProperty("duration").GetDouble() / 60m,

            Geometry =
                route.GetProperty("geometry").GetString() ?? ""
        };
    }
}