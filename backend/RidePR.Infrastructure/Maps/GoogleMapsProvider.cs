using System.Globalization;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Application.Settings;

namespace RidePR.Infrastructure.Maps;

public class GoogleMapsProvider : IMapProvider
{
    private readonly HttpClient _http;
    private readonly GoogleMapsOptions _options;

    public GoogleMapsProvider(HttpClient http, IOptions<MapsOptions> options)
    {
        _http = http;
        _options = options.Value.Google;
    }

    public string Name => "Google";

    public async Task<RouteResultDto?> GetRouteAsync(MapRouteRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        var url = $"{_options.BaseUrl}/directions/json?origin={Coordinate(request.Origin)}&destination={Coordinate(request.Destination)}&key={_options.ApiKey}";
        using var document = await GetJsonAsync(url);
        var root = document?.RootElement;

        if (root == null || !root.Value.TryGetProperty("routes", out var routes) || routes.GetArrayLength() == 0)
            return null;

        var route = routes[0];
        var leg = route.GetProperty("legs")[0];
        var duration = (decimal)leg.GetProperty("duration").GetProperty("value").GetDouble() / 60m;

        return new RouteResultDto
        {
            DistanceKm = (decimal)leg.GetProperty("distance").GetProperty("value").GetDouble() / 1000m,
            DurationMinutes = duration,
            EtaMinutes = duration,
            Geometry = route.GetProperty("overview_polyline").GetProperty("points").GetString() ?? "",
            GeometryFormat = "polyline",
            Provider = Name
        };
    }

    public async Task<DistanceMatrixResultDto?> GetDistanceMatrixAsync(DistanceMatrixRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        var origins = string.Join("|", request.Origins.Select(Coordinate));
        var destinations = string.Join("|", request.Destinations.Select(Coordinate));
        var url = $"{_options.BaseUrl}/distancematrix/json?origins={HttpUtility.UrlEncode(origins)}&destinations={HttpUtility.UrlEncode(destinations)}&key={_options.ApiKey}";
        using var document = await GetJsonAsync(url);
        var root = document?.RootElement;

        if (root == null || !root.Value.TryGetProperty("rows", out var rows))
            return null;

        var result = new DistanceMatrixResultDto { Provider = Name };

        for (var i = 0; i < rows.GetArrayLength(); i++)
        {
            var elements = rows[i].GetProperty("elements");

            for (var j = 0; j < elements.GetArrayLength(); j++)
            {
                var element = elements[j];

                if (!element.TryGetProperty("distance", out var distance) ||
                    !element.TryGetProperty("duration", out var duration))
                    continue;

                result.Elements.Add(new DistanceMatrixElementDto
                {
                    OriginIndex = i,
                    DestinationIndex = j,
                    DistanceKm = (decimal)distance.GetProperty("value").GetDouble() / 1000m,
                    DurationMinutes = (decimal)duration.GetProperty("value").GetDouble() / 60m
                });
            }
        }

        return result;
    }

    public async Task<GeocodingResultDto?> GeocodeAsync(GeocodingRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        var url = $"{_options.BaseUrl}/geocode/json?address={HttpUtility.UrlEncode(request.Address)}&key={_options.ApiKey}";
        using var document = await GetJsonAsync(url);

        return ParseGeocode(document, request.Address);
    }

    public async Task<GeocodingResultDto?> ReverseGeocodeAsync(ReverseGeocodingRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return null;

        var url = $"{_options.BaseUrl}/geocode/json?latlng={Coordinate(request.Coordinate)}&key={_options.ApiKey}";
        using var document = await GetJsonAsync(url);

        return ParseGeocode(document, "");
    }

    private GeocodingResultDto? ParseGeocode(JsonDocument? document, string fallbackAddress)
    {
        var root = document?.RootElement;

        if (root == null || !root.Value.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
            return null;

        var item = results[0];
        var location = item.GetProperty("geometry").GetProperty("location");

        return new GeocodingResultDto
        {
            Address = item.GetProperty("formatted_address").GetString() ?? fallbackAddress,
            Latitude = location.GetProperty("lat").GetDouble(),
            Longitude = location.GetProperty("lng").GetDouble(),
            Provider = Name
        };
    }

    private async Task<JsonDocument?> GetJsonAsync(string url)
    {
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();

        return JsonDocument.Parse(json);
    }

    private static string Coordinate(CoordinateDto coordinate)
    {
        return $"{coordinate.Latitude.ToString(CultureInfo.InvariantCulture)},{coordinate.Longitude.ToString(CultureInfo.InvariantCulture)}";
    }
}
