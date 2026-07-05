using System.Globalization;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using RidePR.Application.DTOs;
using RidePR.Application.Interfaces;
using RidePR.Application.Settings;

namespace RidePR.Infrastructure.Maps;

public class OpenStreetMapProvider : IMapProvider
{
    private readonly HttpClient _http;
    private readonly OpenStreetMapOptions _options;

    public OpenStreetMapProvider(HttpClient http, IOptions<MapsOptions> options)
    {
        _http = http;
        _options = options.Value.OpenStreetMap;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
    }

    public string Name => "OpenStreetMap";

    public async Task<RouteResultDto?> GetRouteAsync(MapRouteRequestDto request)
    {
        var geometryFormat = ResolveGeometryFormat(request.GeometryFormat);
        var url = $"{_options.OsrmBaseUrl}/route/v1/driving/" +
                  $"{Format(request.Origin.Longitude)},{Format(request.Origin.Latitude)};" +
                  $"{Format(request.Destination.Longitude)},{Format(request.Destination.Latitude)}" +
                  $"?overview=full&geometries={geometryFormat}&steps=false&alternatives=false";

        using var document = await GetJsonAsync(url);
        var root = document?.RootElement;

        if (root == null ||
            !IsOk(root.Value) ||
            !root.Value.TryGetProperty("routes", out var routes) ||
            routes.GetArrayLength() == 0)
            return null;

        var route = routes[0];
        var duration = (decimal)route.GetProperty("duration").GetDouble() / 60m;
        var geometry = route.GetProperty("geometry");

        return new RouteResultDto
        {
            DistanceKm = (decimal)route.GetProperty("distance").GetDouble() / 1000m,
            DurationMinutes = duration,
            EtaMinutes = duration,
            Geometry = geometry.ValueKind == JsonValueKind.String
                ? geometry.GetString() ?? ""
                : geometry.GetRawText(),
            GeometryFormat = geometryFormat,
            Provider = Name
        };
    }

    public async Task<DistanceMatrixResultDto?> GetDistanceMatrixAsync(DistanceMatrixRequestDto request)
    {
        var coordinates = request.Origins.Concat(request.Destinations).ToList();
        var origins = string.Join(";", Enumerable.Range(0, request.Origins.Count));
        var destinations = string.Join(";", Enumerable.Range(request.Origins.Count, request.Destinations.Count));
        var points = string.Join(";", coordinates.Select(x => $"{Format(x.Longitude)},{Format(x.Latitude)}"));
        var url = $"{_options.OsrmBaseUrl}/table/v1/driving/{points}?sources={origins}&destinations={destinations}&annotations=distance,duration";

        using var document = await GetJsonAsync(url);
        var root = document?.RootElement;

        if (root == null ||
            !IsOk(root.Value) ||
            !root.Value.TryGetProperty("distances", out var distances) ||
            !root.Value.TryGetProperty("durations", out var durations))
            return null;

        var result = new DistanceMatrixResultDto { Provider = Name };

        for (var i = 0; i < request.Origins.Count; i++)
        {
            for (var j = 0; j < request.Destinations.Count; j++)
            {
                result.Elements.Add(new DistanceMatrixElementDto
                {
                    OriginIndex = i,
                    DestinationIndex = j,
                    DistanceKm = ReadNullableDouble(distances[i][j]) is { } distance
                        ? (decimal)distance / 1000m
                        : 0,
                    DurationMinutes = ReadNullableDouble(durations[i][j]) is { } duration
                        ? (decimal)duration / 60m
                        : 0
                });
            }
        }

        return result;
    }

    public async Task<GeocodingResultDto?> GeocodeAsync(GeocodingRequestDto request)
    {
        var url = $"{_options.NominatimBaseUrl}/search?format=json&limit=1&addressdetails=1&accept-language={HttpUtility.UrlEncode(_options.AcceptLanguage)}&q={HttpUtility.UrlEncode(request.Address)}";
        using var document = await GetJsonAsync(url);
        var root = document?.RootElement;

        if (root == null || root.Value.GetArrayLength() == 0)
            return null;

        var item = root.Value[0];

        return new GeocodingResultDto
        {
            Address = item.GetProperty("display_name").GetString() ?? request.Address,
            Latitude = double.Parse(item.GetProperty("lat").GetString() ?? "0", CultureInfo.InvariantCulture),
            Longitude = double.Parse(item.GetProperty("lon").GetString() ?? "0", CultureInfo.InvariantCulture),
            Provider = Name
        };
    }

    public async Task<GeocodingResultDto?> ReverseGeocodeAsync(ReverseGeocodingRequestDto request)
    {
        var url = $"{_options.NominatimBaseUrl}/reverse?format=json&addressdetails=1&accept-language={HttpUtility.UrlEncode(_options.AcceptLanguage)}&lat={Format(request.Coordinate.Latitude)}&lon={Format(request.Coordinate.Longitude)}";
        using var document = await GetJsonAsync(url);
        var root = document?.RootElement;

        if (root == null || !root.Value.TryGetProperty("display_name", out var address))
            return null;

        return new GeocodingResultDto
        {
            Address = address.GetString() ?? "",
            Latitude = request.Coordinate.Latitude,
            Longitude = request.Coordinate.Longitude,
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

    private static string Format(double value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private string ResolveGeometryFormat(string? requestedFormat)
    {
        var value = string.IsNullOrWhiteSpace(requestedFormat)
            ? _options.RouteGeometryFormat
            : requestedFormat;

        return value.Equals("geojson", StringComparison.OrdinalIgnoreCase)
            ? "geojson"
            : "polyline";
    }

    private static bool IsOk(JsonElement root)
    {
        return !root.TryGetProperty("code", out var code) ||
               code.GetString()?.Equals("Ok", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static double? ReadNullableDouble(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Number
            ? element.GetDouble()
            : null;
    }
}
